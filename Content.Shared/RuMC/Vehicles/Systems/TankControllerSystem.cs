using Content.Shared.RuMC.Vehicles.Components;
using Content.Shared.Vehicles.Components;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;

namespace Content.Shared.RuMC.Vehicles.Systems;

/// <summary>
/// Система контроля танка
/// </summary>
public sealed class TankControllerSystem : EntitySystem
{
    [Dependency] private readonly TankGunSystem _gunSystem = null!;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(EngineKeyFunctions.Use,
                new PointerInputCmdHandler(HandleShoot))
            .Register<TankControllerSystem>();
    }

    /// <summary>
    /// Обработка выстрела
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    private bool HandleShoot(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (args.Session?.AttachedEntity is not { } user)
            return false;

        if (!TryComp<TankDriverComponent>(user, out var driver) ||
            !Exists(driver.Tank) ||
            Deleted(driver.Tank))
            return false;

        if (TryComp<TankGunComponent>(driver.Tank, out var gun))
            _gunSystem.TryFire(driver.Tank, user, gun);

        return true;
    }

    /// <summary>
    /// Привязка водителя к танку
    /// </summary>
    /// <param name="tank">Танк</param>
    /// <param name="user">Игрок</param>
    public void AssignDriver(EntityUid tank, EntityUid user)
    {
        var driver = EnsureComp<TankDriverComponent>(user);
        driver.Tank = tank;
    }
    /// <summary>
    /// Отвязка водителя от танка
    /// </summary>
    /// <param name="user">Игрок</param>
    public void UnassignDriver(EntityUid user)
    {
        RemComp<TankDriverComponent>(user);
    }
}
