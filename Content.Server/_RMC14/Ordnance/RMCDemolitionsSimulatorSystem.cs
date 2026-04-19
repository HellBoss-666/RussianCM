using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Ordnance;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Ordnance;

public sealed class RMCDemolitionsSimulatorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        Subs.BuiEvents<RMCDemolitionsSimulatorComponent>(RMCDemolitionsSimulatorUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnBuiOpened);
            subs.Event<RMCDemolitionsSimulatorSimulateMsg>(OnSimulateMsg);
        });
    }

    private void OnBuiOpened(Entity<RMCDemolitionsSimulatorComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUiState(ent);
    }

    private void OnSimulateMsg(Entity<RMCDemolitionsSimulatorComponent> ent, ref RMCDemolitionsSimulatorSimulateMsg args)
    {
        var now = _timing.CurTime;
        if (now < ent.Comp.CooldownEnd)
        {
            UpdateUiState(ent);
            return;
        }

        if (!_hands.TryGetActiveItem(args.Actor, out var item) || item == null)
        {
            ent.Comp.LastResult = Loc.GetString("rmc-demolitions-sim-no-casing");
            Dirty(ent);
            UpdateUiState(ent);
            return;
        }

        if (!TryComp<RMCChembombCasingComponent>(item.Value, out var casing))
        {
            ent.Comp.LastResult = Loc.GetString("rmc-demolitions-sim-not-casing");
            Dirty(ent);
            UpdateUiState(ent);
            return;
        }

        ent.Comp.LastResult = RunSimulation(item.Value, casing);
        ent.Comp.CooldownEnd = now + RMCDemolitionsSimulatorComponent.Cooldown;
        Dirty(ent);
        UpdateUiState(ent);
    }

    private string RunSimulation(EntityUid casing, RMCChembombCasingComponent comp)
    {
        float powerMod = 0f;
        float falloffMod = 0f;
        float intensityMod = 0f;
        float radiusMod = 0f;
        float durationMod = 0f;
        float totalVolume = 0f;
        bool hasExplosive = false;
        bool hasIncendiary = false;

        if (_solution.TryGetSolution(casing, comp.ChemicalSolution, out _, out var solution))
        {
            foreach (var reagent in solution)
            {
                if (!_prototype.TryIndexReagent(reagent.Reagent.Prototype, out var proto))
                    continue;

                var qty = (float) reagent.Quantity;
                totalVolume += qty;

                powerMod += qty * (float) proto!.PowerMod;
                falloffMod += qty * (float) proto.FalloffMod;
                intensityMod += qty * (float) proto.IntensityMod;
                radiusMod += qty * (float) proto.RadiusMod;
                durationMod += qty * (float) proto.DurationMod;

                if (proto.PowerMod > FixedPoint2.Zero)
                    hasExplosive = true;
                if (proto.IntensityMod > FixedPoint2.Zero || proto.Intensity > 0)
                    hasIncendiary = true;
            }
        }

        var name = MetaData(casing).EntityName;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(Loc.GetString("rmc-demolitions-sim-header", ("name", name)));
        sb.AppendLine(Loc.GetString("rmc-demolitions-sim-volume",
            ("current", (int) totalVolume), ("max", (int)(float) comp.MaxVolume)));

        if (!hasExplosive && !hasIncendiary)
        {
            sb.AppendLine(Loc.GetString("rmc-demolitions-sim-empty"));
            return sb.ToString();
        }

        if (hasExplosive || powerMod > 0)
        {
            float power = comp.BasePower + powerMod;
            float falloff = MathF.Max(comp.MinFalloff, comp.BaseFalloff + falloffMod);
            float totalIntensity = MathF.Max(1f, power);
            float intensitySlope = MathF.Max(1.5f, falloff / 14f);
            float maxIntensity = MathF.Max(5f, power / 15f);
            float approxRadius = MathF.Sqrt(totalIntensity / intensitySlope);

            sb.AppendLine(Loc.GetString("rmc-demolitions-sim-explosion",
                ("power", (int) power),
                ("falloff", (int) falloff),
                ("radius", approxRadius.ToString("F1"))));
        }

        if (hasIncendiary)
        {
            float intensity = Math.Clamp(comp.MinFireIntensity + intensityMod, comp.MinFireIntensity, comp.MaxFireIntensity);
            float radius = Math.Clamp(comp.MinFireRadius + radiusMod, comp.MinFireRadius, comp.MaxFireRadius);
            float duration = Math.Clamp(comp.MinFireDuration + durationMod, comp.MinFireDuration, comp.MaxFireDuration);

            sb.AppendLine(Loc.GetString("rmc-demolitions-sim-fire",
                ("intensity", (int) intensity),
                ("radius", (int) radius),
                ("duration", (int) duration)));
        }

        return sb.ToString();
    }

    private void UpdateUiState(Entity<RMCDemolitionsSimulatorComponent> ent)
    {
        var now = _timing.CurTime;
        var onCooldown = now < ent.Comp.CooldownEnd;
        var secsLeft = onCooldown ? (int)(ent.Comp.CooldownEnd - now).TotalSeconds : 0;
        var state = new RMCDemolitionsSimulatorBuiState(ent.Comp.LastResult, onCooldown, secsLeft);
        _ui.SetUiState(ent.Owner, RMCDemolitionsSimulatorUiKey.Key, state);
    }
}
