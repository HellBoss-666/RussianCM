namespace Content.Server._RuMC14.Chemistry;

/// <summary>
/// Машина, охлаждающая растворы в мензурках внутри неё до целевой температуры.
/// Требуется для синтеза нитроглицерина, циклонита и октогена.
/// </summary>
[RegisterComponent]
public sealed partial class RuMCCoolingChamberComponent : Component
{
    /// <summary>
    /// Тепловая энергия, снимаемая каждую секунду (в Дж/с).
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float CoolPerSecond = 160f;

    /// <summary>
    /// Целевая температура охлаждения в Кельвинах (0K ≈ -273°C).
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float TargetTemperature = 0f;
}

/// <summary>
/// Маркер активного состояния — навешивается при включённом питании.
/// </summary>
[RegisterComponent]
public sealed partial class ActiveRuMCCoolingChamberComponent : Component
{
}
