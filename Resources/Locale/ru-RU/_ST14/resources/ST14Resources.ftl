st14-resource-neft-name = нефть
st14-resource-armium-name = армиум
st14-resource-auron-name = аурон

st14-capsule-examine-empty = Капсула пуста. Ёмкость: [color=yellow]{ $capacity }[/color] ед.
st14-capsule-examine-contents = Внутри { $resource }: [color=yellow]{ $amount }[/color] / { $capacity } ед.

st14-extractor-no-capsule = Нет капсулы.
st14-extractor-capsule-full = Капсула уже заполнена.

st14-extractor-no-field = Здесь нет подходящего месторождения.

st14-extractor-examine-idle = Выключатель в исходном положении, горит [color=green]зелёным[/color].
st14-extractor-examine-extracting = Идёт добыча, выключатель горит [color=red]красным[/color].
st14-extractor-examine-modifier = Модификатор добычи: [color=yellow]{ $modifier }%[/color].

ent-ST14NeftField = нефтяное месторождение
    .desc = Тёмная нефть сочится из-под земли. Здесь можно построить станок-качалку.
ent-ST14ArmiumField = месторождение армиума
    .desc = Лужа синего жидкого металла. Здесь можно построить бур армиума.
ent-ST14AuronField = месторождение аурона
    .desc = Из грунта сочится жёлтый газ. Здесь можно построить бур аурона.

ent-ST14ResourceCapsule = капсула для ресурсов
    .desc = Герметичная капсула для переноски добытых ресурсов.
ent-ST14ResourceCapsuleArmiumFilled = { ent-ST14ResourceCapsule }
    .desc = { ent-ST14ResourceCapsule.desc }
ent-ST14ResourceCapsuleAuronFilled = { ent-ST14ResourceCapsule }
    .desc = { ent-ST14ResourceCapsule.desc }
ent-ST14ResourceCapsuleNeftFilled = { ent-ST14ResourceCapsule }
    .desc = { ent-ST14ResourceCapsule.desc }

ent-ST14ArmiumDrill = бур армиума
    .desc = Добывает армиум из месторождения во вставленную капсулу.
ent-ST14AuronDrill = бур аурона
    .desc = Добывает аурон из месторождения во вставленную капсулу.
ent-ST14NeftPumpJack = станок-качалка
    .desc = Качает нефть из месторождения во вставленную капсулу.

st14-extractor-ui-insert = Вставить
st14-extractor-ui-eject = Извлечь
st14-extractor-ui-turn-on = Включить
st14-extractor-ui-turn-off = Выключить

st14-extractor-capsule-slot-1 = капсула (слот 1)
st14-extractor-capsule-slot-2 = капсула (слот 2)

st14-extractor-ui-status-off = [color=#D9534F][bold]• Отключено[/bold][/color]
st14-extractor-ui-status-working = [color=#5CB85C][bold]• Работает[/bold][/color]
st14-extractor-ui-status-no-space = [color=#F0AD4E][bold]• Нет места[/bold][/color]
st14-extractor-ui-rate = +{ $rate } ед/сек
st14-extractor-ui-rate-none = —
st14-extractor-ui-feed = Подача
st14-extractor-ui-feed-slot-1 = ← Слот 1
st14-extractor-ui-feed-slot-2 = Слот 2 →
st14-extractor-ui-auto-switch = Автопереключение
st14-extractor-ui-slot-amount = { $amount } / { $capacity }
st14-extractor-ui-slot-empty = пусто
