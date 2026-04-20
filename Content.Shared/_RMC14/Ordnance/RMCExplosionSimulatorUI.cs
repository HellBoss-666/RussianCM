using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Ordnance;

[Serializable, NetSerializable]
public enum RMCExplosionSimulatorUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class RMCExplosionSimulatorSimulateMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCExplosionSimulatorTargetMsg(RMCExplosionSimulatorTarget target) : BoundUserInterfaceMessage
{
    public readonly RMCExplosionSimulatorTarget Target = target;
}

[Serializable, NetSerializable]
public sealed class RMCExplosionSimulatorReplayMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCExplosionSimulatorBuiState(
    bool hasBeaker,
    RMCExplosionSimulatorTarget target,
    bool isProcessing,
    int processingSecsLeft,
    bool simulationReady,
    bool hasExplosion,
    float totalIntensity,
    float intensitySlope,
    float maxIntensity,
    bool hasFire,
    float fireIntensity,
    float fireRadius,
    float fireDuration,
    NetEntity? cameraNetId) : BoundUserInterfaceState
{
    public readonly bool HasBeaker = hasBeaker;
    public readonly RMCExplosionSimulatorTarget Target = target;
    public readonly bool IsProcessing = isProcessing;
    public readonly int ProcessingSecsLeft = processingSecsLeft;
    public readonly bool SimulationReady = simulationReady;
    public readonly bool HasExplosion = hasExplosion;
    public readonly float TotalIntensity = totalIntensity;
    public readonly float IntensitySlope = intensitySlope;
    public readonly float MaxIntensity = maxIntensity;
    public readonly bool HasFire = hasFire;
    public readonly float FireIntensity = fireIntensity;
    public readonly float FireRadius = fireRadius;
    public readonly float FireDuration = fireDuration;
    public readonly NetEntity? CameraNetId = cameraNetId;
}
