using Content.Shared.Buckle.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.RuMC.Vehicles.Components;
using Content.Shared.Vehicles.Components;

namespace Content.Shared.RuMC.Vehicles.Systems;

/// <summary>
/// Система танка
/// </summary>
public sealed class TankSystem : EntitySystem
{
    [Dependency] private readonly TankControllerSystem _controllerSystem = null!;
    [Dependency] private readonly SharedMoverController _mover = null!;
    [Dependency] private readonly SharedInteractionSystem _interaction = null!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TankComponent, ComponentInit>(OnTankInit);
        SubscribeLocalEvent<TankComponent, EntityTerminatingEvent>(OnTankTerminating);
        // Подписываемся на взаимодействие для посадки в танк через ПКМ-меню
        SubscribeLocalEvent<TankComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<TankComponent, UnstrappedEvent>(OnUnstrapped);
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

    private void OnStrapped(Entity<TankComponent> ent, ref StrappedEvent args)
    {
        var buckle = args.Buckle;

        // Добавляем компонент, который будет релеить клики
        var interactionRelay = EnsureComp<InteractionRelayComponent>(buckle);
        // И вот тут передаём именно его
        _interaction.SetRelay(buckle, ent, interactionRelay);

        // Добавляем RelayInputMoverComponent и сразу подключаем ввод
        EnsureComp<RelayInputMoverComponent>(buckle);
        _mover.SetRelay(buckle, ent);
    }

    private void OnUnstrapped(Entity<TankComponent> ent, ref UnstrappedEvent args)
    {
        var buckle = args.Buckle;
        RemCompDeferred<RelayInputMoverComponent>(buckle);
        RemCompDeferred<InteractionRelayComponent>(buckle);
    }
}
