using System;
using Mikew.BirdsOfAFeather.Helpers;
using WildEggsHatching.Application.Facade;
using WildEggsHatching.Core.Enums;

namespace WildEggsHatching.Infrastructure.Commands;

public sealed class WeggCommandDispatcher
{
    private readonly IModFacade _facade;
    private readonly I18TranslationHelper? _i18n;

    public WeggCommandDispatcher(IModFacade facade, I18TranslationHelper? i18n = null)
    {
        _facade = facade;
        _i18n = i18n;
    }

    public CommandResult Execute(string[] args)
    {
        if (args.Length == 0 || IsHelp(args[0]))
            return new CommandResult(true, HelpText());

        var verb = args[0].ToLowerInvariant();

        return verb switch
        {
            "help" => new CommandResult(true, HelpText()),
            "reload" => ToCommandResult(_facade.ReloadAll()),
            "status" => HandleStatus(),
            "step" => HandleStep(args),
            "progression" => HandleProgression(args),
            "spawn" => HandleSpawn(args),
            "notes" => HandleNotes(args),
            "schema" => HandleSchema(args),
            "verify" => ToCommandResult(_facade.Verify(args)),
            _ => new CommandResult(false, $"{T("command.wegg.error.unknownSubcommand", "Unknown subcommand '{0}'.", args[0])}\n\n{HelpText()}"),
        };
    }

    private CommandResult HandleStep(string[] args)
    {
        if (args.Length < 2)
            return new CommandResult(false, $"{T("command.wegg.error.step.missing", "Missing step subcommand.")}\n\n{HelpText()}");

        var sub = args[1].ToLowerInvariant();
        return sub switch
        {
            "get" => new CommandResult(true, T("command.wegg.step.get", "ProgressionStep={0}", _facade.GetProgressionStep())),
            "set" => HandleStepSet(args),
            _ => new CommandResult(false, $"{T("command.wegg.error.step.unknown", "Unknown step subcommand '{0}'.", args[1])}\n\n{HelpText()}"),
        };
    }

    private CommandResult HandleStepSet(string[] args)
    {
        if (args.Length < 3)
            return new CommandResult(false, $"{T("command.wegg.usage.step.set", "Usage: wegg step set <PartI|PartII|...|PartVI> [--effects]")}\n\n{HelpText()}");

        if (!Enum.TryParse<ProgressionStep>(args[2], ignoreCase: true, out var step))
            return new CommandResult(false, $"{T("command.wegg.error.step.invalid", "Invalid step '{0}'.", args[2])}\n\n{HelpText()}");

        var applyEffects = Array.Exists(args, x => string.Equals(x, "--effects", StringComparison.OrdinalIgnoreCase));
        var res = _facade.TrySetProgressionStep(step, applyEffects, reason: "console command");

        return ToCommandResult(res);
    }

    private CommandResult HandleProgression(string[] args)
    {
        if (args.Length < 2)
            return new CommandResult(false, $"{T("command.wegg.error.progression.missing", "Missing progression subcommand.")}\n\n{HelpText()}");

        var sub = args[1].ToLowerInvariant();
        return sub switch
        {
            "eval" => new CommandResult(true, T("command.wegg.step.get", "ProgressionStep={0}", _facade.GetProgressionStep())),
            _ => new CommandResult(false, $"{T("command.wegg.error.progression.unknown", "Unknown progression subcommand '{0}'.", args[1])}\n\n{HelpText()}"),
        };
    }

    private CommandResult HandleSpawn(string[] args)
    {
        if (args.Length < 2)
            return new CommandResult(false, $"{T("command.wegg.error.spawn.missing", "Missing spawn subcommand.")}\n\n{HelpText()}");

        var sub = args[1].ToLowerInvariant();
        return sub switch
        {
            "now" => ToCommandResult(_facade.TryRunSpawnPlanNow()),
            "place" => HandleSpawnPlace(args),
            _ => new CommandResult(false, $"{T("command.wegg.error.spawn.unknown", "Unknown spawn subcommand '{0}'.", args[1])}\n\n{HelpText()}"),
        };
    }

    private CommandResult HandleSpawnPlace(string[] args)
    {
        if (args.Length < 4)
            return new CommandResult(false, $"{T("command.wegg.usage.spawn.place", "Usage: wegg spawn place <LocationName> <QualifiedItemId> [count] [maxAttempts]")}\n\n{HelpText()}");

        var location = args[2];
        var itemId = args[3];

        var count = 1;
        if (args.Length >= 5 && !int.TryParse(args[4], out count))
            return new CommandResult(false, $"{T("command.wegg.error.spawn.invalidCount", "Invalid count '{0}'.", args[4])}\n\n{HelpText()}");

        var maxAttempts = 400;
        if (args.Length >= 6 && !int.TryParse(args[5], out maxAttempts))
            return new CommandResult(false, $"{T("command.wegg.error.spawn.invalidMaxAttempts", "Invalid maxAttempts '{0}'.", args[5])}\n\n{HelpText()}");

        return ToCommandResult(_facade.TryPlaceItem(location, itemId, count, maxAttempts));
    }

    private CommandResult HandleNotes(string[] args)
    {
        if (args.Length < 2)
            return new CommandResult(false, $"{T("command.wegg.error.notes.missing", "Missing notes subcommand.")}\n\n{HelpText()}");

        var sub = args[1].ToLowerInvariant();
        return sub switch
        {
            "dump" => HandleNotesDump(args),
            "validate" => ToCommandResult(_facade.ValidateAnimalNotes()),
            "repair" => HandleNotesRepair(args),
            _ => new CommandResult(false, $"{T("command.wegg.error.notes.unknown", "Unknown notes subcommand '{0}'.", args[1])}\n\n{HelpText()}"),
        };
    }

