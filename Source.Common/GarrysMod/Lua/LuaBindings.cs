namespace Source.Common.GarrysMod.Lua;

/// <summary>
/// Marks a <c>LuaClass</c> field as the metatable that <see cref="LuaMethodAttribute"/> methods in the same type register into,
/// and maps <paramref name="type"/> to it so any binding parameter or return value of that type is read with
/// <c>LuaClass.Get</c>/<c>GetValue</c> and pushed with <c>LuaClass.Push</c>/<c>PushValueUserType</c>.
/// </summary>
/// <param name="type">
/// The C# type stored in the userdata. Unmanaged structs are value userdata, everything else is object userdata.
/// Null registers methods without mapping a type, for userdata with its own accessors (ie. entities, which store a handle).
/// </param>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public sealed class LuaClassAttribute(Type? type = null) : Attribute
{
	public Type? Type { get; } = type;

	/// <summary>
	/// Raised with <c>ILuaInterface.Error</c> when a non-nullable object userdata parameter resolves to null (ie. "Tried to use a NULL ConVar!").
	/// Nullable parameters skip the check.
	/// </summary>
	public string? NullError { get; set; }
}

/// <summary>
/// Marks a <c>LuaLibrary</c> field as the table that <see cref="LuaFunctionAttribute"/> methods in the same type register into.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public sealed class LuaLibraryAttribute : Attribute;

/// <summary>
/// Registers the method on the type's <see cref="LuaClassAttribute"/> metatable. The generator emits the native
/// <c>{Method}__Redirect</c> entry point that reads the arguments, calls the method and pushes the result.
/// </summary>
/// <param name="name">The Lua name. Defaults to the method name after its first <c>__</c> (<c>ConVar__GetName</c> is <c>GetName</c>).</param>
/// <remarks>
/// A method shaped <c>static int Name(ILuaInterface lua)</c> is a raw binding: it is called as is and returns its own result count.
/// Otherwise every parameter consumes the next stack slot in order (an <c>ILuaInterface</c> parameter consumes none), and the
/// return value is pushed: <c>void</c> returns nothing, a tuple pushes each element, and a null <c>Nullable&lt;T&gt;</c> or
/// nullable reference returns nothing.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class LuaMethodAttribute(string? name = null) : Attribute
{
	public string? Name { get; } = name;
}

/// <summary>
/// Registers the method on the type's <see cref="LuaLibraryAttribute"/> table, with a generated <c>redir__{library}__{name}</c> entry point.
/// Parameters and return values follow the same rules as <see cref="LuaMethodAttribute"/>.
/// </summary>
/// <param name="name">The Lua name. Defaults to the method name.</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class LuaFunctionAttribute(string? name = null) : Attribute
{
	public string? Name { get; } = name;
}

/// <summary>
/// Registers the method in <c>_G</c> through <c>LuaGlobalLibrary</c>, with a generated <c>redir__GLobal__{name}</c> entry point.
/// Parameters and return values follow the same rules as <see cref="LuaMethodAttribute"/>.
/// </summary>
/// <param name="name">The Lua name. Defaults to the method name.</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class LuaGlobalAttribute(string? name = null) : Attribute
{
	public string? Name { get; } = name;
}

/// <summary>
/// Reads the parameter without type checking, like the C++ <c>Get*</c> calls: <c>GetNumber</c> (anything that isn't a number is 0),
/// <c>GetBool</c> (only nil and false are false) or <c>GetString</c> (null if it isn't a string or number). Never raises an error.
/// <c>bool</c> parameters have no checked form and always need this attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class LuaGetAttribute : Attribute;

/// <summary>
/// Makes the parameter optional: when the argument is none or nil the parameter gets <c>default(T)</c> (0, null, a zero vector),
/// otherwise it is checked exactly like a required parameter, so a value of the wrong type still raises
/// "bad argument #n (... expected, got ...)". Numbers use <c>CheckNumberOpt(n, 0)</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class LuaOptAttribute : Attribute;

/// <summary>
/// Makes the parameter optional with an explicit default: when the argument is none or nil the parameter gets <paramref name="value"/>,
/// otherwise it is checked exactly like a required parameter. Numbers and strings compile to <c>CheckNumberOpt(n, value)</c> and
/// <c>CheckStringOpt(n, value)</c>. <typeparamref name="T"/> must match the parameter type, and since attribute arguments must be
/// constants only numbers, strings, bools and enums can be given; anything else needs a raw binding.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class LuaOptAttribute<T>(T value) : Attribute
{
	public T Value { get; } = value;
}

/// <summary>
/// Calls the named static method with the parameter right after it is read and before the next argument is touched,
/// for bindings that validate an argument before reading the rest (ie. <c>CheckLuaConVar</c> before the new value).
/// </summary>
/// <param name="method">A static method in the same type taking the parameter's type.</param>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class LuaValidateAttribute(string method) : Attribute
{
	public string Method { get; } = method;
}
