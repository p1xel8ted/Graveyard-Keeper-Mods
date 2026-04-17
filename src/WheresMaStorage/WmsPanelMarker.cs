namespace WheresMaStorage;

public enum WmsPanelKind
{
    Unknown,
    Player,
    Vendor,
    Chest,
    Resource
}

public sealed class WmsPanelMarker : MonoBehaviour
{
    public WmsPanelKind Kind = WmsPanelKind.Unknown;
    public string InteractionObjId = string.Empty;
}
