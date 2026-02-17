using System;
using WildEggsHatching.Application.Facade;
using WildEggsHatching.Core.Enums;

namespace WildEggsHatching.Infrastructure.Commands;

public sealed class WeggCommandDispatcher
{
    private readonly IModFacade _facade;

    public WeggCommandDispatcher(IModFacade facade) => _facade = facade;

    public CommandResult Execute(string[] args)
    {
        if (args.Length == 0 || IsHelp(args[0]))
            return new CommandResult(true, HelpText());

        var verb = args[0].ToLowerInvariant();

        return verb switch
        {
            "help" => new CommandResult(true, HelpText()),
            "reload" => ToCommandResult(_facade.ReloadAll()),
            "step" => HandleStep(args),
            "progression" => HandleProgression(args),
            "spawn" => HandleSpawn(args),
            "notes" => HandleNotes(args),
            "schema" => HandleSchema(args),
            _ => new CommandResult(false, $"Unknown subcommand '{args[0]}'.\n\n{HelpText()}"),
        };
    }

    private CommandResult HandleStep(string[] args)
    {
        if (args.Length < 2)
            return new CommandResult(false, $"Missing step subcommand.\n\n{HelpText()}");

        var sub = args[1].ToLowerInvariant();
        return sub switch
        {
            "get" => new CommandResult(true, $"ProgressionStep={_facade.GetProgressionStep()}"),
            "set" => HandleStepSet(args),
            _ => new CommandResult(false, $"Unknown step subcommand '{args[1]}'.\n\n{HelpText()}"),
        };
    }

    private CommandResult HandleStepSet(string[] args)
    {
        if (args.Length < 3)
            return new CommandResult(false, $"Usage: wegg step set <PartI|PartII|...|PartVI> [--effects]\n\n{HelpText()}");

        if (!Enum.TryParse<ProgressionStep>(args[2], ignoreCase: true, out var step))
            return new CommandResult(false, $"Invalid step '{args[2]}'.\n\n{HelpText()}");

        var applyEffects = Array.Exists(args, x => string.Equals(x, "--effects", StringComparison.OrdinalIgnoreCase));
        var res = _facade.TrySetProgressionStep(step, applyEffects, reason: "console command");

        return ToCommandResult(res);
    }

    private CommandResult HandleProgression(string[] args)
    {
        if (args.Length < 2)
            return new CommandResult(false, $"Missing progression subcommand.\n\n{HelpText()}");

        var sub = args[1].ToLowerInvariant();
        return sub switch
        {
            "eval" => new CommandResult(true, $"ProgressionStep={_facade.GetProgressionStep()}"),
            _ => new CommandResult(false, $"Unknown progression subcommand '{args[1]}'.\n\n{HelpText()}"),
        };
    }

    private CommandResult HandleSpawn(string[] args)
    {
        if (args.Length < 2)
            return new CommandResult(false, $"Missing spawn subcommand.\n\n{HelpText()}");

        var sub = args[1].ToLowerInvariant();
        return sub switch
        {
            "now" => ToCommandResult(_facade.TryRunSpawnPlanNow()),
            "place" => HandleSpawnPlace(args),
            _ => new CommandResult(false, $"Unknown spawn subcommand '{args[1]}'.\n\n{HelpText()}"),
        };
    }

    private CommandResult HandleSpawnPlace(string[] args)
    {
        if (args.Length < 4)
            return new CommandResult(false, $"Usage: wegg spawn place <LocationName> <QualifiedItemId> [count] [maxAttempts]\n\n{HelpText()}");

        var location = args[2];
        var itemId = args[3];

        var count = 1;
        if (args.Length >= 5 && !int.TryParse(args[4], out count))
            return new CommandResult(false, $"Invalid count '{args[4]}'.\n\n{HelpText()}");

        var maxAttempts = 400;
        if (args.Length >= 6 && !int.TryParse(args[5], out maxAttempts))
            return new CommandResult(false, $"Invalid maxAttempts '{args[5]}'.\n\n{HelpText()}");

        return ToCommandResult(_facade.TryPlaceItem(location, itemId, count, maxAttempts));
    }

    private CommandResult HandleNotes(string[] args)
    {
        if (args.Length < 2)
            return new CommandResult(false, $"Missing notes subcommand.\n\n{HelpText()}");

        var sub = args[1].ToLowerInvariant();
        return sub switch
        {
            "dump" => HandleNotesDump(args),
            "validate" => ToCommandResult(_facade.ValidateAnimalNotes()),
            "repair" => HandleNotesRepair(args),
            _ => new CommandResult(false, $"Unknown notes subcommand '{args[1]}'.\n\n{HelpText()}"),
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
                return new CommandResult(false, "Usage: wegg notes dump [--json] [--farmer <id>] [--animal <token>]");

            farmerId = parsed;
        }

        string? token = null;
        var tokenIdx = Array.FindIndex(args, x => string.Equals(x, "--animal", StringComparison.OrdinalIgnoreCase));
        if (tokenIdx >= 0)
        {
            if (tokenIdx + 1 >= args.Length)
                return new CommandResult(false, "Usage: wegg notes dump [--json] [--farmer <id>] [--animal <token>]");

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

    private static bool IsHelp(string arg)
        => arg is "-h" or "--help" or "help" or "?";

    private static CommandResult ToCommandResult(FacadeResult res)
        => new(res.Success, res.Message);

    private static string HelpText()
        => string.Join(
            Environment.NewLine,
            "WildEggsHatching commands:",
            "  wegg help",
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
}
