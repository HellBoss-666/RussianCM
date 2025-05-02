using Content.Server.Imperial.Revolutionary.Components;
using Content.Server.Imperial.Revolutionary.UI;
using Content.Server.EUI;
using Content.Server.GameTicking.Rules;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Revolutionary.Components;
using Content.Shared.Imperial.Revolutionary.Events;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Roles;
using Content.Shared.StatusEffect;
using Content.Shared.Verbs;
using Content.Shared.Zombies;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Content.Server.Imperial.Revolutionary;

namespace Content.Server.Imperial.Revolutionary;

public sealed class ConsentRevolutionarySystem : EntitySystem
{
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly EuiManager _euiMan = default!;
    [Dependency] private readonly RevolutionaryRuleSystem _revRule = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    private float _accumulator = 0f;
    private const float AccumulatorThreshold = 1f;

    public const string RevConvertDeniedStatusEffect = "RevConversionDenied";
    public const string RevConvertCooldownStatusEffect = "RevConversionCooldown";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HeadRevolutionaryComponent, GetVerbsEvent<InnateVerb>>(OnInnateVerb);
        SubscribeLocalEvent<ConsentRevolutionaryComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<ConsentRevolutionaryComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<ConsentRevolutionaryComponent, RoleAddedEvent>(OnRoleAdded);
        SubscribeLocalEvent<ConsentRevolutionaryComponent, RemoveConversionDeniedAlertEvent>(OnRemoveConversionDeniedAlert);
    }

    private void OnRemoveConversionDeniedAlert(Entity<ConsentRevolutionaryComponent> entity, ref RemoveConversionDeniedAlertEvent args)
    {
        _status.TryRemoveStatusEffect(entity.Owner, RevConvertDeniedStatusEffect);
    }

    private void OnInnateVerb(EntityUid uid, HeadRevolutionaryComponent comp, GetVerbsEvent<InnateVerb> args)
    {
        // Проверяем, можно ли конвертировать цель
        if (!comp.OnlyConsentConvert
            || !comp.ConvertAbilityEnabled
            || !args.CanAccess
            || !args.CanInteract
            || HasComp<RevolutionaryComponent>(args.Target)
            || !_mobState.IsAlive(args.Target)
            || HasComp<ZombieComponent>(args.Target))
        {
            return;
        }

        if (IsInConversionProcess(args.Target) || IsInConversionProcess(args.User))
            return;

        var alwaysConvertible = HasComp<AlwaysRevolutionaryConvertibleComponent>(args.Target);

        if ((!HasComp<HumanoidAppearanceComponent>(args.Target) ||
             !_mind.TryGetMind(args.Target, out var mindId, out var mind))
            && !alwaysConvertible)
        {
            return;
        }

        InnateVerb verb;

        if (HasComp<ConsentRevolutionaryCooldownComponent>(args.User))
        {
            // Если у конвертера есть кулдаун, показываем неактивный глагол
            verb = new InnateVerb
            {
                Disabled = true,
                Text = Loc.GetString("rev-verb-consent-convert-text"),
                Message = Loc.GetString("rev-verb-consent-convert-message-cooldown"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Imperial/Interface/Revolution/VerbIcons/revolution_convert.png")),
            };
        }
        else
        {
            // Активный глагол для обращения
            verb = new InnateVerb
            {
                Act = () =>
                {
                    // Запрещаем конвертацию, если цель недавно отказала
                    if (TryComp<ConsentRevolutionaryDenyComponent>(args.Target, out var denyComponent))
                    {
                        _popup.PopupEntity(
                            Loc.GetString(denyComponent.OnConversionAttemptText, ("target", Identity.Entity(args.Target, EntityManager))),
                            args.Target,
                            args.User);
                        return;
                    }

                    // Не скрываем глагол, если у цели есть mindshield или командная защита, чтобы не раскрывать механику
                    if (HasComp<MindShieldComponent>(args.Target) ||
                        HasComp<CommandStaffComponent>(args.Target))
                    {
                        _popup.PopupEntity(
                            Loc.GetString("rev-consent-convert-attempted-to-be-converted", ("user", Identity.Entity(args.User, EntityManager))),
                            args.User,
                            args.Target,
                            PopupType.MediumCaution);
                        _popup.PopupEntity(
                            Loc.GetString("rev-consent-convert-failed", ("target", Identity.Entity(args.Target, EntityManager))),
                            args.Target,
                            args.User,
                            PopupType.MediumCaution);
                        return;
                    }

                    // Проверяем, что цель все еще конвертируема
                    if (!_revRule.IsConvertable(args.Target))
                        return;

                    RequestConsentConversionToEntity(args.Target, args.User);
                },
                Text = Loc.GetString("rev-verb-consent-convert-text"),
                Message = Loc.GetString("rev-verb-consent-convert-message"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Imperial/Interface/Revolution/VerbIcons/revolution_convert.png")),
            };
        }

        args.Verbs.Add(verb);
    }

    public override void Update(float frameTime)
    {
        _accumulator += frameTime;

        if (_accumulator < AccumulatorThreshold)
            return;

        _accumulator -= AccumulatorThreshold;

        var query = EntityQueryEnumerator<ConsentRevolutionaryComponent>();
        while (query.MoveNext(out var uid, out var consentRev))
        {
            if (consentRev.IsConverter || consentRev.OtherMember == null)
                continue;

            if (!TryComp<ConsentRevolutionaryComponent>(consentRev.OtherMember, out var converterConsentRev))
            {
                consentRev.OtherMember = null;
                continue;
            }

            if (consentRev.RequestStartTime != null &&
                _timing.CurTime - consentRev.RequestStartTime > consentRev.ResponseTime)
            {
                CancelRequest((uid, consentRev),
                    (consentRev.OtherMember.Value, converterConsentRev),
                    reason: Loc.GetString("rev-consent-convert-failed-mid-convert-timeout"));
                continue;
            }

            if (!_transform.InRange(Transform(uid).Coordinates,
                    Transform(consentRev.OtherMember.Value).Coordinates,
                    consentRev.MaxDistance))
            {
                CancelRequest((uid, consentRev),
                    (consentRev.OtherMember.Value, converterConsentRev),
                    reason: Loc.GetString("rev-consent-convert-failed-mid-convert-out-of-range"));
                continue;
            }
        }
    }

    private void OnMobStateChanged(EntityUid uid, ConsentRevolutionaryComponent consentRev, MobStateChangedEvent args)
    {
        if (consentRev.OtherMember == null || !TryComp<ConsentRevolutionaryComponent>(consentRev.OtherMember, out var otherConsentRev))
            return;

        if (args.NewMobState == MobState.Alive)
            return;

        if (consentRev.IsConverter)
        {
            CancelRequest((consentRev.OtherMember.Value, otherConsentRev),
                (uid, consentRev),
                reason: Loc.GetString("rev-consent-convert-failed-mid-convert-not-alive"));
        }
        else
        {
            CancelRequest((uid, consentRev),
                (consentRev.OtherMember.Value, otherConsentRev),
                reason: Loc.GetString("rev-consent-convert-failed-mid-convert-not-alive"));
        }
    }

    private void OnMindRemoved(Entity<ConsentRevolutionaryComponent> ent, ref MindRemovedMessage args)
    {
        if (ent.Comp.OtherMember == null || !TryComp<ConsentRevolutionaryComponent>(ent.Comp.OtherMember, out var otherConsentRev))
            return;

        if (ent.Comp.IsConverter)
        {
            CancelRequest((ent.Comp.OtherMember.Value, otherConsentRev),
                ent,
                reason: Loc.GetString("rev-consent-convert-failed-mid-convert-not-alive"));
        }
        else
        {
            CancelRequest(ent,
                (ent.Comp.OtherMember.Value, otherConsentRev),
                reason: Loc.GetString("rev-consent-convert-failed-mid-convert-not-alive"));
        }
    }

    private void OnRoleAdded(Entity<ConsentRevolutionaryComponent> ent, ref RoleAddedEvent args)
    {
        if (ent.Comp.OtherMember == null ||
            ent.Comp.IsConverter ||
            !TryComp<ConsentRevolutionaryComponent>(ent.Comp.OtherMember, out var otherConsentRev) ||
            !HasComp<RevolutionaryComponent>(ent))
            return;

        CancelRequest((ent.Comp.OtherMember.Value, otherConsentRev), ent);
    }

    /// <summary>
    /// Запрос на обращение сущности
    /// </summary>
    /// <param name="target">Цель обращения</param>
    /// <param name="converter">Инициатор обращения</param>
    public void RequestConsentConversionToEntity(EntityUid target, EntityUid converter)
    {
        if (_mind.TryGetMind(target, out var consentMindId, out var _) &&
            _mind.TryGetSession(consentMindId, out var session))
        {
            _popup.PopupEntity(
                Loc.GetString("rev-consent-convert-requested", ("target", Identity.Entity(target, EntityManager))),
                converter,
                converter);

            var window = new ConsentRequestedEui(target, converter, _revRule, this, _popup, EntityManager);

            var targetComp = EnsureComp<ConsentRevolutionaryComponent>(target);
            targetComp.OtherMember = converter;
            targetComp.Window = window;
            targetComp.RequestStartTime = _timing.CurTime;
            targetComp.IsConverter = false;

            var converterComp = EnsureComp<ConsentRevolutionaryComponent>(converter);
            converterComp.OtherMember = target;
            converterComp.IsConverter = true;

            _euiMan.OpenEui(window, session);
        }
        else
        {
            _popup.PopupEntity(
                Loc.GetString("rev-consent-convert-auto-accepted", ("target", Identity.Entity(target, EntityManager))),
                converter,
                converter);
            _revRule.ConvertEntityToRevolution(target, converter);
        }
    }

    /// <summary>
    /// Проверяет, находится ли сущность в процессе обращения
    /// </summary>
    /// <param name="entity">Сущность для проверки</param>
    /// <returns>True, если в процессе обращения</returns>
    public bool IsInConversionProcess(EntityUid entity)
        => TryComp<ConsentRevolutionaryComponent>(entity, out var consentRev)
           && consentRev.OtherMember != null;

    /// <summary>
    /// Применяет кулдаун к конвертеру после обращения
    /// </summary>
    /// <param name="converter">Компонент конвертера</param>
    public void ApplyConversionCooldown(Entity<ConsentRevolutionaryComponent> converter)
    {
        _status.TryAddStatusEffect<ConsentRevolutionaryCooldownComponent>(converter,
            RevConvertCooldownStatusEffect,
            converter.Comp.ConversionBlockTime,
            true);
    }

    /// <summary>
    /// Применяет блокировку обращения к цели после отказа
    /// </summary>
    /// <param name="target">Компонент цели</param>
    public void ApplyConversionDeny(Entity<ConsentRevolutionaryComponent> target)
    {
        _status.TryAddStatusEffect<ConsentRevolutionaryDenyComponent>(target,
            RevConvertDeniedStatusEffect,
            target.Comp.RequestBlockTime,
            true);
    }

    /// <summary>
    /// Отменяет запрос на обращение
    /// </summary>
    /// <param name="target">Цель запроса</param>
    /// <param name="converter">Инициатор запроса</param>
    /// <param name="reason">Причина отмены, показываемая в попапах</param>
    public void CancelRequest(Entity<ConsentRevolutionaryComponent> target, Entity<ConsentRevolutionaryComponent> converter, string? reason = null)
    {
        if (reason != null)
        {
            _popup.PopupEntity(reason, target, target, PopupType.MediumCaution);
            _popup.PopupEntity(reason, converter, converter, PopupType.MediumCaution);
        }

        target.Comp.OtherMember = null;

        if (target.Comp.Window != null)
        {
            target.Comp.Window.Close();
            target.Comp.Window = null;
        }

        target.Comp.RequestStartTime = null;

        converter.Comp.OtherMember = null;
    }
}
