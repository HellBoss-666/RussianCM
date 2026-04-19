using Content.Client.Message;
using Content.Shared._RMC14.Ordnance;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Ordnance;

[UsedImplicitly]
public sealed class RMCDemolitionsSimulatorBui : BoundUserInterface
{
    private RMCDemolitionsSimulatorWindow? _window;

    public RMCDemolitionsSimulatorBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<RMCDemolitionsSimulatorWindow>();
        _window.SimulateButton.OnPressed += _ => SendPredictedMessage(new RMCDemolitionsSimulatorSimulateMsg());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || state is not RMCDemolitionsSimulatorBuiState s)
            return;

        _window.SimulateButton.Disabled = s.OnCooldown;

        if (s.OnCooldown)
        {
            _window.CooldownLabel.Visible = true;
            _window.CooldownLabel.Text = $"Processors cooling down... {s.CooldownSecsLeft}s remaining";
        }
        else
        {
            _window.CooldownLabel.Visible = false;
        }

        if (!string.IsNullOrWhiteSpace(s.Result))
            _window.ResultLabel.SetMarkup(s.Result);
        else
            _window.ResultLabel.SetMarkup("[color=gray]No simulation data.[/color]");
    }
}
