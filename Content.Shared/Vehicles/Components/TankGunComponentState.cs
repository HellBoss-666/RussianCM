using Robust.Shared.Serialization;

namespace Content.Shared.Vehicles.Components;

[Serializable, NetSerializable]
public sealed class TankGunComponentState(int ammo, TimeSpan nextFire, bool canShoot) : ComponentState
{
    public int Ammo { get; } = ammo;
    public TimeSpan NextFire { get; } = nextFire;
    public bool CanShoot { get; } = canShoot;
}
