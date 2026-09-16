namespace Sipitex.Infrastructure.Persistence;

public static class PostgresDefaults
{
    public const string LocalConnectionString =
        "Host=localhost;Port=5432;Database=sipitex;Username=sipitex;Password=sipitex";

    public static void EnableCompatibilitySwitches()
    {
        // DateTime.Now / Unspecified del código legado (SQLite era TEXT).
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }
}
