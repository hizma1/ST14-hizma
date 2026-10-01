st14-resource-neft-name = oil
st14-resource-armium-name = armium
st14-resource-auron-name = auron

st14-capsule-examine-empty = The capsule is empty. Capacity: [color=yellow]{ $capacity }[/color] u.
st14-capsule-examine-contents = Contains { $resource }: [color=yellow]{ $amount }[/color] / { $capacity } u.

st14-extractor-no-capsule = There is no capsule.
st14-extractor-capsule-full = The capsule is already full.

st14-extractor-no-field = There is no suitable field here.

st14-extractor-examine-idle = The switch is off and glowing [color=green]green[/color].
st14-extractor-examine-extracting = Extracting, the switch is glowing [color=red]red[/color].
st14-extractor-examine-modifier = Extraction modifier: [color=yellow]{ $modifier }%[/color].

ent-ST14NeftField = oil field
    .desc = Dark oil seeping up from underground. A pump jack can be built here.
ent-ST14ArmiumField = armium field
    .desc = A pool of blue liquid metal. An armium drill can be built here.
ent-ST14AuronField = auron field
    .desc = Yellow gas seeping from the ground. An auron drill can be built here.

ent-ST14ResourceCapsule = resource capsule
    .desc = A sealed capsule for carrying extracted resources.
ent-ST14ResourceCapsuleArmiumFilled = { ent-ST14ResourceCapsule }
    .desc = { ent-ST14ResourceCapsule.desc }
ent-ST14ResourceCapsuleAuronFilled = { ent-ST14ResourceCapsule }
    .desc = { ent-ST14ResourceCapsule.desc }
ent-ST14ResourceCapsuleNeftFilled = { ent-ST14ResourceCapsule }
    .desc = { ent-ST14ResourceCapsule.desc }

ent-ST14ArmiumDrill = armium drill
    .desc = Drills armium from a field into an inserted capsule.
ent-ST14AuronDrill = auron drill
    .desc = Drills auron from a field into an inserted capsule.
ent-ST14NeftPumpJack = pump jack
    .desc = Pumps oil from a field into an inserted capsule.

st14-extractor-ui-insert = Insert
st14-extractor-ui-eject = Eject
st14-extractor-ui-turn-on = Turn on
st14-extractor-ui-turn-off = Turn off

st14-extractor-capsule-slot-1 = capsule (slot 1)
st14-extractor-capsule-slot-2 = capsule (slot 2)

st14-extractor-ui-status-off = [color=#D9534F][bold]• Off[/bold][/color]
st14-extractor-ui-status-working = [color=#5CB85C][bold]• Working[/bold][/color]
st14-extractor-ui-status-no-space = [color=#F0AD4E][bold]• No space[/bold][/color]
st14-extractor-ui-rate = +{ $rate } u/s
st14-extractor-ui-rate-none = —
st14-extractor-ui-feed = Feed
st14-extractor-ui-feed-slot-1 = ← Slot 1
st14-extractor-ui-feed-slot-2 = Slot 2 →
st14-extractor-ui-auto-switch = Auto-switch
st14-extractor-ui-slot-amount = { $amount } / { $capacity }
st14-extractor-ui-slot-empty = empty
