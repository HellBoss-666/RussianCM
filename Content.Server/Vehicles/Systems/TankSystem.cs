using Content.Server.Vehicles.Components;
using Content.Shared.Vehicles.Components;

namespace Content.Server.Vehicles.Systems;


/// <summary>
/// Система танка
/// </summary>
public sealed class TankSystem : EntitySystem
{
    [Dependency] private readonly TankControllerSystem _controllerSystem = null!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TankComponent, ComponentInit>(OnTankInit);
        SubscribeLocalEvent<TankComponent, EntityTerminatingEvent>(OnTankTerminating);
    }

    /// <summary>
    /// Инициализация танка
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <param name="args"></param>
    private static void OnTankInit(EntityUid uid, TankComponent component, ComponentInit args)
    {
        component.Health = component.MaxHealth;
        component.Fuel = component.MaxFuel;
    }

    /// <summary>
    /// Уничтожение танка
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <param name="args"></param>
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

    /// <summary>
    /// Получение урона
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="damage"></param>
    /// <param name="tank"></param>
    public void TakeDamage(EntityUid uid, float damage, TankComponent? tank = null)
    {
        if (!Resolve(uid, ref tank))
            return;

        tank.Health -= damage;
        if (tank.Health <= 0)
            QueueDel(uid);
    }
}
