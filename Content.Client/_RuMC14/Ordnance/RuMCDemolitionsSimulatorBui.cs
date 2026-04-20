using Content.Client.Message;
using Content.Shared._RuMC14.Ordnance;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client._RuMC14.Ordnance;

[UsedImplicitly]
public sealed class RuMCDemolitionsSimulatorBui : BoundUserInterface
{
    // ── Grid constants ────────────────────────────────────────────────────────
    private const int GridSize = 17;
    private const int CellPx = 18;
    private const int Center = GridSize / 2;

    // ── Target definitions (Name, HP, armour coefficient 0-1) ─────────────────
    private static readonly (string Name, float Hp, float Armour)[] Targets =
    {
        ("Xenomorph Drone",      150f, 0.20f),
        ("Xenomorph Warrior",    300f, 0.30f),
        ("Xenomorph Crusher",    500f, 0.40f),
        ("Marine (no armour)",    80f, 0.00f),
        ("Marine (full armour)", 120f, 0.25f),
        ("Metal wall",           300f, 0.50f),
    };

    // ── Colours ───────────────────────────────────────────────────────────────
    private static readonly Color ColNone    = Color.FromHex("#1a1a1a");
    private static readonly Color ColLight   = Color.FromHex("#554400");
    private static readonly Color ColMedium  = Color.FromHex("#aa4400");
    private static readonly Color ColHeavy   = Color.FromHex("#dd1100");
    private static readonly Color ColCentre  = Color.FromHex("#ffee00");
    private static readonly Color ColFire    = Color.FromHex("#ff7700");
    private static readonly Color ColFireDim = Color.FromHex("#662200");
    private static readonly Color ColTarget  = Color.FromHex("#2255cc");
    private static readonly Color ColPending = Color.FromHex("#223344");

    // ── State ─────────────────────────────────────────────────────────────────
    private RuMCDemolitionsSimulatorWindow? _window;
    private PanelContainer[,]? _cells;
    private int _targetIndex;
    private RMCDemolitionsSimulatorBuiState? _pendingState;
    private float _animTimer;   // counts down from AnimDuration to 0
    private const float AnimDuration = 1.5f;

    public RuMCDemolitionsSimulatorBui(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    // ── Open ──────────────────────────────────────────────────────────────────

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<RuMCDemolitionsSimulatorWindow>();

        BuildGrid();

        _window.SimulateButton.OnPressed += _ =>
        {
            _animTimer = AnimDuration;
            SetGridPending();
            SendPredictedMessage(new RMCDemolitionsSimulatorSimulateMsg());
        };

        _window.TargetButton.OnPressed += _ =>
        {
            _targetIndex = (_targetIndex + 1) % Targets.Length;
            _window.TargetButton.Text = Targets[_targetIndex].Name;
            if (_pendingState != null && _animTimer <= 0f)
                RenderState(_pendingState);
        };

        _window.OnFrameUpdate += OnFrameUpdate;
    }

    // ── Grid construction ─────────────────────────────────────────────────────

    private void BuildGrid()
    {
        if (_window == null) return;

        _cells = new PanelContainer[GridSize, GridSize];
        var grid = _window.BlastGrid;
        grid.RemoveAllChildren();

        for (var row = 0; row < GridSize; row++)
        {
            for (var col = 0; col < GridSize; col++)
            {
                var cell = new PanelContainer
                {
                    SetWidth  = CellPx,
                    SetHeight = CellPx,
                };
                SetCellColor(cell, ColNone);
                grid.AddChild(cell);
                _cells[row, col] = cell;
            }
        }
    }

    private static void SetCellColor(PanelContainer cell, Color color)
    {
        cell.PanelOverride = new StyleBoxFlat { BackgroundColor = color };
    }

    // ── Animation ─────────────────────────────────────────────────────────────

    private void SetGridPending()
    {
        if (_cells == null) return;
        for (var r = 0; r < GridSize; r++)
            for (var c = 0; c < GridSize; c++)
                SetCellColor(_cells[r, c], ColPending);
    }

    private void OnFrameUpdate(FrameEventArgs args)
    {
        if (_animTimer <= 0f) return;

        _animTimer -= args.DeltaSeconds;

        // Subtle pulse during analysis: vary brightness using sin wave
        if (_cells != null && _animTimer > 0f)
        {
            float t = MathF.Abs(MathF.Sin(_animTimer * 4f));
            var pulse = Color.InterpolateBetween(ColPending, Color.FromHex("#3a5566"), t);
            for (var r = 0; r < GridSize; r++)
                for (var c = 0; c < GridSize; c++)
                    SetCellColor(_cells[r, c], pulse);
            return;
        }

        // Animation finished — show results
        _animTimer = 0f;
        if (_pendingState != null)
            RenderState(_pendingState);
    }

    // ── BUI state update ──────────────────────────────────────────────────────

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (_window == null || state is not RMCDemolitionsSimulatorBuiState s)
            return;

        _window.SimulateButton.Disabled = s.OnCooldown;

        if (s.OnCooldown)
        {
            _window.CooldownLabel.Visible = true;
            _window.CooldownLabel.Text = $"Cooling down… {s.CooldownSecsLeft}s";
        }
        else
        {
            _window.CooldownLabel.Visible = false;
        }

        _pendingState = s;

