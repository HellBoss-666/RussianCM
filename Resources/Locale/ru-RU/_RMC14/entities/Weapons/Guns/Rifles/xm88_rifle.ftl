### Основное оружие ###
ent-WeaponRifleXM88 = Тяжёлая винтовка XM88
ent-WeaponRifleXM88-desc = Экспериментальная переносная противо-материальная винтовка под патрон .458 SOCOM. Требует ручного перезаряжения после каждого выстрела.

### Боеприпасы ###
ent-RMCCartridge458SOCOM = Патрон .458 SOCOM
ent-RMCCartridge458SOCOM-desc = Крупнокалиберный патрон для тяжёлой винтовки XM88.

ent-RMCBox458SOCOM = Ящик патронов .458 SOCOM
ent-RMCBox458SOCOM-desc = Ящик с патронами .458 SOCOM для тяжёлой винтовки XM88.

### Характеристики ###
xm88-features =
    • Ручное перезаряжение
    • Мощный противо-материальный патрон
    • Ограниченная ёмкость (9 патронов)

### Слоты и компоненты ###

ent-WeaponRifleXM88-slot-rmc-aslot-stock-name =
    { -slot-rmc-aslot-stock-name }

ent-WeaponRifleXM88-slot-gun-magazine-name =
    { -slot-gun-magazine-name }

ent-WeaponRifleXM88-slot-rmc-aslot-underbarrel-name =
    { -slot-rmc-aslot-underbarrel-name }

ent-WeaponRifleXM88-slot-rmc-aslot-barrel-name =
    { -slot-rmc-aslot-barrel-name }

ent-WeaponRifleXM88-slot-rmc-aslot-rail-name =
    { -slot-rmc-aslot-rail-name }

### Режимы стрельбы ###
ent-WeaponRifleXM88-fire-mode-semiauto =
    { -fire-mode-semiauto }

### Камуфляж ###
ent-WeaponRifleXM88-camouflage-variation-Jungle =
    { -camouflage-variation-Jungle }

ent-WeaponRifleXM88-camouflage-variation-Desert =
    { -camouflage-variation-Desert }

ent-WeaponRifleXM88-camouflage-variation-Snow =
    { -camouflage-variation-Snow }

ent-WeaponRifleXM88-camouflage-variation-Classic =
    { -camouflage-variation-Classic }

ent-WeaponRifleXM88-camouflage-variation-Urban =
    { -camouflage-variation-Urban }

### Действия ###
gun-reload-insert = Патрон заряжен
gun-pump-action = Затвор перезаряжен
gun-wield-delay = Готовность к стрельбе: 0.4с
gun-heavy-penalty = Замедление при ношении: -27.5%

### Технические термины ###
ammo-type-458socom = .458 SOCOM
ammo-count-xm88 = { $count ->
    [one] { $count } патрон
    [few] { $count } патрона
   *[many] { $count } патронов
} .458 SOCOM
