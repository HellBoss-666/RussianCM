using Content.Server.Damage.Components;
using Content.Server.Storage.Components;
using Content.Shared.Damage;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Damage.Systems
{
    public sealed class DamageModifierByItemSystem : EntitySystem
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DamageModifierByItemComponent, GetMeleeDamageEvent>(OnGetMeleeDamage);
        }

        private void OnGetMeleeDamage(EntityUid uid, DamageModifierByItemComponent component, ref GetMeleeDamageEvent args)
        {
            if (!_entityManager.TryGetComponent(uid, out EntityStorageComponent? storage))
                return;

            if (storage.Contents.ContainedEntities.Count == 0)
                return;

            int count = 0;
            foreach (var entity in storage.Contents.ContainedEntities)
            {
                if (!_entityManager.TryGetComponent(entity, out MetaDataComponent? meta))
                    continue;

                var protoId = meta.EntityPrototype?.ID;
                if (protoId != null && component.ItemIds.Contains(protoId))
                    count++;
            }

            if (count == 0)
                return;

            // Создаем новый DamageSpecifier вместо Clone()
            var damage = new DamageSpecifier();
            foreach (var (damageType, damageValue) in component.Damage.DamageDict)
            {
                damage.DamageDict[damageType] = damageValue * count;
            }

            args.Damage += damage;
        }
    }
}
