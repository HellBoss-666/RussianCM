using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Ordnance;

[Serializable, NetSerializable]
public enum RMCDemolitionsSimulatorUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class RMCDemolitionsSimulatorSimulateMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCDemolitionsSimulatorBuiState(
    string result,
    bool onCooldown,
    int cooldownSecsLeft) : BoundUserInterfaceState
{
    public readonly string Result = result;
    public readonly bool OnCooldown = onCooldown;
    public readonly int CooldownSecsLeft = cooldownSecsLeft;
}
