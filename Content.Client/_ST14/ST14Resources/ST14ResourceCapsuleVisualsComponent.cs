namespace Content.Client._ST14.ST14Resources;

[RegisterComponent]
public sealed partial class ST14ResourceCapsuleVisualsComponent : Component
{
    [DataField]
    public string Layer = "fill";

    [DataField]
    public int MaxFillLevels = 4;

    [DataField]
    public string FillBaseName = "fill-";
}
