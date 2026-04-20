using Content.Shared._RuMC14.Ordnance;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client._RuMC14.Ordnance;

[UsedImplicitly]
public sealed class RuMCExplosionSimulatorBui : BoundUserInterface
{
    private RuMCExplosionSimulatorWindow? _window;
    private RMCExplosionSimulatorBuiState? _lastState;

    public RuMCExplosionSimulatorBui(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<RuMCExplosionSimulatorWindow>();

        _window.MarinesButton.OnPressed += _ =>
            SendPredictedMessage(new RMCExplosionSimulatorTargetMsg(RMCExplosionSimulatorTarget.Marines));
        _window.SpecialForcesButton.OnPressed += _ =>
            SendPredictedMessage(new RMCExplosionSimulatorTargetMsg(RMCExplosionSimulatorTarget.SpecialForces));
        _window.XenomorphsButton.OnPressed += _ =>
            SendPredictedMessage(new RMCExplosionSimulatorTargetMsg(RMCExplosionSimulatorTarget.Xenomorphs));

        _window.SimulateButton.OnPressed += _ =>
            SendPredictedMessage(new RMCExplosionSimulatorSimulateMsg());
        _window.ReplayButton.OnPressed += _ =>
            SendPredictedMessage(new RMCExplosionSimulatorReplayMsg());

        _window.OnFrameUpdate += OnFrameUpdate;
    }

    private void OnFrameUpdate(FrameEventArgs args)
    {
        if (_window == null || _lastState == null || !_lastState.IsProcessing)
            return;

        // Tick down the countdown label in real time between server updates
        var secsLeft = _lastState.ProcessingSecsLeft - (int)(args.DeltaSeconds);
        if (secsLeft >= 0)
            _window.StatusLabel.Text = $"Processing… {_lastState.ProcessingSecsLeft}s remaining";
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (_window == null || state is not RMCExplosionSimulatorBuiState s)
            return;

        _lastState = s;

        _window.BeakerLabel.Text = s.HasBeaker ? "Beaker: Inserted" : "Beaker: Not inserted";

        _window.MarinesButton.Pressed = s.Target == RMCExplosionSimulatorTarget.Marines;
        _window.SpecialForcesButton.Pressed = s.Target == RMCExplosionSimulatorTarget.SpecialForces;
        _window.XenomorphsButton.Pressed = s.Target == RMCExplosionSimulatorTarget.Xenomorphs;

        _window.SimulateButton.Disabled = s.IsProcessing || !s.HasBeaker;

        if (s.IsProcessing)
        {
            _window.StatusLabel.Text = $"Processing… {s.ProcessingSecsLeft}s remaining";
            _window.StatusLabel.FontColorOverride = Color.FromHex("#ffcc44");
        }
        else if (s.SimulationReady)
        {
            _window.StatusLabel.Text = "Simulation complete. Press Replay to view.";
            _window.StatusLabel.FontColorOverride = Color.FromHex("#88dd88");
        }
        else
        {
            _window.StatusLabel.Text = "Insert a beaker and press Simulate.";
            _window.StatusLabel.FontColorOverride = Color.FromHex("#888888");
        }

        if (s.SimulationReady)
        {
            var approxRadius = s.HasExplosion
                ? MathF.Sqrt(MathF.Max(1f, s.TotalIntensity) / MathF.Max(1.5f, s.IntensitySlope))
                : 0f;

            _window.BlastLabel.Text = s.HasExplosion
                ? $"Power {(int)s.TotalIntensity}   Falloff {s.IntensitySlope:F1}   Radius ~{approxRadius:F1} tiles"
                : "No explosive chemicals.";

            _window.FireLabel.Text = s.HasFire
                ? $"Intensity {(int)s.FireIntensity}   Radius {(int)s.FireRadius} tiles   Duration {(int)s.FireDuration}s"
                : "No incendiary chemicals.";

            _window.ResultsBox.Visible = true;
        }
        else
        {
            _window.ResultsBox.Visible = false;
        }

        _window.ReplayButton.Disabled = !s.SimulationReady || s.IsProcessing;
    }
}
