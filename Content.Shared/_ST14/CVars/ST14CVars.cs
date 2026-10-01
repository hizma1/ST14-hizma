using Robust.Shared.Configuration;

namespace Content.Shared._ST14.CCVar;

[CVarDefs]
public static partial class ST14CVars
{
    //game-wide st14 difficulty cvar
    public static readonly CVarDef<float> DifficultyMultiplier =
        CVarDef.Create("st14.difficulty_multiplier", 1f, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    public static readonly CVarDef<string> ClientLanguage =
        CVarDef.Create("st14.language", "", CVar.ARCHIVE);

    public static readonly CVarDef<string> ServerLanguage =
        CVarDef.Create("st14.server_language", "", CVar.ARCHIVE);
}
