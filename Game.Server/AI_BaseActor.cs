namespace Game.Server;

public class AI_BaseActor : AI_ExpresserHost_AI_BaseHumanoid
{
	public override void Precache() => throw new NotImplementedException();

	public override void SetModel(ReadOnlySpan<char> modelName) => throw new NotImplementedException();

	public virtual AI_Expresser? GetExpresser() {
		return Expresser;
	}

	public override bool CreateComponents() {
		if (!base.CreateComponents())
			return false;

		Expresser = CreateExpresser();
		if (Expresser == null)
			return false;

		Expresser.Connect(this);

		return true;
	}

	public virtual AI_Expresser? CreateExpresser() {
		Expresser = new AI_Expresser(this);
		return Expresser;
	}

	AI_Expresser? Expresser;
}
