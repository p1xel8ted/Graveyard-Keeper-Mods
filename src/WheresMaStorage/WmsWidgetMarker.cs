namespace WheresMaStorage;

public enum WmsWidgetKind
{
    Unknown,
    PersonalInventory,
    Toolbelt,
    Bag,
    SoulContainer,
    Stockpile,
    Tavern,
    WarehouseShop,
    WritersTable,
    VendorOffer,
    RefugeeStructure,
    AlwaysHide,
}

public sealed class WmsWidgetMarker : MonoBehaviour
{
    public WmsWidgetKind Kind = WmsWidgetKind.Unknown;
    public bool ShouldHide;
}
