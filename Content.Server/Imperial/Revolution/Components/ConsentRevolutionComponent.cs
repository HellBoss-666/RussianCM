using Content.Server.Imperial.Revolutionary.UI;

namespace Content.Server.Imperial.Revolutionary.Components;

[RegisterComponent]
public sealed partial class ConsentRevolutionaryComponent : Component
{
    /// <summary>
    /// Другой участник запроса на обращение. Если null, значит сущность не участвует в запросе.
    /// </summary>
    [DataField]
    public EntityUid? OtherMember;

    /// <summary>
    /// Является ли сущность инициатором обращения.
    /// Если false, значит сущность запрашивает преобразование.
    /// </summary>
    [DataField]
    public bool IsConverter = false;

    /// <summary>
    /// Окно интерфейса подтверждения обращения
    /// </summary>
    public ConsentRequestedEui? Window;

    /// <summary>
    /// Время начала последнего запроса на обращение
    /// </summary>
    [DataField]
    public TimeSpan? RequestStartTime;

    /// <summary>
    /// Таймаут ожидания ответа на запрос обращения
    /// </summary>
    [DataField]
    public TimeSpan ResponseTime = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Время блокировки повторных запросов после отказа
    /// </summary>
    [DataField]
    public TimeSpan RequestBlockTime = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Время блокировки новых запросов после успешного обращения
    /// </summary>
    [DataField]
    public TimeSpan ConversionBlockTime = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Максимальная дистанция для взаимодействия при запросе
    /// </summary>
    [DataField]
    public float MaxDistance = 3f;
}
