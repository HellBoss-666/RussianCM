using Content.Server.Vehicles.Components;
using Content.Shared.Projectiles;
using Content.Shared.Vehicles.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

// Используем общий компонент состояния

namespace Content.Server.Vehicles.Systems;

/// <summary>
/// Система управления оружием танка
/// </summary>
public sealed class TankGunSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = null!;
    [Dependency] private readonly IPrototypeManager _proto = null!;
    [Dependency] private readonly SharedPhysicsSystem _physics = null!;
    [Dependency] private readonly SharedTransformSystem _transform = null!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TankGunComponent, ComponentGetState>(OnGetState);
    }

    /// <summary>
    /// Получение состояния оружия
    /// </summary>
    /// <param name="uid">Танк</param>
    /// <param name="component">Компонент оружия</param>
    /// <param name="args"></param>
    private static void OnGetState(EntityUid uid, TankGunComponent component, ref ComponentGetState args)
    {
        args.State = new TankGunComponentState(component.Ammo, component.NextFire, component.CanShoot);
    }

    /// <summary>
    /// Стрельба
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="user">Игрок</param>
    /// <param name="gun">Пушка</param>
    public void TryFire(EntityUid uid, EntityUid user, TankGunComponent gun)
    {
        if (gun.Ammo <= 0 || !gun.CanShoot || gun.NextFire > _gameTiming.CurTime)
            return;

        gun.Ammo--;
        gun.NextFire = _gameTiming.CurTime + TimeSpan.FromSeconds(gun.Cooldown);

        var direction = _transform.GetWorldRotation(uid).ToWorldVec();
        var spawnPos = _transform.GetMapCoordinates(uid).Offset(direction * 1.5f);

        // Создаем снаряд
        var projectile = EntityManager.SpawnEntity(gun.ProjectilePrototype, spawnPos);

        // Устанавливаем скорость снаряда
        if (TryComp<PhysicsComponent>(projectile, out var physics))
        {
            _physics.ApplyLinearImpulse(projectile, direction * gun.ProjectileSpeed, body: physics);
        }

        // Устанавливаем стрелка (shooter)
        if (TryComp<ProjectileComponent>(projectile, out var projectileComp))
        {
            projectileComp.Shooter = user;
        }
    }
}
