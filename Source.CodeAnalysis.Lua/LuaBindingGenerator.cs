using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Source.CodeAnalysis.Lua
{
	[Generator(LanguageNames.CSharp)]
	public sealed class LuaBindingGenerator : IIncrementalGenerator
	{
		private const string Namespace = "Source.Common.GarrysMod.Lua.";
		private const string ClassAttribute = Namespace + "LuaClassAttribute";
		private const string LibraryAttribute = Namespace + "LuaLibraryAttribute";
		private const string MethodAttribute = Namespace + "LuaMethodAttribute";
		private const string FunctionAttribute = Namespace + "LuaFunctionAttribute";
		private const string GlobalAttribute = Namespace + "LuaGlobalAttribute";
		private const string GetAttribute = Namespace + "LuaGetAttribute";
		private const string OptAttribute = Namespace + "LuaOptAttribute";
		private const string OptGenericAttribute = Namespace + "LuaOptAttribute`1";
		private const string ValidateAttribute = Namespace + "LuaValidateAttribute";
		private const string LuaInterfaceName = Namespace + "ILuaInterface";
		private const string LuaInterface = "global::Source.Common.GarrysMod.Lua.ILuaInterface";
		private const string LuaTypeNil = "global::Source.Common.GarrysMod.Lua.LuaType.Nil";

		private static DiagnosticDescriptor Descriptor(string id, string title, string message) => new DiagnosticDescriptor(
			id: id,
			title: title,
			messageFormat: message,
			category: "LuaBindings",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true);

		private static readonly DiagnosticDescriptor NotPartial = Descriptor("LUA001", "Lua binding type must be partial", "'{0}' contains Lua bindings but is not partial");
		private static readonly DiagnosticDescriptor Unsupported = Descriptor("LUA002", "Lua binding type must be top-level and non-generic", "'{0}' contains Lua bindings but is nested or generic");
		private static readonly DiagnosticDescriptor HasStaticConstructor = Descriptor("LUA003", "Lua binding type cannot declare a static constructor", "'{0}' declares a static constructor; Lua bindings register from a generated one");
		private static readonly DiagnosticDescriptor NoTarget = Descriptor("LUA004", "Lua binding has no single registration target", "'{0}' is marked [{1}] but '{2}' declares {3} [{4}] fields; exactly one is required");
		private static readonly DiagnosticDescriptor BadParameter = Descriptor("LUA005", "Unsupported Lua binding parameter", "Parameter '{0}' of '{1}': {2}");
		private static readonly DiagnosticDescriptor BadReturn = Descriptor("LUA006", "Unsupported Lua binding return type", "'{0}' returns '{1}', which cannot be pushed to Lua");
		private static readonly DiagnosticDescriptor BadMethod = Descriptor("LUA007", "Unsupported Lua binding method", "'{0}': {1}");

		private sealed class LuaClassInfo
		{
			public string Field = "";
			public INamedTypeSymbol Owner = null!;
			public ITypeSymbol Type = null!;
			public bool IsValue;
			public string? NullError;
		}

		public void Initialize(IncrementalGeneratorInitializationContext context) {
			IncrementalValueProvider<ImmutableArray<string>> methods = Methods(context, MethodAttribute);
			IncrementalValueProvider<ImmutableArray<string>> functions = Methods(context, FunctionAttribute);
			IncrementalValueProvider<ImmutableArray<string>> globals = Methods(context, GlobalAttribute);
			IncrementalValueProvider<ImmutableArray<string>> classes = context.SyntaxProvider
				.ForAttributeWithMetadataName(
					ClassAttribute,
					predicate: static (node, _) => node is VariableDeclaratorSyntax,
					transform: static (ctx, _) => MetadataName(ctx.TargetSymbol.ContainingType))
				.Collect();

			var all = methods.Combine(functions).Combine(globals).Combine(classes).Combine(context.CompilationProvider);
			context.RegisterSourceOutput(all, static (spc, data) => {
				Compilation compilation = data.Right;
				ImmutableArray<string> types = data.Left.Left.Left.Left.AddRange(data.Left.Left.Left.Right).AddRange(data.Left.Left.Right);
				Execute(spc, compilation, types.Distinct().OrderBy(t => t, System.StringComparer.Ordinal), data.Left.Right.Distinct());
			});
		}

		private static IncrementalValueProvider<ImmutableArray<string>> Methods(IncrementalGeneratorInitializationContext context, string attribute) => context.SyntaxProvider
			.ForAttributeWithMetadataName(
				attribute,
				predicate: static (node, _) => node is MethodDeclarationSyntax,
				transform: static (ctx, _) => MetadataName(ctx.TargetSymbol.ContainingType))
			.Collect();

		private static string MetadataName(INamedTypeSymbol type) =>
			type.ContainingNamespace.IsGlobalNamespace ? type.MetadataName : type.ContainingNamespace.ToDisplayString() + "." + type.MetadataName;

		private static bool Is(AttributeData attribute, string name) =>
			attribute.AttributeClass != null && MetadataName(attribute.AttributeClass.OriginalDefinition) == name;

		private static void Execute(SourceProductionContext spc, Compilation compilation, IEnumerable<string> typeNames, IEnumerable<string> classOwners) {
			Dictionary<ITypeSymbol, LuaClassInfo> classMap = new Dictionary<ITypeSymbol, LuaClassInfo>(SymbolEqualityComparer.Default);
			foreach (string ownerName in classOwners) {
				INamedTypeSymbol? owner = compilation.Assembly.GetTypeByMetadataName(ownerName);
				if (owner == null)
					continue;

				foreach (IFieldSymbol field in owner.GetMembers().OfType<IFieldSymbol>()) {
					AttributeData? attribute = field.GetAttributes().FirstOrDefault(a => Is(a, ClassAttribute));
					if (attribute == null || attribute.ConstructorArguments.Length != 1 || !(attribute.ConstructorArguments[0].Value is ITypeSymbol mapped))
						continue;

					string? nullError = null;
					foreach (KeyValuePair<string, TypedConstant> named in attribute.NamedArguments) {
						if (named.Key == "NullError")
							nullError = named.Value.Value as string;
					}

					classMap[mapped] = new LuaClassInfo {
						Field = field.Name,
						Owner = owner,
						Type = mapped,
						IsValue = mapped.TypeKind == TypeKind.Struct && mapped.IsUnmanagedType,
						NullError = nullError
					};
				}
			}

			foreach (string typeName in typeNames) {
				INamedTypeSymbol? type = compilation.Assembly.GetTypeByMetadataName(typeName);
				if (type != null)
					GenerateType(spc, type, classMap);
			}
		}

		private static void GenerateType(SourceProductionContext spc, INamedTypeSymbol type, Dictionary<ITypeSymbol, LuaClassInfo> classMap) {
			Location location = type.Locations.FirstOrDefault() ?? Location.None;

			bool isPartial = type.DeclaringSyntaxReferences
				.Select(r => r.GetSyntax())
				.OfType<TypeDeclarationSyntax>()
				.All(t => t.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)));
			if (!isPartial) {
				spc.ReportDiagnostic(Diagnostic.Create(NotPartial, location, type.Name));
				return;
			}

			if (type.ContainingType != null || type.IsGenericType) {
				spc.ReportDiagnostic(Diagnostic.Create(Unsupported, location, type.Name));
				return;
			}

			if (type.StaticConstructors.Any(c => !c.IsImplicitlyDeclared)) {
				spc.ReportDiagnostic(Diagnostic.Create(HasStaticConstructor, location, type.Name));
				return;
			}

			List<IFieldSymbol> classFields = type.GetMembers().OfType<IFieldSymbol>().Where(f => f.GetAttributes().Any(a => Is(a, ClassAttribute))).ToList();
			List<IFieldSymbol> libraryFields = type.GetMembers().OfType<IFieldSymbol>().Where(f => f.GetAttributes().Any(a => Is(a, LibraryAttribute))).ToList();

			List<IMethodSymbol> methods = type.GetMembers().OfType<IMethodSymbol>()
				.Where(m => m.GetAttributes().Any(a => Is(a, MethodAttribute) || Is(a, FunctionAttribute) || Is(a, GlobalAttribute)))
				.OrderBy(m => m.Locations.FirstOrDefault()?.SourceTree?.FilePath ?? "", System.StringComparer.Ordinal)
				.ThenBy(m => m.Locations.FirstOrDefault()?.SourceSpan.Start ?? 0)
				.ToList();

			HashSet<string> usedNames = new HashSet<string>(type.GetMembers().Select(m => m.Name));
			StringBuilder registrations = new StringBuilder();
			StringBuilder thunks = new StringBuilder();

			foreach (IMethodSymbol method in methods) {
				Location methodLocation = method.Locations.FirstOrDefault() ?? location;
				string? thunk = null;

				foreach (AttributeData attribute in method.GetAttributes()) {
					string kind;
					if (Is(attribute, MethodAttribute))
						kind = "LuaMethod";
					else if (Is(attribute, FunctionAttribute))
						kind = "LuaFunction";
					else if (Is(attribute, GlobalAttribute))
						kind = "LuaGlobal";
					else
						continue;

					string? explicitName = attribute.ConstructorArguments.Length > 0 ? attribute.ConstructorArguments[0].Value as string : null;
					string luaName;
					string target;
					string thunkName;

					if (kind == "LuaMethod") {
						if (classFields.Count != 1) {
							spc.ReportDiagnostic(Diagnostic.Create(NoTarget, methodLocation, method.Name, kind, type.Name, classFields.Count, "LuaClass"));
							continue;
						}

						int split = method.Name.IndexOf("__", System.StringComparison.Ordinal);
						luaName = explicitName ?? (split > 0 ? method.Name.Substring(split + 2) : method.Name);
						target = classFields[0].Name;
						thunkName = method.Name + "__Redirect";
					}
					else if (kind == "LuaFunction") {
						if (libraryFields.Count != 1) {
							spc.ReportDiagnostic(Diagnostic.Create(NoTarget, methodLocation, method.Name, kind, type.Name, libraryFields.Count, "LuaLibrary"));
							continue;
						}

						luaName = explicitName ?? method.Name;
						target = libraryFields[0].Name;
						thunkName = "redir__" + LibraryName(libraryFields[0]) + "__" + luaName;
					}
					else {
						luaName = explicitName ?? method.Name;
						target = "LuaGlobalLibrary";
						thunkName = "redir__GLobal__" + luaName;
					}

					if (thunk == null) {
						string? body = BuildBody(spc, method, methodLocation, type, classMap);
						if (body == null)
							break;

						thunk = thunkName;
						while (!usedNames.Add(thunk))
							thunk += "_";

						thunks.Append("\n\t[global::System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(global::System.Runtime.CompilerServices.CallConvCdecl) })]\n");
						thunks.Append("\tstatic int ").Append(thunk).Append("(nint L) {\n");
						thunks.Append("\t\t").Append(LuaInterface).Append(" lua = g_Lua!;\n");
						thunks.Append("\t\tlua.SetState(new global::Source.Common.GarrysMod.Lua.lua_State(L));\n");
						thunks.Append("\t\ttry {\n");
						thunks.Append(body);
						thunks.Append("\t\t}\n");
						thunks.Append("\t\tcatch (global::System.Exception e) {\n");
						thunks.Append("\t\t\treturn lua.HandleException(e);\n");
						thunks.Append("\t\t}\n");
						thunks.Append("\t}\n");
					}

					registrations.Append("\t\t").Append(target).Append(".Add(").Append(Literal(luaName)).Append(", &").Append(thunk).Append(");\n");
				}
			}

			if (registrations.Length == 0)
				return;

			StringBuilder source = new StringBuilder();
			source.Append("// <auto-generated/>\n");
			source.Append("#nullable enable\n\n");
			if (!type.ContainingNamespace.IsGlobalNamespace)
				source.Append("namespace ").Append(type.ContainingNamespace.ToDisplayString()).Append(";\n\n");
			source.Append(type.IsStatic ? "static partial class " : "partial class ").Append(type.Name).Append("\n{\n");
			source.Append("\tinternal static readonly bool LuaBindingsRegistered;\n\n");
			source.Append("\tstatic unsafe ").Append(type.Name).Append("() {\n");
			source.Append(registrations);
			source.Append("\t\tLuaBindingsRegistered = true;\n");
			source.Append("\t}\n");
			source.Append(thunks);
			source.Append("}\n");

			spc.AddSource(MetadataName(type) + ".LuaBindings.g.cs", SourceText.From(source.ToString(), Encoding.UTF8));
		}

		private static string LibraryName(IFieldSymbol field) {
			foreach (SyntaxReference reference in field.DeclaringSyntaxReferences) {
				if (reference.GetSyntax() is VariableDeclaratorSyntax declarator
					&& declarator.Initializer?.Value is BaseObjectCreationExpressionSyntax creation
					&& creation.ArgumentList != null
					&& creation.ArgumentList.Arguments.Count > 0
					&& creation.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax literal
					&& literal.Token.Value is string name)
					return name;
			}
			return field.Name;
		}

		private static string Literal(string value) => SymbolDisplay.FormatLiteral(value, true);

		private static string TypeName(ITypeSymbol type) => type.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

		private static bool IsLuaInterface(ITypeSymbol type) => type is INamedTypeSymbol named && MetadataName(named) == LuaInterfaceName;

		private static bool IsNumber(ITypeSymbol type) =>
			type.SpecialType == SpecialType.System_Double || type.SpecialType == SpecialType.System_Single || type.SpecialType == SpecialType.System_Int32 || type.SpecialType == SpecialType.System_Int64;

		private static bool IsPushableNumber(ITypeSymbol type) => IsNumber(type) || type.SpecialType == SpecialType.System_UInt32;

		private static string? NumberLiteral(object? value, ITypeSymbol type) {
			switch (value) {
				case double d:
					if (double.IsNaN(d))
						return "double.NaN";
					if (double.IsPositiveInfinity(d))
						return "double.PositiveInfinity";
					if (double.IsNegativeInfinity(d))
						return "double.NegativeInfinity";
					return d.ToString("R", CultureInfo.InvariantCulture) + "d";
				case float f:
					if (float.IsNaN(f))
						return "float.NaN";
					if (float.IsPositiveInfinity(f))
						return "float.PositiveInfinity";
					if (float.IsNegativeInfinity(f))
						return "float.NegativeInfinity";
					return f.ToString("R", CultureInfo.InvariantCulture) + "f";
				case int i:
					return i.ToString(CultureInfo.InvariantCulture);
				case long l:
					return l.ToString(CultureInfo.InvariantCulture) + "L";
				default:
					return null;
			}
		}

		private static string? BuildBody(SourceProductionContext spc, IMethodSymbol method, Location location, INamedTypeSymbol type, Dictionary<ITypeSymbol, LuaClassInfo> classMap) {
			if (!method.IsStatic || method.IsGenericMethod || method.ReturnsByRef || method.ReturnsByRefReadonly) {
				spc.ReportDiagnostic(Diagnostic.Create(BadMethod, location, method.Name, "Lua bindings must be static, non-generic and must not return by reference"));
				return null;
			}

			if (method.ReturnType.SpecialType == SpecialType.System_Int32 && method.Parameters.Length == 1 && method.Parameters[0].RefKind == RefKind.None && IsLuaInterface(method.Parameters[0].Type))
				return "\t\t\treturn " + method.Name + "(lua);\n";

			StringBuilder body = new StringBuilder();
			List<string> arguments = new List<string>();
			int slot = 1;
			bool ok = true;

			for (int index = 0; index < method.Parameters.Length; index++) {
				IParameterSymbol parameter = method.Parameters[index];
				ITypeSymbol parameterType = parameter.Type;
				string local = "arg" + index;
				string modifier = parameter.RefKind == RefKind.Ref ? "ref " : parameter.RefKind == RefKind.In || parameter.RefKind == RefKind.RefReadOnlyParameter ? "in " : "";

				if (IsLuaInterface(parameterType)) {
					arguments.Add("lua");
					continue;
				}

				if (parameter.RefKind == RefKind.Out) {
					Report(spc, parameter, method, location, "out parameters are not supported");
					ok = false;
					continue;
				}

				bool get = false;
				bool opt = false;
				TypedConstant? optValue = null;
				ITypeSymbol? optType = null;
				string? validate = null;
				foreach (AttributeData attribute in parameter.GetAttributes()) {
					if (Is(attribute, GetAttribute))
						get = true;
					else if (Is(attribute, OptAttribute))
						opt = true;
					else if (Is(attribute, OptGenericAttribute)) {
						opt = true;
						optType = attribute.AttributeClass!.TypeArguments[0];
						if (attribute.ConstructorArguments.Length == 1)
							optValue = attribute.ConstructorArguments[0];
					}
					else if (Is(attribute, ValidateAttribute) && attribute.ConstructorArguments.Length == 1)
						validate = attribute.ConstructorArguments[0].Value as string;
				}

				if (get && opt) {
					Report(spc, parameter, method, location, "[LuaGet] and [LuaOpt] cannot be combined");
					ok = false;
					continue;
				}

				if (optType != null && !SymbolEqualityComparer.Default.Equals(optType, parameterType)) {
					Report(spc, parameter, method, location, $"[LuaOpt<{optType.ToDisplayString()}>] does not match the parameter type '{parameterType.ToDisplayString()}'");
					ok = false;
					continue;
				}

				int n = slot++;
				string typeName = TypeName(parameterType);

				if (IsNumber(parameterType)) {
					string read;
					if (get)
						read = $"lua.GetNumber({n})";
					else if (opt) {
						string? literal = optValue.HasValue ? NumberLiteral(optValue.Value.Value, parameterType) : "0";
						if (literal == null) {
							Report(spc, parameter, method, location, "the [LuaOpt] default is not a number");
							ok = false;
							continue;
						}
						read = $"lua.CheckNumberOpt({n}, {literal})";
					}
					else
						read = $"lua.CheckNumber({n})";

					switch (parameterType.SpecialType) {
						case SpecialType.System_Single:
							read = "(float)" + read;
							break;
						case SpecialType.System_Int32:
							read = $"LuaHelper.cvttsd2si({read})";
							break;
						case SpecialType.System_Int64:
							read = $"LuaHelper.cvttsd2si64({read})";
							break;
					}
					body.Append($"\t\t\t{typeName} {local} = {read};\n");
				}
				else if (parameterType.SpecialType == SpecialType.System_Boolean) {
					if (!get) {
						Report(spc, parameter, method, location, "bool has no checked read; mark it [LuaGet] (GetBool)");
						ok = false;
						continue;
					}
					body.Append($"\t\t\tbool {local} = lua.GetBool({n});\n");
				}
				else if (parameterType.SpecialType == SpecialType.System_String) {
					if (get)
						body.Append($"\t\t\tstring? {local} = lua.GetString({n});\n");
					else if (opt && optValue.HasValue)
						body.Append($"\t\t\tstring {local} = lua.CheckStringOpt({n}, {(optValue.Value.Value is string s ? Literal(s) : "null")});\n");
					else if (opt)
						body.Append($"\t\t\tstring? {local} = lua.GetType({n}) <= {LuaTypeNil} ? null : lua.CheckString({n});\n");
					else
						body.Append($"\t\t\tstring {local} = lua.CheckString({n});\n");
				}
				else if (classMap.TryGetValue(parameterType, out LuaClassInfo? info)) {
					string field = TypeName(info.Owner) + "." + info.Field;
					if (get || optValue.HasValue) {
						Report(spc, parameter, method, location, "userdata parameters only support [LuaOpt] without a value");
						ok = false;
						continue;
					}

					if (info.IsValue) {
						if (opt) {
							if (parameter.RefKind != RefKind.None) {
								Report(spc, parameter, method, location, "an optional value userdata cannot be passed by reference");
								ok = false;
								continue;
							}
							body.Append($"\t\t\t{typeName} {local} = lua.GetType({n}) <= {LuaTypeNil} ? default : {field}.GetValue<{typeName}>({n});\n");
						}
						else if (parameter.RefKind == RefKind.None)
							body.Append($"\t\t\t{typeName} {local} = {field}.GetValue<{typeName}>({n});\n");
						else
							body.Append($"\t\t\tref {typeName} {local} = ref {field}.GetValue<{typeName}>({n});\n");
					}
					else {
						if (parameter.RefKind != RefKind.None) {
							Report(spc, parameter, method, location, "object userdata cannot be passed by reference");
							ok = false;
							continue;
						}

						if (opt)
							body.Append($"\t\t\t{typeName}? {local} = lua.GetType({n}) <= {LuaTypeNil} ? null : ({typeName}?){field}.Get({n});\n");
						else
							body.Append($"\t\t\t{typeName}? {local} = ({typeName}?){field}.Get({n});\n");

						if (!opt && parameter.NullableAnnotation != NullableAnnotation.Annotated) {
							if (info.NullError == null) {
								Report(spc, parameter, method, location, $"a non-nullable '{parameterType.ToDisplayString()}' needs NullError on its [LuaClass], or make the parameter nullable");
								ok = false;
								continue;
							}
							body.Append($"\t\t\tif ({local} == null)\n\t\t\t\tlua.Error({Literal(info.NullError)});\n");
						}
					}
				}
				else {
					Report(spc, parameter, method, location, $"'{parameterType.ToDisplayString()}' has no Lua conversion");
					ok = false;
					continue;
				}

				if (validate != null) {
					if (!type.GetMembers(validate).OfType<IMethodSymbol>().Any(m => m.IsStatic)) {
						Report(spc, parameter, method, location, $"[LuaValidate] method '{validate}' is not a static method of '{type.Name}'");
						ok = false;
						continue;
					}
					body.Append($"\t\t\t{validate}({local});\n");
				}

				arguments.Add(modifier + local);
			}

			if (!ok)
				return null;

			string call = method.Name + "(" + string.Join(", ", arguments) + ")";
			if (method.ReturnsVoid) {
				body.Append($"\t\t\t{call};\n");
				body.Append("\t\t\treturn 0;\n");
				return body.ToString();
			}

			ITypeSymbol returnType = method.ReturnType;
			body.Append($"\t\t\tvar result = {call};\n");

			if (returnType is INamedTypeSymbol nullable && nullable.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T) {
				body.Append("\t\t\tif (!result.HasValue)\n\t\t\t\treturn 0;\n");
				if (!Push(body, "result.Value", nullable.TypeArguments[0], classMap, "pushed")) {
					spc.ReportDiagnostic(Diagnostic.Create(BadReturn, location, method.Name, returnType.ToDisplayString()));
					return null;
				}
				body.Append("\t\t\treturn 1;\n");
				return body.ToString();
			}

			if (returnType.IsTupleType && returnType is INamedTypeSymbol tuple) {
				ImmutableArray<IFieldSymbol> elements = tuple.TupleElements;
				for (int i = 0; i < elements.Length; i++) {
					if (!Push(body, "result.Item" + (i + 1), elements[i].Type, classMap, "pushed" + i)) {
						spc.ReportDiagnostic(Diagnostic.Create(BadReturn, location, method.Name, returnType.ToDisplayString()));
						return null;
					}
				}
				body.Append($"\t\t\treturn {elements.Length};\n");
				return body.ToString();
			}

			if (returnType.IsReferenceType && method.ReturnNullableAnnotation == NullableAnnotation.Annotated)
				body.Append("\t\t\tif (result == null)\n\t\t\t\treturn 0;\n");

			if (!Push(body, "result", returnType, classMap, "pushed")) {
				spc.ReportDiagnostic(Diagnostic.Create(BadReturn, location, method.Name, returnType.ToDisplayString()));
				return null;
			}
			body.Append("\t\t\treturn 1;\n");
			return body.ToString();
		}

		private static bool Push(StringBuilder body, string value, ITypeSymbol type, Dictionary<ITypeSymbol, LuaClassInfo> classMap, string local) {
			if (IsPushableNumber(type))
				body.Append($"\t\t\tlua.PushNumber({value});\n");
			else if (type.SpecialType == SpecialType.System_Boolean)
				body.Append($"\t\t\tlua.PushBool({value});\n");
			else if (type.SpecialType == SpecialType.System_String)
				body.Append($"\t\t\tlua.PushString({value});\n");
			else if (classMap.TryGetValue(type, out LuaClassInfo? info)) {
				string field = TypeName(info.Owner) + "." + info.Field;
				if (info.IsValue) {
					body.Append($"\t\t\t{TypeName(type)} {local} = {value};\n");
					body.Append($"\t\t\tlua.PushValueUserType(in {local}, {field}.Type);\n");
				}
				else
					body.Append($"\t\t\t{field}.Push({value});\n");
			}
			else
				return false;
			return true;
		}

		private static void Report(SourceProductionContext spc, IParameterSymbol parameter, IMethodSymbol method, Location location, string message) =>
			spc.ReportDiagnostic(Diagnostic.Create(BadParameter, parameter.Locations.FirstOrDefault() ?? location, parameter.Name, method.Name, message));
	}
}
