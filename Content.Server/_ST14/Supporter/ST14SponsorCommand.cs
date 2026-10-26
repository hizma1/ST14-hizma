using Content.Server.Administration;
using Content.Shared._ST14.Supporter;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Player;

namespace Content.Server._ST14.Supporter;

/// <summary>
/// Test tool: gives a player a supporter tier so the support screen and the perk locks can be checked.
/// Nothing is paid or saved, the tier is gone after a server restart
/// </summary>
[AdminCommand(AdminFlags.Admin)]
public sealed partial class ST14SponsorCommand : IConsoleCommand
{
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IPlayerManager _player = default!;

    public string Command => "st14_sponsor";
    public string Description => Loc.GetString("st14-sponsor-command-description");
    public string Help => Loc.GetString("st14-sponsor-command-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is < 1 or > 2 || !int.TryParse(args[0], out var tier) || tier < 0 || tier > ST14SupporterTierEvent.MaxTier)
        {
            shell.WriteError(Help);
            return;
        }

        ICommonSession? target = shell.Player;
        if (args.Length == 2 && !_player.TryGetSessionByUsername(args[1], out target))
        {
            shell.WriteError(Loc.GetString("st14-sponsor-command-no-player", ("player", args[1])));
            return;
        }

        if (target == null)
        {
            shell.WriteError(Help);
            return;
        }

        _entMan.System<ST14SupporterSystem>().SetTier(target, tier);
        shell.WriteLine(Loc.GetString("st14-sponsor-command-done", ("player", target.Name), ("tier", tier)));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(new[] { "0", "1", "2", "3", "4" }, Loc.GetString("st14-sponsor-command-arg-tier")),
            2 => CompletionResult.FromHintOptions(CompletionHelper.SessionNames(), Loc.GetString("st14-sponsor-command-arg-player")),
            _ => CompletionResult.Empty,
        };
    }
}