        // Respect animation: if still playing, wait; otherwise render immediately
        if (_animTimer <= 0f)
            RenderState(s);
    }

    // ── Render simulation results ─────────────────────────────────────────────

    private void RenderState(RMCDemolitionsSimulatorBuiState s)
    {
        if (_window == null || _cells == null) return;

        if (string.IsNullOrEmpty(s.CasingName))
        {
            _window.CasingLabel.Text = "No casing simulated.";
            _window.VolumeLabel.Text = string.Empty;
            _window.BlastStatsLabel.Text = "—";
            _window.FireStatsLabel.Text = "—";
            _window.DamageTable.SetMarkup("[color=gray]Hold a chembomb casing and press Simulate.[/color]");
            ClearGrid();
            return;
        }

        _window.CasingLabel.Text = $"Simulating: {s.CasingName}";
        _window.VolumeLabel.Text = $"Chemicals: {(int)s.CurrentVolume}/{(int)s.MaxVolume} u";

        var approxRadius = s.HasExplosion
            ? MathF.Sqrt(MathF.Max(1f, s.TotalIntensity) / MathF.Max(1.5f, s.IntensitySlope))
            : 0f;

        _window.BlastStatsLabel.Text = s.HasExplosion
            ? $"Power {(int)s.TotalIntensity}   Falloff {s.IntensitySlope:F1}   Radius ~{approxRadius:F1} tiles"
            : "No explosive chemicals.";

        _window.FireStatsLabel.Text = s.HasFire
            ? $"Intensity {(int)s.FireIntensity}   Radius {(int)s.FireRadius} tiles   Duration {(int)s.FireDuration}s"
            : "No incendiary chemicals.";

        ColorGrid(s);
        BuildDamageTable(s);
    }

    // ── Grid coloring ─────────────────────────────────────────────────────────

    private void ClearGrid()
    {
        if (_cells == null) return;
        for (var r = 0; r < GridSize; r++)
            for (var c = 0; c < GridSize; c++)
                SetCellColor(_cells[r, c], ColNone);
    }

    private void ColorGrid(RMCDemolitionsSimulatorBuiState s)
    {
        if (_cells == null) return;

        for (var row = 0; row < GridSize; row++)
        {
            for (var col = 0; col < GridSize; col++)
            {
                int dx = col - Center;
                int dy = row - Center;
                float dist = MathF.Sqrt(dx * dx + dy * dy);
                var color = ColNone;

                // Fire zone (base layer)
                if (s.HasFire && dist <= s.FireRadius)
                    color = dist <= s.FireRadius * 0.5f ? ColFire : ColFireDim;

                // Blast zone (overlays fire)
                if (s.HasExplosion)
                {
                    float intensity = IntensityAt(s, dist);
                    if (intensity > 0)
                    {
                        float norm = intensity / s.MaxIntensity;
                        color = norm >= 0.70f ? ColCentre
                              : norm >= 0.40f ? ColHeavy
                              : norm >= 0.15f ? ColMedium
                              : ColLight;
                    }
                }

                // Target markers — 6 targets along east cardinal axis
                if (dy == 0 && dx >= 1 && dx <= 6)
                    color = BlendColor(color, ColTarget, 0.55f);

                SetCellColor(_cells[row, col], color);
            }
        }

        // Ground-zero marker
        SetCellColor(_cells[Center, Center], Color.White);
    }

    // ── Blast intensity approximation ─────────────────────────────────────────

    private static float IntensityAt(RMCDemolitionsSimulatorBuiState s, float dist)
    {
        float remaining = s.TotalIntensity - s.IntensitySlope * dist * dist;
        if (remaining <= 0f) return 0f;
        float tilesInRing = MathF.Max(1f, dist * MathF.Tau);
        return MathF.Min(s.MaxIntensity, remaining / tilesInRing);
    }

    private static Color BlendColor(Color a, Color b, float t)
    {
        return new Color(
            a.R + (b.R - a.R) * t,
            a.G + (b.G - a.G) * t,
            a.B + (b.B - a.B) * t,
            1f);
    }

    // ── Damage table ──────────────────────────────────────────────────────────

    private void BuildDamageTable(RMCDemolitionsSimulatorBuiState s)
    {
        if (_window == null) return;

        var (targetName, targetHp, armour) = Targets[_targetIndex];
        var sb = new System.Text.StringBuilder();
        string header = $"[bold][color=#88dd88]{targetName}[/color][/bold] — HP: {(int)targetHp}  Armour: {(int)(armour*100)}%";
        sb.AppendLine(header);
        sb.AppendLine();

        for (var dist = 0; dist <= 6; dist++)
        {
            float blastDmg = 0f;
            if (s.HasExplosion)
            {
                float intensity = IntensityAt(s, dist);
                blastDmg = intensity * 10f * (1f - armour);
            }

            float fireDmg = s.HasFire && dist <= s.FireRadius
                ? s.FireIntensity * (1f - armour)
                : 0f;

            float total = blastDmg + fireDmg;
            float hpLeft = MathF.Max(0f, targetHp - total);
            float hpPct = hpLeft / targetHp;

            string status = hpPct <= 0f       ? "[color=#ff2222]LETHAL[/color]"
                          : hpPct < 0.30f     ? "[color=#ff8800]Critical[/color]"
                          : hpPct < 0.60f     ? "[color=#ffdd00]Wounded[/color]"
                          :                     "[color=#88ff88]Alive[/color]";

            string label = dist == 0 ? "0 (epicentre)" : $"{dist} tile{(dist == 1 ? "" : "s")}";
            string row = $"[color=#aaaaaa]{label,-14}[/color] {(int)blastDmg}+{(int)fireDmg} = [bold]{(int)total}[/bold] dmg → {status}";
            sb.AppendLine(row);
        }

        _window.DamageTable.SetMarkup(sb.ToString());
    }
}
