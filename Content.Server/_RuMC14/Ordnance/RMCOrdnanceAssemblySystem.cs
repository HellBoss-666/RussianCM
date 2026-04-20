using Content.Shared._RuMC14.Ordnance;
using Content.Shared.Interaction;
using Content.Shared.Tools.Systems;
using Discord.Commands;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._RuMC14.Ordnance;

public sealed class RMCOrdnanceAssemblySystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;

    private static readonly EntProtoId AssemblyPrototype = "RMCOrdnanceAssembly";

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCOrdnancePartComponent, InteractUsingEvent>(OnInteractUsing); // Сборка из двух компонентов
        SubscribeLocalEvent<RMCOrdnanceAssemblyComponent, InteractUsingEvent>(OnAssemblyInteractUsing);
    }
    private void OnAssemblyInteractUsing(Entity<RMCOrdnanceAssemblyComponent> ent, ref InteractUsingEvent args)
    {
        // Когда человек нажимает ломиком по сборке - она распадается на две частички.

        if (_toolSystem.HasQuality(args.Used, "Prying"))
            OnAssemblyInteractPrying(ent);
        else if (_toolSystem.HasQuality(args.Used, "Screwing"))
            // Пытаемся "прикрутить" сборку.
            return;

    }
    /// <summary>
    /// Метод, разбирающий Assembly на составные части
    /// </summary>
    private void OnAssemblyInteractPrying(Entity<RMCOrdnanceAssemblyComponent> ent)
    {
        // Пытаемся разобрать сборку
        var leftPart = ent.Comp.LeftPartType;
        var rightPart = ent.Comp.RightPartType;

        var xform = Transform(ent).Coordinates; // Получаем координаты сборки

        // Спавним обе части
        Spawn(leftPart.ToString(), xform);
        Spawn(rightPart.ToString(), xform);

        // Удаляем сборку
        QueueDel(ent);
    }


    private void OnInteractUsing(Entity<RMCOrdnancePartComponent> target, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<RMCOrdnancePartComponent>(args.Used, out var usedPart))
            return;

        args.Handled = true;

        // Предмет в активной руке — LEFT, цель взаимодействия — RIGHT
        var leftType = usedPart.PartType;
        var rightType = target.Comp.PartType;

        var assemblyEnt = Spawn(AssemblyPrototype, _transform.GetMapCoordinates(args.User));
        var assembly = EnsureComp<RMCOrdnanceAssemblyComponent>(assemblyEnt);
        assembly.LeftPartType = leftType;
        assembly.RightPartType = rightType;

        QueueDel(args.Used);
        QueueDel(target.Owner);

        _appearance.SetData(assemblyEnt, RMCAssemblyVisualKey.LeftType, leftType);
        _appearance.SetData(assemblyEnt, RMCAssemblyVisualKey.RightType, rightType);
    }
}
