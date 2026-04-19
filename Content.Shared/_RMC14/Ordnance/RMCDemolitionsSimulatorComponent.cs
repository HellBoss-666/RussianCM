using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Ordnance;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCDemolitionsSimulatorComponent : Component
{
    public static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(2);

    [DataField, AutoNetworkedField]
    public string LastResult = string.Empty;

    [DataField, AutoNetworkedField]
    public TimeSpan CooldownEnd;
}
