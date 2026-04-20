using Content.Server.Explosion.EntitySystems;
using Robust.Server.Audio;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RuMC14.Ordnance;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Movement.Events;

namespace Content.Server._RuMC14.Ordnance;

public sealed class RMCExplosionSimulatorSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriber = default!;
    [Dependency] private readonly EyeSystem _eye = default!;

    private static readonly SoundPathSpecifier BeepSound = new("/Audio/Machines/twobeep.ogg");
    private const string BeakerSlotId = "beakerSlot";

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCExplosionSimulatorWatchingComponent, MoveInputEvent>(OnWatchingMoveInput);
        Subs.BuiEvents<RMCExplosionSimulatorComponent>(RMCExplosionSimulatorUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnBuiOpened);
            subs.Event<BoundUIClosedEvent>(OnBuiClosed);
            subs.Event<RMCExplosionSimulatorSimulateMsg>(OnSimulateMsg);
            subs.Event<RMCExplosionSimulatorTargetMsg>(OnTargetMsg);
            subs.Event<RMCExplosionSimulatorReplayMsg>(OnReplayMsg);
        });
    }

    private void OnBuiOpened(Entity<RMCExplosionSimulatorComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (ent.Comp.ChamberCameraEnt != EntityUid.Invalid &&
            _playerManager.TryGetSessionByEntity(args.Actor, out var session))
        {
            _viewSubscriber.AddViewSubscriber(ent.Comp.ChamberCameraEnt, session);
        }

        UpdateUiState(ent);
    }

    private void OnBuiClosed(Entity<RMCExplosionSimulatorComponent> ent, ref BoundUIClosedEvent args)
    {
        if (ent.Comp.ChamberCameraEnt != EntityUid.Invalid &&
            _playerManager.TryGetSessionByEntity(args.Actor, out var session))
        {
            _viewSubscriber.RemoveViewSubscriber(ent.Comp.ChamberCameraEnt, session);
        }
    }

    private void OnSimulateMsg(Entity<RMCExplosionSimulatorComponent> ent, ref RMCExplosionSimulatorSimulateMsg args)
    {
        if (ent.Comp.IsProcessing)
        {
            UpdateUiState(ent);
            return;
        }

        if (!_itemSlots.TryGetSlot(ent, BeakerSlotId, out var itemSlot) || itemSlot.Item is not { } beaker)
        {
            UpdateUiState(ent);
            return;
        }

        RunSimulation(ent, beaker);

        ent.Comp.IsProcessing = true;
        ent.Comp.SimulationReady = false;
        ent.Comp.ProcessingEnd = _timing.CurTime + RMCExplosionSimulatorComponent.ProcessingDuration;
        ent.Comp.ProcessingActor = args.Actor;

        Dirty(ent);
        UpdateUiState(ent);
    }

    private void OnTargetMsg(Entity<RMCExplosionSimulatorComponent> ent, ref RMCExplosionSimulatorTargetMsg args)
    {
        ent.Comp.SelectedTarget = args.Target;
        Dirty(ent);
        UpdateUiState(ent);
    }

    private void OnReplayMsg(Entity<RMCExplosionSimulatorComponent> ent, ref RMCExplosionSimulatorReplayMsg args)
    {
        if (!ent.Comp.SimulationReady)
            return;

        CreateChamberAndReplay(ent, args.Actor);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<RMCExplosionSimulatorComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.IsProcessing && _timing.CurTime >= comp.ProcessingEnd)
            {
                comp.IsProcessing = false;
                comp.SimulationReady = true;
                Dirty(uid, comp);

                _audio.PlayPvs(BeepSound, uid);

                if (comp.ProcessingActor != EntityUid.Invalid)
                    _popup.PopupEntity("Simulation complete.", uid, comp.ProcessingActor);

                UpdateUiState((uid, comp));
            }

            if (comp.PendingExplosion && _timing.CurTime >= comp.ExplosionAt)
            {
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
    }

    private void RunSimulation(Entity<RMCExplosionSimulatorComponent> simEnt, EntityUid beaker)
    {
        float powerMod = 0f, falloffMod = 0f;
        float intensityMod = 0f, radiusMod = 0f, durationMod = 0f;
        bool hasExplosive = false, hasIncendiary = false;

        if (_solution.TryGetFitsInDispenser(beaker, out _, out var solution))
        {
            foreach (var reagent in solution)
            {
                if (!_prototype.TryIndexReagent(reagent.Reagent.Prototype, out var proto))
                    continue;

                var qty = (float)reagent.Quantity;
                powerMod += qty * (float)proto!.PowerMod;
                falloffMod += qty * (float)proto.FalloffMod;
                intensityMod += qty * (float)proto.IntensityMod;
                radiusMod += qty * (float)proto.RadiusMod;
                durationMod += qty * (float)proto.DurationMod;

                if (proto.PowerMod > FixedPoint2.Zero) hasExplosive = true;
                if (proto.IntensityMod > FixedPoint2.Zero || proto.Intensity > 0) hasIncendiary = true;
            }
        }

        float totalIntensity = 0f, intensitySlope = 0f, maxIntensity = 0f;
        if (hasExplosive || powerMod > 0)
        {
            float power = 180f + powerMod;
            float falloff = MathF.Max(25f, 80f + falloffMod);
            totalIntensity = MathF.Max(1f, power);
            intensitySlope = MathF.Max(1.5f, falloff / 14f);
            maxIntensity = MathF.Max(5f, power / 15f);
        }

        float fireIntensity = 0f, fireRadius = 0f, fireDuration = 0f;
        if (hasIncendiary)
        {
            fireIntensity = Math.Clamp(3f + intensityMod, 3f, 25f);
            fireRadius = Math.Clamp(1f + radiusMod, 1f, 5f);
            fireDuration = Math.Clamp(3f + durationMod, 3f, 24f);
        }

        simEnt.Comp.LastHasExplosion = hasExplosive || powerMod > 0;
        simEnt.Comp.LastTotalIntensity = totalIntensity;
        simEnt.Comp.LastIntensitySlope = intensitySlope;
        simEnt.Comp.LastMaxIntensity = maxIntensity;
        simEnt.Comp.LastHasFire = hasIncendiary;
        simEnt.Comp.LastFireIntensity = fireIntensity;
        simEnt.Comp.LastFireRadius = fireRadius;
        simEnt.Comp.LastFireDuration = fireDuration;

        simEnt.Comp.PendingTotalIntensity = totalIntensity;
        simEnt.Comp.PendingIntensitySlope = intensitySlope;
        simEnt.Comp.PendingMaxIntensity = maxIntensity;

        Dirty(simEnt);
    }

    private void CreateChamberAndReplay(Entity<RMCExplosionSimulatorComponent> ent, EntityUid actor)
    {
        if (ent.Comp.ChamberMapEnt != EntityUid.Invalid && EntityManager.EntityExists(ent.Comp.ChamberMapEnt))
            QueueDel(ent.Comp.ChamberMapEnt);

        if (ent.Comp.ChamberCameraEnt != EntityUid.Invalid &&
            _playerManager.TryGetSessionByEntity(actor, out var oldSession))
        {
            _viewSubscriber.RemoveViewSubscriber(ent.Comp.ChamberCameraEnt, oldSession);
        }

        var mapEnt = _mapSystem.CreateMap(out var mapId);
        ent.Comp.ChamberMapEnt = mapEnt;

        var gridEnt = _mapManager.CreateGridEntity(mapId);
        var gridComp = Comp<MapGridComponent>(gridEnt);

        var floorTile = new Tile(_tileDefinitionManager["FloorSteel"].TileId);
        for (var x = -8; x <= 8; x++)
            for (var y = -8; y <= 8; y++)
                _mapSystem.SetTile(gridEnt, gridComp, new Vector2i(x, y), floorTile);

        SpawnTargetEntities(ent.Comp.SelectedTarget, gridEnt);

        var cam = Spawn("RMCDemolitionsCamera", new EntityCoordinates(gridEnt, System.Numerics.Vector2.Zero));
        ent.Comp.ChamberCameraEnt = cam;
        ent.Comp.ChamberCamera = GetNetEntity(cam);

        if (_playerManager.TryGetSessionByEntity(actor, out var session))
            _viewSubscriber.AddViewSubscriber(cam, session);

        _eye.SetTarget(actor, cam);
        _eye.SetDrawLight(actor, false);
        var watchingComp = EnsureComp<RMCExplosionSimulatorWatchingComponent>(actor);
        watchingComp.Watching = cam;
        ent.Comp.PendingExplosion = true;
        ent.Comp.ExplosionAt = _timing.CurTime + TimeSpan.FromSeconds(2.0);

        Dirty(ent);
        UpdateUiState(ent);
    }

    private void SpawnTargetEntities(RMCExplosionSimulatorTarget target, EntityUid gridEnt)
    {
        System.Numerics.Vector2[] positions = target switch
        {
            RMCExplosionSimulatorTarget.Marines => new[]
            {
                new System.Numerics.Vector2(2, 1),  new System.Numerics.Vector2(2, -1),
                new System.Numerics.Vector2(3, 2),  new System.Numerics.Vector2(3, 0),  new System.Numerics.Vector2(3, -2),
                new System.Numerics.Vector2(4, 1),  new System.Numerics.Vector2(4, -1),
            },
            RMCExplosionSimulatorTarget.SpecialForces => new[]
            {
                new System.Numerics.Vector2(2, 0),  new System.Numerics.Vector2(2, 1),  new System.Numerics.Vector2(2, -1),
                new System.Numerics.Vector2(3, 0),  new System.Numerics.Vector2(3, 1),
            },
            RMCExplosionSimulatorTarget.Xenomorphs => new[]
            {
                new System.Numerics.Vector2(1, 0),  new System.Numerics.Vector2(1, 1),  new System.Numerics.Vector2(1, -1),
                new System.Numerics.Vector2(2, 0),  new System.Numerics.Vector2(2, 1),  new System.Numerics.Vector2(2, -1),
                new System.Numerics.Vector2(3, 0),  new System.Numerics.Vector2(3, 1),  new System.Numerics.Vector2(3, -1),
            },
            _ => Array.Empty<System.Numerics.Vector2>(),
        };

        foreach (var pos in positions)
            Spawn("RMCTrainingDummy", new EntityCoordinates(gridEnt, pos));
    }

    private void UpdateUiState(Entity<RMCExplosionSimulatorComponent> ent)
    {
        var hasBeaker = _itemSlots.TryGetSlot(ent, BeakerSlotId, out var slot) && slot.Item.HasValue;
        var now = _timing.CurTime;
        var secsLeft = ent.Comp.IsProcessing ? (int)(ent.Comp.ProcessingEnd - now).TotalSeconds : 0;

        var state = new RMCExplosionSimulatorBuiState(
            hasBeaker,
            ent.Comp.SelectedTarget,
            ent.Comp.IsProcessing,
            secsLeft,
            ent.Comp.SimulationReady,
            ent.Comp.LastHasExplosion,
            ent.Comp.LastTotalIntensity,
            ent.Comp.LastIntensitySlope,
            ent.Comp.LastMaxIntensity,
            ent.Comp.LastHasFire,
            ent.Comp.LastFireIntensity,
            ent.Comp.LastFireRadius,
            ent.Comp.LastFireDuration,
            ent.Comp.ChamberCamera);

        _ui.SetUiState(ent.Owner, RMCExplosionSimulatorUiKey.Key, state);
    }

    // Это нужно будет переписать под шейрд
    private void OnWatchingMoveInput(Entity<RMCExplosionSimulatorWatchingComponent> ent, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement)
            return;

        if (TryComp(ent, out ActorComponent? actor))
            Unwatch(ent.Owner, actor.PlayerSession);
    }

    private void Unwatch(Entity<EyeComponent?> watcher, ICommonSession player)
    {
        if (!Resolve(watcher, ref watcher.Comp))
            return;

        _eye.SetTarget(watcher, null);
        _eye.SetDrawLight(watcher, true);
    }
}
