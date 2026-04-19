using Content.Server.Explosion.EntitySystems;
using Content.Server.Popups;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Ordnance;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.StepTrigger.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;

namespace Content.Server._RMC14.Ordnance;

/// <summary>
///     Mine-casing deployment: use-in-hand → DoAfter → anchor in-place.
///     Follows the same pattern as <see cref="SharedRMCLandmineSystem"/>.
/// </summary>
public sealed class RMCMineCasingSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly CollisionWakeSystem _collisionWake = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly StepTriggerSystem _stepTrigger = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] protected readonly GunIFFSystem GunIff = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCMineCasingComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<RMCMineCasingComponent, RMCMinePlantDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<RMCPlantedMineComponent, StepTriggeredOffEvent>(OnStepOff);
        SubscribeLocalEvent<RMCPlantedMineComponent, StepTriggerAttemptEvent>(OnStepAttempt);
    }

    // ── Deploy: use in hand → start DoAfter ──────────────────────────────────

    private void OnUseInHand(Entity<RMCMineCasingComponent> ent, ref UseInHandEvent args)
    {
        if (ent.Comp.Planted)
            return;

        if (!TryComp<RMCChembombCasingComponent>(ent.Owner, out var casing))
            return;

        if (!casing.HasActiveDetonator)
        {
            _popup.PopupEntity(Loc.GetString("rmc-mine-no-detonator"), ent.Owner, args.User, PopupType.SmallCaution);
            args.Handled = true;
            return;
        }

        if (casing.Stage != RMCCasingAssemblyStage.Armed)
        {
            _popup.PopupEntity(Loc.GetString("rmc-chembomb-not-armed"), ent.Owner, args.User, PopupType.SmallCaution);
            args.Handled = true;
            return;
        }

        if (!CanDeploy(ent, args.User))
        {
            args.Handled = true;
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.PlacementDelay,
            new RMCMinePlantDoAfterEvent(),
            ent,
            ent,
            args.User)
        {
            NeedHand = true,
            BreakOnMove = true,
            BreakOnHandChange = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    // ── DoAfter complete: anchor entity in-place ──────────────────────────────

    private void OnDoAfter(Entity<RMCMineCasingComponent> ent, ref RMCMinePlantDoAfterEvent args)
    {
        if (args.Cancelled || _net.IsClient)
            return;

        if (!CanDeploy(ent, args.User))
            return;

        var moverCoords = _transform.GetMoverCoordinateRotation(args.User, Transform(args.User));
        var coords = moverCoords.Coords;
        var rotation = moverCoords.worldRot.GetCardinalDir().ToAngle();

        var xform = Transform(ent);
        _transform.SetCoordinates(ent, xform, coords, rotation);
        _transform.AnchorEntity(ent, xform);
        _collisionWake.SetEnabled(ent, false);
        _physics.SetBodyType(ent, BodyType.Static);

        ent.Comp.Planted = true;

        // Activate the step trigger now that the mine is on the floor
        _stepTrigger.SetActive(ent, true);
        var plantedComp = EnsureComp<RMCPlantedMineComponent>(ent);
        GunIff.TryGetFaction(args.User, out var faction);
        plantedComp.Faction = faction;
        Dirty(ent);
        _popup.PopupEntity(Loc.GetString("rmc-mine-planted"), args.User, args.User, PopupType.Medium);
    }

    // ── Placement validation (mirrors RMCLandmineSystem.CanDeployPopup) ────────

    private bool CanDeploy(Entity<RMCMineCasingComponent> ent, EntityUid user)
    {
        var moverCoords = _transform.GetMoverCoordinateRotation(user, Transform(user));

        var query = _rmcMap.GetAnchoredEntitiesEnumerator(moverCoords.Coords);
        while (query.MoveNext(out var anchored))
        {
            if (!HasComp<RMCPlantedMineComponent>(anchored))
                continue;

            _popup.PopupEntity(Loc.GetString("rmc-mine-deploy-fail-occupied"), user, user, PopupType.SmallCaution);
            return false;
        }

        return true;
    }

    // ── Planted mine: step-trigger → TriggerEvent → RMCChembombSystem ─────────

    private void OnStepAttempt(Entity<RMCPlantedMineComponent> ent, ref StepTriggerAttemptEvent args)
    {
        if (ent.Comp.Faction != null && GunIff.IsInFaction(args.Tripper, ent.Comp.Faction.Value))
            args.Cancelled = true;
        args.Continue = true;
    }

    private void OnStepOff(Entity<RMCPlantedMineComponent> ent, ref StepTriggeredOffEvent args)
    {
        _trigger.Trigger(ent.Owner, args.Tripper);
    }
}
