using Robust.Shared.GameStates;

namespace Content.Shared.Vehicles.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class TankDriverComponent : Component
{
    [ViewVariables]
    public EntityUid Tank;
}
