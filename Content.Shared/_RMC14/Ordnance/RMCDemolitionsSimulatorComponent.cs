using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Ordnance;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCDemolitionsSimulatorComponent : Component
{
    public static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(2);

    [DataField, AutoNetworkedField]
    public TimeSpan CooldownEnd;

    /// <summary>The camera entity inside the test chamber. Networked so the client can set its viewport eye.</summary>
    [DataField, AutoNetworkedField]
    public NetEntity? ChamberCamera;

    // Last simulation results (sent to client via BuiState)
    [DataField, AutoNetworkedField]
    public string LastCasingName = string.Empty;

    [DataField, AutoNetworkedField]
    public float LastCurrentVolume;

    [DataField, AutoNetworkedField]
    public float LastMaxVolume;

    [DataField, AutoNetworkedField]
    public bool LastHasExplosion;

    [DataField, AutoNetworkedField]
    public float LastTotalIntensity;

    [DataField, AutoNetworkedField]
    public float LastIntensitySlope;

    [DataField, AutoNetworkedField]
    public float LastMaxIntensity;

    [DataField, AutoNetworkedField]
    public bool LastHasFire;

    [DataField, AutoNetworkedField]
    public float LastFireIntensity;

    [DataField, AutoNetworkedField]
    public float LastFireRadius;

    [DataField, AutoNetworkedField]
    public float LastFireDuration;

    // ── Server-only transient state (not networked, not serialized) ──────────
    public EntityUid ChamberMapEnt = EntityUid.Invalid;
    public EntityUid ChamberCameraEnt = EntityUid.Invalid;
    public bool PendingExplosion;
    public TimeSpan ExplosionAt;
    public float PendingTotalIntensity;
    public float PendingIntensitySlope;
    public float PendingMaxIntensity;
}
