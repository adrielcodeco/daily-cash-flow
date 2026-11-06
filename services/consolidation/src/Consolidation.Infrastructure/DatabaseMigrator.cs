// @anchors
//   code: CNMIG
//   layer: infrastructure
//   updated_at: 2026-11-05
using DbUp;

namespace Consolidation.Infrastructure;

public static class DatabaseMigrator
{
    public static void Run(string connectionString)
    {
        var result = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(typeof(DatabaseMigrator).Assembly)
            .WithTransactionPerScript()
            .LogToConsole()
            .Build()
            .PerformUpgrade();

        if (!result.Successful)
        {
            throw new InvalidOperationException("Database migration failed.", result.Error);
        }
    }
}
