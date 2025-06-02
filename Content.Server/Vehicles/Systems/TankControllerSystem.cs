using Content.Server.Vehicles.Components;
using Content.Shared.Vehicles.Components;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Server.Vehicles.Systems;

public sealed class TankControllerSystem : EntitySystem
{
    [Dependency] private readonly TankGunSystem _gunSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(EngineKeyFunctions.Use,
                new PointerInputCmdHandler(HandleShoot))
            .Register<TankControllerSystem>();
    }

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

    public void AssignDriver(EntityUid tank, EntityUid user)
    {
        var driver = EnsureComp<TankDriverComponent>(user);
        driver.Tank = tank;
    }

    public void UnassignDriver(EntityUid user)
    {
        RemComp<TankDriverComponent>(user);
    }
}
