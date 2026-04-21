using Content.Server.Storage.Components;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Power;

namespace Content.Server._RuMC14.Chemistry;

public sealed class RuMCCoolingChamberSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RuMCCoolingChamberComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnPowerChanged(Entity<RuMCCoolingChamberComponent> entity, ref PowerChangedEvent args)
    {
        if (args.Powered)
            EnsureComp<ActiveRuMCCoolingChamberComponent>(entity);
        else
            RemComp<ActiveRuMCCoolingChamberComponent>(entity);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveRuMCCoolingChamberComponent, RuMCCoolingChamberComponent, EntityStorageComponent>();
        while (query.MoveNext(out _, out _, out var cooler, out var storage))
        {
            // Копия списка чтобы избежать изменения коллекции при итерации
            foreach (var contained in new List<EntityUid>(storage.Contents.ContainedEntities))
            {
                if (!TryComp<SolutionContainerManagerComponent>(contained, out var manager))
                    continue;

                // Явная типизация для корректной перегрузки метода
                Entity<SolutionContainerManagerComponent?> entityManager = new(contained, manager);
                foreach (var (_, soln) in _solutionContainer.EnumerateSolutions(entityManager))
                {
                    var temp = soln.Comp.Solution.Temperature;
                    if (temp <= cooler.TargetTemperature)
                        continue;

                    _solutionContainer.AddThermalEnergy(soln, -cooler.CoolPerSecond * frameTime);

                    // Не переохлаждаем ниже целевой температуры
                    if (soln.Comp.Solution.Temperature < cooler.TargetTemperature)
                        _solutionContainer.SetTemperature(soln, cooler.TargetTemperature);
                }
            }
        }
    }
}
