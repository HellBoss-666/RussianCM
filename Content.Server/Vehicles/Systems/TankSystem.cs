using Content.Server.Vehicles.Components;
using Content.Shared.Vehicles.Components;
using Robust.Shared.GameStates;

namespace Content.Server.Vehicles.Systems;

public sealed class TankSystem : EntitySystem
{
    [Dependency] private readonly TankControllerSystem _controllerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TankComponent, ComponentInit>(OnTankInit);
        SubscribeLocalEvent<TankComponent, EntityTerminatingEvent>(OnTankTerminating);
    }

    private void OnTankInit(EntityUid uid, TankComponent component, ComponentInit args)
    {
        component.Health = component.MaxHealth;
        component.Fuel = component.MaxFuel;
    }

    private void OnTankTerminating(EntityUid uid, TankComponent component, ref EntityTerminatingEvent args)
    {
        // Освобождаем всех водителей перед удалением танка
        var query = EntityQueryEnumerator<TankDriverComponent>();
        while (query.MoveNext(out var user, out var driver))
        {
            if (driver.Tank == uid)
            {
                _controllerSystem.UnassignDriver(user);
            }
        }
    }

    public void TakeDamage(EntityUid uid, float damage, TankComponent? tank = null)
    {
        if (!Resolve(uid, ref tank))
            return;

        tank.Health -= damage;
        if (tank.Health <= 0)
            QueueDel(uid);
    }
}
