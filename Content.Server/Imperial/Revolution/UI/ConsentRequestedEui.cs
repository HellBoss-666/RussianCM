using Content.Server.Imperial.Revolutionary.Components;
using Content.Server.EUI;
using Content.Server.GameTicking.Rules;
using Content.Server.Popups;
using Content.Shared.Imperial.Revolutionary;
using Content.Shared.Eui;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;


namespace Content.Server.Imperial.Revolutionary.UI;

public sealed class ConsentRequestedEui : BaseEui
{
    private readonly EntityUid _target;
    private readonly EntityUid _converter;
    private readonly RevolutionaryRuleSystem _revRuleSystem;
    private readonly ConsentRevolutionarySystem _consRevSystem;
    private readonly PopupSystem _popup;
    private readonly EntityManager _entManager;

    public ConsentRequestedEui(EntityUid target, EntityUid converter, RevolutionaryRuleSystem revRuleSystem, ConsentRevolutionarySystem consRevSystem, PopupSystem popup, EntityManager entManager)
    {
        _target = target;
        _converter = converter;
        _revRuleSystem = revRuleSystem;
        _consRevSystem = consRevSystem;
        _popup = popup;
        _entManager = entManager;
    }

    public override EuiStateBase GetNewState()
    {
        // Возвращаем состояние с именем конвертера
        return new ConsentRequestedState(Identity.Name(_converter, _entManager));
    }

    public override void Opened()
    {
        base.Opened();
        StateDirty();
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is ConsentRequestedEuiMessage consent && _revRuleSystem.IsConvertable(_target))
        {
            if (!_entManager.TryGetComponent<ConsentRevolutionaryComponent>(_target, out var targetConsRev)
                || !_entManager.TryGetComponent<ConsentRevolutionaryComponent>(_converter, out var consRev))
            {
                return;
            }

            if (consent.IsAccepted)
            {
                // Преобразуем цель в революционера
                _revRuleSystem.ConvertEntityToRevolution(_target, _converter);

                // Удаляем запрос
                _consRevSystem.CancelRequest(( _target, targetConsRev), ( _converter, consRev));

                // Применяем кулдаун к конвертеру
                _consRevSystem.ApplyConversionCooldown(( _converter, consRev));

                // Показываем уведомление об успешном обращении
                _popup.PopupEntity(
                    Loc.GetString("rev-consent-convert-accepted", ("target", Identity.Entity(_target, _entManager))),
                    _target,
                    _converter);
            }
            else
            {
                // Отменяем запрос с применением блокировки
                _consRevSystem.CancelRequest(( _target, targetConsRev), ( _converter, consRev));

                // Применяем блокировку обращения к цели
                _consRevSystem.ApplyConversionDeny(( _target, targetConsRev));

                // Показываем уведомление об отказе
                _popup.PopupEntity(
                    Loc.GetString("rev-consent-convert-denied", ("target", Identity.Entity(_target, _entManager))),
                    _target,
                    _converter,
                    PopupType.SmallCaution);
            }
        }

        Close();
    }
}
