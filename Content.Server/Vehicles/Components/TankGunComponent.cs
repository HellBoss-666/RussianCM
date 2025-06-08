using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Vehicles.Components;

/// <summary>
/// Атрибуты оружия танка
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TankGunComponent : Component
{
    [DataField]
    public int MaxAmmo = 20;

    [DataField]
    public int Ammo = 20;

    [DataField]
    public float Cooldown = 1.5f; // Секунд между выстрелами

    [ViewVariables]
    public TimeSpan NextFire = TimeSpan.Zero;

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ProjectilePrototype = "TankShell";

    [DataField]
    public float ProjectileSpeed = 25f;

    // Добавлено новое свойство
    [ViewVariables(VVAccess.ReadWrite)]
    public bool CanShoot = true;
}
