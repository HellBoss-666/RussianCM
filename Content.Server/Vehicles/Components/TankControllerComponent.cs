using Robust.Shared.GameStates;

namespace Content.Server.Vehicles.Components;

/// <summary>
/// Атрибуты контроллера танка
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TankControllerComponent : Component
{
    [ViewVariables]
    public EntityUid? Controller;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool CanMove = true;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool CanShoot = true;
}
