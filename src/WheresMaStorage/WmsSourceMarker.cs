namespace WheresMaStorage;

public enum WmsSourceKind
{
    Unknown,
    Player,
    Chest,
    SoulBox,
    TavernCellar,
    WritersTable,
    ChurchPulpit,
    Barman,
    Vendor,
    CraftStation,
    Well,
    Quarry,
    ZombieMill,
    Stockpile,
    Compost,
    Worker,
    ZombieWorker,
    RefugeeStructure,
    AlwaysSkip,
}

public sealed class WmsSourceMarker : MonoBehaviour
{
    public WmsSourceKind Kind = WmsSourceKind.Unknown;
}
