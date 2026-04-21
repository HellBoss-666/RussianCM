using Content.Server.Popups;
using Content.Shared._RuMC14.Ordnance;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._RuMC14.Ordnance;

public sealed class RMCOrdnancePayloadAssemblySystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    private static readonly SoundSpecifier InsertSound = new SoundPathSpecifier("/Audio/_RMC14/Weapons/Guns/Reload/grenade_insert.ogg");

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCOrdnancePayloadAssemblyComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<RMCOrdnancePayloadAssemblyComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<RMCChembombCasingComponent>(args.Used, out var casing))
            return;

        var resultProto = GetResult(ent.Comp, args.Used);
        if (resultProto == null)
            return;

        args.Handled = true;

        if (casing.Stage != RMCCasingAssemblyStage.Armed || !casing.HasActiveDetonator)
        {
            _popup.PopupEntity(Loc.GetString("rmc-ordnance-payload-not-ready"), ent, args.User, PopupType.SmallCaution);
            return;
        }

        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.FuelSolution, out _, out var fuelSolution) ||
            fuelSolution.Volume < ent.Comp.RequiredFuel)
        {
            _popup.PopupEntity(Loc.GetString("rmc-ordnance-payload-no-fuel"), ent, args.User, PopupType.SmallCaution);
            return;
        }

        if (!_solution.TryGetSolution(args.Used, casing.ChemicalSolution, out _, out var chemicalSolution) ||
            chemicalSolution.Volume <= FixedPoint2.Zero)
        {
            _popup.PopupEntity(Loc.GetString("rmc-ordnance-payload-no-chemicals"), args.Used, args.User, PopupType.SmallCaution);
            return;
        }

        var result = Spawn(resultProto.Value, Transform(ent).Coordinates);

        if (_solution.TryGetSolution(args.Used, casing.ChemicalSolution, out var sourceChemicals, out var _)
            && _solution.TryGetSolution(result, ent.Comp.ChemicalSolution, out var resultChemicals, out _))
        {
            var transferred = _solution.SplitSolution(sourceChemicals.Value, chemicalSolution.Volume);
            _solution.TryAddSolution(resultChemicals.Value, transferred);
        }

        _audio.PlayPredicted(InsertSound, result, args.User);
        _popup.PopupEntity(Loc.GetString("rmc-ordnance-payload-assembled"), result, args.User);

        QueueDel(args.Used);
        QueueDel(ent);
    }

    private EntProtoId? GetResult(RMCOrdnancePayloadAssemblyComponent comp, EntityUid payload)
    {
        var prototypeId = MetaData(payload).EntityPrototype?.ID;
        if (prototypeId == null)
            return null;

        foreach (var result in comp.Results)
        {
            if (result.Payload == prototypeId)
                return result.Result;
        }

        return null;
    }
}
