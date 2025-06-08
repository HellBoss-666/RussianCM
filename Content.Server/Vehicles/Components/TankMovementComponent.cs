using Robust.Shared.GameStates;

namespace Content.Server.Vehicles.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class TankMovementComponent : Component
{
    [DataField]
    public float MaxSpeed = 5f;

    [DataField]
    public float Acceleration = 2f;

    [DataField]
    public float RotationSpeed = 1.5f;

    [ViewVariables]
    public float CurrentSpeed = 0f;

    [ViewVariables]
    public bool MovingForward = true;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool CanMove = true;

    // Добавлено для хранения активных направлений движения
    [ViewVariables]
    public HashSet<Direction> ActiveDirections = new();
}
