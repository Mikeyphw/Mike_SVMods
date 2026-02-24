using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using RoosterControl.Application.Catalogs;
using RoosterControl.Application.Catalogs.Console;
using Xunit;

namespace RoosterControl.PureTests;

public sealed class RaCatalogValidateFixTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 02, 19, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Validate_NoFix_ReportsOkMigratedError_AndDoesNotWrite()
    {
        using var sandbox = new TempModSandbox();
        sandbox.SeedCanonicalCatalogs();

        sandbox.DeleteCatalogFile(CatalogCommandService.ProduceCatalogPath);
        sandbox.WriteCatalogText(CatalogCommandService.HatchingCatalogPath, "{ invalid json");

        var svc = new CatalogCommandService(sandbox.ModDir, now: () => FixedNow);
        var result = svc.ValidateAndMaybeFix(fix: false);

        Assert.Equal(3, result.Reports.Count);

        Assert.Equal(CatalogFileStatus.Ok, GetStatus(result, "roosters"));
        Assert.Equal(CatalogFileStatus.Migrated, GetStatus(result, "produce"));
        Assert.Equal(CatalogFileStatus.Error, GetStatus(result, "hatching"));

        Assert.False(Directory.Exists(Path.Combine(sandbox.ModDir, "data", "catalogs", "backups")));
        Assert.False(Directory.Exists(Path.Combine(sandbox.ModDir, "data", "catalogs", "invalid")));
    }

    [Fact]
    public void Validate_Fix_CreatesBackupsAndInvalidCopies_AndNormalizes()
    {
        using var sandbox = new TempModSandbox();
        sandbox.SeedCanonicalCatalogs();

        sandbox.DeleteCatalogFile(CatalogCommandService.ProduceCatalogPath);
        var badHatching = "{ invalid json";
        sandbox.WriteCatalogText(CatalogCommandService.HatchingCatalogPath, badHatching);

        var svc = new CatalogCommandService(sandbox.ModDir, now: () => FixedNow);
        var result = svc.ValidateAndMaybeFix(fix: true);

        Assert.Equal(CatalogFileStatus.Ok, GetStatus(result, "roosters"));
        Assert.Equal(CatalogFileStatus.Migrated, GetStatus(result, "produce"));
        Assert.Equal(CatalogFileStatus.Migrated, GetStatus(result, "hatching"));

        var backupDir = Path.Combine(sandbox.ModDir, result.BackupRelativeDir!.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(Directory.Exists(backupDir));

        Assert.True(File.Exists(Path.Combine(backupDir, "roosters.json")));
        Assert.True(File.Exists(Path.Combine(backupDir, "hatching.json")));
        Assert.False(File.Exists(Path.Combine(backupDir, "produce.json")));

        var invalidDir = Path.Combine(sandbox.ModDir, result.InvalidRelativeDir!.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(Directory.Exists(invalidDir));

        var invalidHatchingPath = Path.Combine(invalidDir, "hatching.json");
        Assert.True(File.Exists(invalidHatchingPath));
        Assert.Equal(badHatching, File.ReadAllText(invalidHatchingPath, Encoding.UTF8));

        // Normalized catalogs are valid JSON now.
        var produceFull = sandbox.GetCatalogFullPath(CatalogCommandService.ProduceCatalogPath);
        var hatchingFull = sandbox.GetCatalogFullPath(CatalogCommandService.HatchingCatalogPath);

        var produce = JsonSerializer.Deserialize<ProduceCatalog>(File.ReadAllText(produceFull, Encoding.UTF8));
        var hatching = JsonSerializer.Deserialize<HatchingCatalog>(File.ReadAllText(hatchingFull, Encoding.UTF8));

        Assert.NotNull(produce);
        Assert.NotNull(hatching);

        // After --fix, everything should validate cleanly.
        var post = svc.ValidateAndMaybeFix(fix: false);
        Assert.All(post.Reports, r => Assert.Equal(CatalogFileStatus.Ok, r.Status));
        Assert.Empty(post.Issues);
    }

    private static CatalogFileStatus GetStatus(CatalogValidateResult result, string name)
        => result.Reports.Single(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).Status;

    private sealed class TempModSandbox : IDisposable
    {
        public string ModDir { get; } = Path.Combine(Path.GetTempPath(), "RoosterControlTests", Guid.NewGuid().ToString("N"));

        public TempModSandbox()
        {
            Directory.CreateDirectory(ModDir);
            Directory.CreateDirectory(Path.Combine(ModDir, "data", "catalogs"));
        }

        public void SeedCanonicalCatalogs()
        {
            // Use the command service itself to create canonical files, then clear its backup/invalid folders.
            var svc = new CatalogCommandService(ModDir, now: () => new DateTimeOffset(2026, 02, 18, 0, 0, 0, TimeSpan.Zero));
            _ = svc.ValidateAndMaybeFix(fix: true);

            var backups = Path.Combine(ModDir, "data", "catalogs", "backups");
            var invalid = Path.Combine(ModDir, "data", "catalogs", "invalid");

            if (Directory.Exists(backups))
                Directory.Delete(backups, recursive: true);
            if (Directory.Exists(invalid))
                Directory.Delete(invalid, recursive: true);
        }

        public void DeleteCatalogFile(string relCatalogPath)
        {
            var full = GetCatalogFullPath(relCatalogPath);
            if (File.Exists(full))
                File.Delete(full);
        }

        public void WriteCatalogText(string relCatalogPath, string text)
        {
            var full = GetCatalogFullPath(relCatalogPath);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, text, Encoding.UTF8);
        }

        public string GetCatalogFullPath(string relCatalogPath)
            => Path.Combine(ModDir, "data", relCatalogPath.Replace('/', Path.DirectorySeparatorChar));

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(ModDir))
                    Directory.Delete(ModDir, recursive: true);
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }
}
