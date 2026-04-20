using System.Formats.Tar;
using Content.Server.Popups;
using Content.Shared._RuMC14.Ordnance;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server._RuMC14.Ordnance;

/// <summary>
///     Handles combining two ordnance parts (igniter / timer) into a complete detonator assembly.
///     Timer + Igniter  →  RMCTimerDetonatorAssembly
///     Timer + Timer    →  RMCTimerDetonatorAssembly
///     Igniter + Igniter →  RMCDoubleIgniterAssembly  (used for rockets / grenade-launcher rounds)
/// </summary>
public sealed class RMCOrdnanceAssemblySystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    private static readonly EntProtoId AssemblyPrototype = "RMCOrdnanceAssembly";

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCOrdnancePartComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<RMCOrdnancePartComponent> target, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<RMCOrdnancePartComponent>(args.Used, out var used))
            return;

        args.Handled = true;

        var user = args.User;
        // создаём или берём assembly на target
        var assemblyEnt = Spawn(AssemblyPrototype, _transform.GetMapCoordinates(user));
        var assembly = EnsureComp<RMCOrdnanceAssemblyComponent>(assemblyEnt);

        if (assembly.Left != null && assembly.Right != null)
            return; // full

        // LEFT = активная рука
        bool usedIsActive = _hands.IsHolding(user, args.Used);

        if (usedIsActive)
        {
            if (assembly.Left == null)
                assembly.Left = args.Used;
            else
                assembly.Right = args.Used;
        }
        else
        {
            if (assembly.Right == null)
                assembly.Right = args.Used;
            else
                assembly.Left = args.Used;
        }

        QueueDel(args.Used);

        UpdateVisual(assemblyEnt, assembly);
    }
    private void UpdateVisual(EntityUid uid, RMCOrdnanceAssemblyComponent comp)
    {
        if (!TryComp<RMCOrdnanceAssemblyComponent>(uid, out _))
            return;

        RMCOrdnancePartType? leftType = null;
        RMCOrdnancePartType? rightType = null;

        if (comp.Left != null && TryComp(comp.Left.Value, out RMCOrdnancePartComponent? left))
            leftType = left.PartType;
        else
            return;
        if (comp.Right != null && TryComp(comp.Right.Value, out RMCOrdnancePartComponent? right))
            rightType = right.PartType;
        else
            return;

        _appearance.SetData(uid, RMCAssemblyVisualKey.LeftType, leftType);
        _appearance.SetData(uid, RMCAssemblyVisualKey.RightType, rightType);
    }
}
