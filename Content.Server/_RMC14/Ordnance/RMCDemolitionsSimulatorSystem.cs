using Content.Server.Explosion.EntitySystems;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Ordnance;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Ordnance;

public sealed class RMCDemolitionsSimulatorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriber = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCDemolitionsSimulatorComponent, MapInitEvent>(OnMapInit);

        Subs.BuiEvents<RMCDemolitionsSimulatorComponent>(RMCDemolitionsSimulatorUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnBuiOpened);
            subs.Event<BoundUIClosedEvent>(OnBuiClosed);
            subs.Event<RMCDemolitionsSimulatorSimulateMsg>(OnSimulateMsg);
        });
    }

    private void OnMapInit(Entity<RMCDemolitionsSimulatorComponent> ent, ref MapInitEvent args)
    {
        CreateChamber(ent);
    }

    private void OnBuiOpened(Entity<RMCDemolitionsSimulatorComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (ent.Comp.ChamberCameraEnt != EntityUid.Invalid &&
            _playerManager.TryGetSessionByEntity(args.Actor, out var session))
        {
            _viewSubscriber.AddViewSubscriber(ent.Comp.ChamberCameraEnt, session);
        }

        UpdateUiState(ent);
    }

    private void OnBuiClosed(Entity<RMCDemolitionsSimulatorComponent> ent, ref BoundUIClosedEvent args)
    {
        if (ent.Comp.ChamberCameraEnt != EntityUid.Invalid &&
            _playerManager.TryGetSessionByEntity(args.Actor, out var session))
        {
            _viewSubscriber.RemoveViewSubscriber(ent.Comp.ChamberCameraEnt, session);
        }
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
            UpdateUiState(ent);
            return;
        }

        if (!TryComp<RMCChembombCasingComponent>(item.Value, out var casing))
        {
            UpdateUiState(ent);
            return;
        }

        RunSimulation(ent, item.Value, casing);
        ent.Comp.CooldownEnd = now + RMCDemolitionsSimulatorComponent.Cooldown;

        // Recreate chamber so mannequins are fresh
        CreateChamber(ent);

        // Re-subscribe current viewer to new camera
        if (ent.Comp.ChamberCameraEnt != EntityUid.Invalid &&
            _playerManager.TryGetSessionByEntity(args.Actor, out var session))
        {
            _viewSubscriber.AddViewSubscriber(ent.Comp.ChamberCameraEnt, session);
        }

        // Schedule explosion 1.5 s after chamber is created (so player sees the room first)
        ent.Comp.PendingExplosion = true;
        ent.Comp.ExplosionAt = now + TimeSpan.FromSeconds(1.5);

        Dirty(ent);
        UpdateUiState(ent);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<RMCDemolitionsSimulatorComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.PendingExplosion || _timing.CurTime < comp.ExplosionAt)
                continue;

            comp.PendingExplosion = false;
            Dirty(uid, comp);

            if (!EntityManager.EntityExists(comp.ChamberCameraEnt))
                continue;

            var xform = Transform(comp.ChamberCameraEnt);
            var center = new MapCoordinates(xform.WorldPosition, xform.MapID);
            _explosion.QueueExplosion(center, "RMC",
                comp.PendingTotalIntensity,
                comp.PendingIntensitySlope,
                comp.PendingMaxIntensity,
                null,
                addLog: false);
        }
    }

    private void CreateChamber(Entity<RMCDemolitionsSimulatorComponent> ent)
    {
        // Delete previous chamber map if it exists
        if (ent.Comp.ChamberMapEnt != EntityUid.Invalid && EntityManager.EntityExists(ent.Comp.ChamberMapEnt))
            QueueDel(ent.Comp.ChamberMapEnt);

        // Create a dedicated map for this chamber
        var mapEnt = _mapSystem.CreateMap(out var mapId);
        ent.Comp.ChamberMapEnt = mapEnt;

        // Create a grid on the map
        var gridEnt = _mapManager.CreateGridEntity(mapId);
        var gridComp = Comp<MapGridComponent>(gridEnt);

        // Lay floor tiles in an 11×11 area
        var floorTile = new Tile(_tileDefinitionManager["FloorSteel"].TileId);
        for (var x = -5; x <= 5; x++)
            for (var y = -5; y <= 5; y++)
                _mapSystem.SetTile(gridEnt, gridComp, new Vector2i(x, y), floorTile);

        // Spawn 4 training dummies at increasing distances north of center
        for (var i = 1; i <= 4; i++)
            Spawn("RMCTrainingDummy", new EntityCoordinates(gridEnt, new System.Numerics.Vector2(0, i)));

        // Spawn camera at center tile (y=0), looking south toward the dummies
        var cam = Spawn("RMCDemolitionsCamera", new EntityCoordinates(gridEnt, System.Numerics.Vector2.Zero));
        ent.Comp.ChamberCameraEnt = cam;
        ent.Comp.ChamberCamera = GetNetEntity(cam);

        Dirty(ent);
    }

    private void RunSimulation(Entity<RMCDemolitionsSimulatorComponent> simEnt, EntityUid casing, RMCChembombCasingComponent comp)
    {
        float powerMod = 0f, falloffMod = 0f;
        float intensityMod = 0f, radiusMod = 0f, durationMod = 0f;
        float totalVolume = 0f;
        bool hasExplosive = false, hasIncendiary = false;

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

                if (proto.PowerMod > FixedPoint2.Zero) hasExplosive = true;
                if (proto.IntensityMod > FixedPoint2.Zero || proto.Intensity > 0) hasIncendiary = true;
            }
        }

        float totalIntensity = 0f, intensitySlope = 0f, maxIntensity = 0f;
        if (hasExplosive || powerMod > 0)
        {
            float power = comp.BasePower + powerMod;
            float falloff = MathF.Max(comp.MinFalloff, comp.BaseFalloff + falloffMod);
            totalIntensity = MathF.Max(1f, power);
            intensitySlope = MathF.Max(1.5f, falloff / 14f);
            maxIntensity = MathF.Max(5f, power / 15f);
        }

        float fireIntensity = 0f, fireRadius = 0f, fireDuration = 0f;
        if (hasIncendiary)
        {
            fireIntensity = Math.Clamp(comp.MinFireIntensity + intensityMod, comp.MinFireIntensity, comp.MaxFireIntensity);
            fireRadius = Math.Clamp(comp.MinFireRadius + radiusMod, comp.MinFireRadius, comp.MaxFireRadius);
            fireDuration = Math.Clamp(comp.MinFireDuration + durationMod, comp.MinFireDuration, comp.MaxFireDuration);
        }

        simEnt.Comp.LastCasingName = MetaData(casing).EntityName;
        simEnt.Comp.LastCurrentVolume = totalVolume;
        simEnt.Comp.LastMaxVolume = (float) comp.MaxVolume;
        simEnt.Comp.LastHasExplosion = hasExplosive || powerMod > 0;
        simEnt.Comp.LastTotalIntensity = totalIntensity;
        simEnt.Comp.LastIntensitySlope = intensitySlope;
        simEnt.Comp.LastMaxIntensity = maxIntensity;
        simEnt.Comp.LastHasFire = hasIncendiary;
        simEnt.Comp.LastFireIntensity = fireIntensity;
        simEnt.Comp.LastFireRadius = fireRadius;
        simEnt.Comp.LastFireDuration = fireDuration;

        // Store pending blast parameters for the deferred explosion
        simEnt.Comp.PendingTotalIntensity = totalIntensity;
        simEnt.Comp.PendingIntensitySlope = intensitySlope;
        simEnt.Comp.PendingMaxIntensity = maxIntensity;

        Dirty(simEnt);
    }

    private void UpdateUiState(Entity<RMCDemolitionsSimulatorComponent> ent)
    {
        var now = _timing.CurTime;
        var onCooldown = now < ent.Comp.CooldownEnd;
        var secsLeft = onCooldown ? (int)(ent.Comp.CooldownEnd - now).TotalSeconds : 0;

        var state = new RMCDemolitionsSimulatorBuiState(
            ent.Comp.LastCasingName,
            ent.Comp.LastCurrentVolume,
            ent.Comp.LastMaxVolume,
            ent.Comp.LastHasExplosion,
            ent.Comp.LastTotalIntensity,
            ent.Comp.LastIntensitySlope,
            ent.Comp.LastMaxIntensity,
            ent.Comp.LastHasFire,
            ent.Comp.LastFireIntensity,
            ent.Comp.LastFireRadius,
            ent.Comp.LastFireDuration,
            onCooldown,
            secsLeft,
            ent.Comp.ChamberCamera);

        _ui.SetUiState(ent.Owner, RMCDemolitionsSimulatorUiKey.Key, state);
    }
}
