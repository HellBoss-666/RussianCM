using Robust.Shared.Serialization;

namespace Content.Shared._RuMC14.Ordnance;

public static class RMCAssemblyVisuals
{
    public const string LeftType = "left_type";
    public const string RightType = "right_type";
}

[Serializable, NetSerializable]
public enum RMCAssemblyVisualKey
{
    LeftType,
    RightType
}