    private CommandResult HandleNotesDump(string[] args)
    {
        var asJson = Array.Exists(args, x => string.Equals(x, "--json", StringComparison.OrdinalIgnoreCase));

        long? farmerId = null;
        var farmerIdx = Array.FindIndex(args, x => string.Equals(x, "--farmer", StringComparison.OrdinalIgnoreCase));
        if (farmerIdx >= 0)
        {
            if (farmerIdx + 1 >= args.Length || !long.TryParse(args[farmerIdx + 1], out var parsed))
                return new CommandResult(false, T("command.wegg.usage.notes.dump", "Usage: wegg notes dump [--json] [--farmer <id>] [--animal <token>]"));

            farmerId = parsed;
        }

        string? token = null;
        var tokenIdx = Array.FindIndex(args, x => string.Equals(x, "--animal", StringComparison.OrdinalIgnoreCase));
        if (tokenIdx >= 0)
        {
            if (tokenIdx + 1 >= args.Length)
                return new CommandResult(false, T("command.wegg.usage.notes.dump", "Usage: wegg notes dump [--json] [--farmer <id>] [--animal <token>]"));

            token = args[tokenIdx + 1];
        }

        return ToCommandResult(_facade.DumpAnimalNotes(new WildEggsHatching.Application.AnimalNotes.AnimalNotesDumpOptions(
            AsJson: asJson,
            FarmerId: farmerId,
            AnimalToken: token
        )));
    }

    private CommandResult HandleNotesRepair(string[] args)
    {
        var requeueMail = !Array.Exists(args, x => string.Equals(x, "--no-mail", StringComparison.OrdinalIgnoreCase));
        var reconcile = !Array.Exists(args, x => string.Equals(x, "--no-reconcile", StringComparison.OrdinalIgnoreCase));
        var force = Array.Exists(args, x => string.Equals(x, "--force", StringComparison.OrdinalIgnoreCase));

        return ToCommandResult(_facade.RepairAnimalNotes(requeueMail, reconcile, force));
    }

    private CommandResult HandleSchema(string[] args)
    {
        if (args.Length < 2)
            return new CommandResult(false, $"{T("command.wegg.error.schema.missing", "Missing schema subcommand.")}\n\n{HelpText()}");

        var sub = args[1].ToLowerInvariant();
        return sub switch
        {
            "wipe" => ToCommandResult(_facade.WipeSchemaData(
                confirmed: Array.Exists(args, x => string.Equals(x, "--yes", StringComparison.OrdinalIgnoreCase)))),
            _ => new CommandResult(false, $"{T("command.wegg.error.schema.unknown", "Unknown schema subcommand '{0}'.", args[1])}\n\n{HelpText()}"),
        };
    }

    private CommandResult HandleStatus()
    {
        var cfg = _facade.GetConfigSnapshot();
        var lines = new[]
        {
            $"ProgressionStep={_facade.GetProgressionStep()}",
            $"ProgressionMode={cfg.ProgressionMode}",
            $"DesertTombEnabled={cfg.DesertTombModuleEnabled}",
            $"AnimalNotesDebugMode={cfg.AnimalNotesDebugMode}",
            $"QuestCompletionTracked={_facade.GetQuestCompletionDays().Count}",
        };

        return new CommandResult(true, string.Join(Environment.NewLine, lines));
    }


    private string T(string key, string fallback, params object[] tokens)
    {
        var translated = _i18n is null
            ? key
            : tokens.Length == 0
                ? _i18n.GetString(key)
                : _i18n.GetString(key, tokens);

        if (!string.IsNullOrWhiteSpace(translated) && !string.Equals(translated, key, StringComparison.OrdinalIgnoreCase))
            return translated;

        return tokens.Length == 0 ? fallback : string.Format(fallback, tokens);
    }

    private string HelpText()
        => _i18n?.GetOrDefault("command.wegg.help", DefaultHelpText()) ?? DefaultHelpText();

    private static string DefaultHelpText()
        => string.Join(
            Environment.NewLine,
            "WildEggsHatching commands:",
            "  wegg help",
            "  wegg status",
            "  wegg reload",
            "  wegg step get",
            "  wegg step set <PartI|PartII|PartIII|PartIV|PartV|PartVI> [--effects]",
            "  wegg progression eval",
            "  wegg spawn now",
            "  wegg spawn place <LocationName> <QualifiedItemId> [count] [maxAttempts]",
            "  wegg notes dump [--json] [--farmer <id>] [--animal <token>]",
            "  wegg notes validate",
            "  wegg notes repair [--force] [--no-mail] [--no-reconcile]",
            "  wegg schema wipe --yes",
            "  wegg verify [--json] [--full]",
            "  wegg_notes dump [--json] [--farmer <id>] [--animal <token>]",
            "  wegg_notes validate",
            "  wegg_notes repair [--force] [--no-mail] [--no-reconcile]",
            "  wegg_schema wipe --yes",
            "",
            "Notes:",
            "  - Host-only mutations; clients will be rejected.",
            "  - QualifiedItemId example: (O)390",
            "  - step set defaults to no side effects; use --effects to replay step mail effects."
        );


    private static bool IsHelp(string arg)
        => arg is "-h" or "--help" or "help" or "?";

    private static CommandResult ToCommandResult(FacadeResult res)
        => new(res.Success, res.Message);
}
