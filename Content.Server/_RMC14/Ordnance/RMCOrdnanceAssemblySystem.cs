using Content.Server.Popups;
using Content.Shared._RMC14.Ordnance;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Ordnance;

/// <summary>
///     Handles combining two ordnance parts (igniter / timer) into a complete detonator assembly.
///     Timer + Igniter  →  RMCTimerDetonatorAssembly
///     Timer + Timer    →  RMCTimerDetonatorAssembly
///     Igniter + Igniter →  RMCDoubleIgniterAssembly  (used for rockets / grenade-launcher rounds)
/// </summary>
public sealed class RMCOrdnanceAssemblySystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;

    private static readonly EntProtoId TimerResult = "RMCTimerDetonatorAssembly";
    private static readonly EntProtoId DoubleIgniterResult = "RMCDoubleIgniterAssembly";
    private static readonly EntProtoId RMCOrdnanceIgniterPrototype = "RMCOrdnanceIgniter";
    private static readonly EntProtoId RMCOrdnanceTimerPrototype = "RMCOrdnanceTimer";

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCOrdnancePartComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<RMCDetonatorAssemblyComponent, InteractUsingEvent>(OnInteractUsingAssembly);
    }

    private void OnInteractUsingAssembly(Entity<RMCDetonatorAssemblyComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || _net.IsClient)
            return;

        args.Handled = true;

        // Использование отвертки => переключаем готовность этой штуки
        if (_tool.HasQuality(args.Used, "Screwing"))
        {
            ent.Comp.Ready = !ent.Comp.Ready;
        }
        // Использование лома => разбираем на составные части
        if (_tool.HasQuality(args.Used, "Prying"))
        {
            // Если сборка была скручена, значит, разобрать не получится
            if (ent.Comp.Ready)
            {
                _popup.PopupEntity(Loc.GetString("rmc-ordnance-assembly-cannot-disassembly"), args.User, args.User);
                return;
            }
            else
            {
                if (!TryComp<RMCDetonatorAssemblyComponent>(ent, out var comp))
                    return;
                if (comp.DetonatorType == RMCDetonatorType.DoubleIgniter)
                {
                    // делаем дважды, потому что их было 2 в дабл игниторе
                    Spawn(RMCOrdnanceIgniterPrototype, _transform.GetMapCoordinates(args.User));
                    Spawn(RMCOrdnanceIgniterPrototype, _transform.GetMapCoordinates(args.User));
                }
                else if (comp.DetonatorType == RMCDetonatorType.Timer)
                {
                    Spawn(RMCOrdnanceIgniterPrototype, _transform.GetMapCoordinates(args.User));
                    Spawn(RMCOrdnanceTimerPrototype, _transform.GetMapCoordinates(args.User));
                }
                _popup.PopupEntity(Loc.GetString("rmc-ordnance-assembly-success-dissassembly"), args.User, args.User);
            }
        }
    }

    private void OnInteractUsing(Entity<RMCOrdnancePartComponent> target, ref InteractUsingEvent args)
    {
        if (args.Handled || _net.IsClient)
            return;

        if (!TryComp<RMCOrdnancePartComponent>(args.Used, out var usedPart))
            return;

        args.Handled = true;

        var targetType = target.Comp.PartType;
        var usedType = usedPart.PartType;
        var user = args.User;

        EntProtoId? resultProto = null;

        if (targetType == RMCOrdnancePartType.Igniter && usedType == RMCOrdnancePartType.Igniter)
        {
            resultProto = DoubleIgniterResult;
        }
        else if ((targetType == RMCOrdnancePartType.Timer && usedType == RMCOrdnancePartType.Igniter) ||
                 (targetType == RMCOrdnancePartType.Igniter && usedType == RMCOrdnancePartType.Timer) ||
                 (targetType == RMCOrdnancePartType.Timer && usedType == RMCOrdnancePartType.Timer))
        {
            resultProto = TimerResult;
        }

        if (resultProto == null)
        {
            _popup.PopupEntity(Loc.GetString("rmc-ordnance-assembly-incompatible"), target.Owner, user, PopupType.SmallCaution);
            return;
        }

        var resultEnt = Spawn(resultProto.Value, _transform.GetMapCoordinates(user));

        if (!_hands.TryPickupAnyHand(user, resultEnt))
            _transform.SetCoordinates(resultEnt, Transform(user).Coordinates);

        QueueDel(target.Owner);
        QueueDel(args.Used);

        _popup.PopupEntity(
            Loc.GetString("rmc-ordnance-assembly-combined", ("result", MetaData(resultEnt).EntityName)),
            user,
            user);
    }
}
