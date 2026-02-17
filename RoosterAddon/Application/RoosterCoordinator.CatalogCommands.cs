using System;
using System.Linq;
using RoosterAddon.Framework.Config;
using StardewModdingAPI;

namespace RoosterAddon.Application;

internal sealed partial class RoosterCoordinator
{
    private void CatalogCommand(string command, string[] args)
    {
        var verb = args.FirstOrDefault()?.Trim() ?? "help";

        if (verb.Equals("help", StringComparison.OrdinalIgnoreCase) || verb.Equals("-h", StringComparison.OrdinalIgnoreCase))
        {
            Monitor.Log(
                "ra_catalog usage:\n" +
                "  ra_catalog reload\n" +
                "    - reload JSON catalogs from data/catalogs and re-apply per config General.CatalogApplyMode.\n" +
                "  ra_catalog export\n" +
                "    - export effective catalogs from current config to data/catalogs/*.json (overwrites).\n" +
                "  ra_catalog mode\n" +
                "    - show current CatalogApplyMode.\n" +
                "  ra_catalog mode <SeedOnly|RepairInvalid|MergeMissing|Authoritative>\n" +
                "    - set CatalogApplyMode and immediately apply it to the current config.",
                LogLevel.Info
            );
            return;
        }

        if (verb.Equals("reload", StringComparison.OrdinalIgnoreCase))
        {
            ReloadCatalogs();
            return;
        }

        if (verb.Equals("export", StringComparison.OrdinalIgnoreCase))
        {
            ExportCatalogs();
            return;
        }

        if (verb.Equals("mode", StringComparison.OrdinalIgnoreCase))
        {
            SetOrShowCatalogMode(args.Skip(1).ToArray());
            return;
        }

        Monitor.Log($"Unknown ra_catalog verb '{verb}'. Use: ra_catalog help", LogLevel.Info);
    }

    private void ReloadCatalogs()
    {
        try
        {
            _catalogs = _catalogManager.LoadOrCreateAll(Helper, Monitor);

            var changedConfig = _catalogManager.ApplyToConfig(
                config: Config,
                catalogs: _catalogs,
                isNewConfig: false,
                monitor: Monitor
            );

            if (changedConfig)
                Helper.WriteConfig(Config);

            RoosterHelper.UpdateRoosterCatalog(_catalogs.Roosters);

            InvalidateDailyPlanCache("Catalog reload");

            if (Context.IsWorldReady)
            {
                RequestInvalidate(FarmAnimalsAssetName);
                RequestInvalidate(ObjectsAssetName);

                try
                {
                    RunStartupValidationV2();
                }
                catch (Exception ex)
                {
                    Monitor.Log($"Catalog reload validation failed (fatal on startup): {ex.Message}", LogLevel.Error);
                }
            }

            Monitor.Log("Catalogs reloaded.", LogLevel.Info);
        }
        catch (Exception ex)
        {
            Monitor.Log($"ra_catalog reload failed: {ex}", LogLevel.Error);
        }
    }

    private void ExportCatalogs()
    {
        try
        {
            _catalogManager.ExportEffective(Helper, Monitor, Config, _catalogs);
        }
        catch (Exception ex)
        {
            Monitor.Log($"ra_catalog export failed: {ex}", LogLevel.Error);
        }
    }

    private void SetOrShowCatalogMode(string[] args)
    {
        if (args.Length == 0)
        {
            Monitor.Log($"CatalogApplyMode = {Config.General.CatalogApplyMode}", LogLevel.Info);
            return;
        }

        var raw = (args[0] ?? string.Empty).Trim();

        if (!TryParseCatalogApplyMode(raw, out var mode))
        {
            Monitor.Log($"Invalid CatalogApplyMode '{raw}'. Use: SeedOnly, RepairInvalid, MergeMissing, Authoritative.", LogLevel.Info);
            return;
        }

        Config.General.CatalogApplyMode = mode;
        Helper.WriteConfig(Config);

        var changedConfig = _catalogManager.ApplyToConfig(Config, _catalogs, isNewConfig: false, Monitor);
        if (changedConfig)
            Helper.WriteConfig(Config);

        if (Context.IsWorldReady)
        {
            InvalidateDailyPlanCache("Catalog mode changed");
            RequestInvalidate(FarmAnimalsAssetName);
            RequestInvalidate(ObjectsAssetName);
        }

        Monitor.Log($"CatalogApplyMode set to {mode}.", LogLevel.Info);
    }

    private static bool TryParseCatalogApplyMode(string raw, out CatalogApplyMode mode)
    {
        mode = CatalogApplyMode.RepairInvalid;

        if (string.IsNullOrWhiteSpace(raw))
            return false;

        raw = raw.Trim();

        if (Enum.TryParse(raw, ignoreCase: true, out CatalogApplyMode parsed))
        {
            mode = parsed;
            return true;
        }

        // friendly aliases
        if (raw.Equals("merge", StringComparison.OrdinalIgnoreCase) || raw.Equals("mergemissing", StringComparison.OrdinalIgnoreCase))
        {
            mode = CatalogApplyMode.MergeMissing;
            return true;
        }

        if (raw.Equals("authoritative", StringComparison.OrdinalIgnoreCase) || raw.Equals("overwrite", StringComparison.OrdinalIgnoreCase))
        {
            mode = CatalogApplyMode.Authoritative;
            return true;
        }

        if (raw.Equals("seed", StringComparison.OrdinalIgnoreCase))
        {
            mode = CatalogApplyMode.SeedOnly;
            return true;
        }

        if (raw.Equals("repair", StringComparison.OrdinalIgnoreCase))
        {
            mode = CatalogApplyMode.RepairInvalid;
            return true;
        }

        return false;
    }
}
