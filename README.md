# Clone Wars: Fleet Survival

A free, single-player Windows fleet-command survival prototype for Unity 2022.3.62f3.

## Play

[Download the Windows v0.8.0 prototype](https://github.com/JokerIllusive/CloneWarsFleetSurvival/releases/tag/v0.8.0).
Under Assets, download `CloneWarsFleetSurvival-Windows.zip`, extract all files, and
run `CloneWarsFleetSurvival.exe`. Keep its `_Data` directory, `UnityPlayer.dll`, and
`MonoBleedingEdge` together. Unity is not required to play.

The Windows game launches in borderless fullscreen at your monitor's desktop resolution,
including when upgrading from an older windowed build. Alt+Enter switches between
fullscreen and windowed mode; Alt+F4 closes the game.

Choose the Galactic Republic or Separatist Alliance. Each run begins with a command ship,
two cruisers/frigates, two fighter squadrons, and 220 salvage. Position your ships, buy
reinforcements, and launch a wave. Destroyed enemies and cleared waves grant salvage.
Shields recharge after avoiding damage and refill between waves. Hulls require repairs.
Survive as long as possible; losing the command ship ends the run. Every fifth wave adds
an enemy flagship, and later waves introduce destroyers and carriers.

## Controls

| Action | Control |
| --- | --- |
| Select allied ships | Left click or drag a selection box |
| Add/remove selection | Shift + left click |
| Move selected ships in formation | Right click empty space |
| Focus fire | Right click an enemy ship |
| Attack-move | F, then right click a destination |
| Select the whole fleet | Tab |
| Select and center the command ship | Q |
| Pan camera | WASD, arrow keys, or middle mouse drag |
| Jump/pan across the sector | Left click or drag on the minimap |
| Expand/collapse controls guide | H or the top-right ? button |
| Zoom | Mouse wheel |
| Pause/resume | Space or Esc |
| Choose fighter / frigate / destroyer call-in | 1 / 2 / 3, during combat or between waves |
| Confirm reinforcement arrival point | Left click clear space while placing a ship |
| Cancel reinforcement placement | Right click or Esc |
| Repair fleet | R, between waves |
| Recover selected squadron | Backspace or Recover in the hangar panel |

Ships automatically fire at nearby targets. The command panel includes additional
reinforcement types, weapon refits, sound controls, and a tactical map.

## Tactical readouts

The command bar and reinforcement dock are compact, leaving more space for the
battlefield. All six reinforcement cards fit in one row and retain their baseline
ship stats. Repairs, refits, and wave launch sit together on the right. The controls
guide opens on demand. The minimap includes a sector grid, boundary, and live camera
footprint; selected units are white, allies blue, and enemies orange. Healthy
unselected ships hide their world status bars; selected, damaged, and command ships
keep their readouts.

The left battle tally separates enemy capitals destroyed, enemy individual fighters
destroyed, friendly capitals lost, and friendly individual fighters lost. It shows
the current wave and the whole run. Losing a squadron does not double-count its
fighters; repairs restore craft without erasing earlier losses. A new wave resets
only the wave column, and a new run resets both columns.

Reinforcement buttons show salvage cost, fleet capacity, base damage per second, hull, and shields.
Arquitens pair costs cover both ships; its damage, hull, and shield values are per
cruiser. Fighter button values are for a full six-craft squadron.

Selecting one unit shows current and maximum hull/shields, effective damage per
second, range, damage-adjusted speed, and hull condition. Its refit count and actual
weapon bonus are explicit: each refit adds 10% of base weapon damage, so two add
20%. Each unit accepts at most ten refits, capping its weapon bonus at +100%
(double base damage). Refits cost 200 salvage; capped ships stay unchanged and a
fully capped fleet cannot spend salvage on another refit.
Effective damage includes damaged-hull penalties and surviving fighter count.
Selecting several units shows combined hull, shields, damage, and refitted unit count.
Refits apply to active friendly units present when purchased; new and arriving
reinforcements require a later refit.

## Combat and reinforcements

Ships fire staggered volleys from multiple muzzle positions. Fighters use paired
wing cannons; capital ships use representative major twin batteries, with more
barrels on larger ships. Each volley divides the existing weapon damage across
its bolts to preserve combat balance. Independent rotating turret targeting and
every small defensive emplacement are not modeled.

Republic bolts are blue and CIS bolts are red, following the space battle visuals
in the [official Clone Wars Christophsis reference](https://www.starwars.com/databank/Christophsis).
Venators and Munificents use short firing samples edited from the supplied
`Venator.wav` and `Munificent Class.wav` recordings. ARC-170 and Vulture squadrons
use their own supplied recordings. V-19 interceptors temporarily use the supplied
V-Wing firing recording. Each weapon profile has two sample variations. Audio
plays once per volley so all battery bolts do not stack the same recording.

Hyperspace uses the supplied recording, edited into a 2.2-second charge and a
separate exit effect. Extracts are mono 44.1 kHz PCM, with trimmed silence,
normalized levels, and short edge fades. Acclamator and Arquitens firing profiles
are edited from Venator samples; Providence, Recusant, and Lucrehulk profiles use
Munificent samples. Pitch, duration, and bass weight vary by hull while keeping
the same faction sound character. Every ship now has recording-based weapon
audio. Explosions retain synthesized effects. Tactical pause pauses battle audio. Extract timestamps and
source hashes are recorded in `Assets/FleetSurvival/Resources/Audio/Sources.json`.
The additional profiles are documented in `Resources/Audio/DerivedSounds.json`.

Use the reinforcement panel or 1 / 2 / 3, then left-click an arrival point.
A cyan hologram marks clear space; red means the position is blocked or outside
the sector. Salvage is spent only when placement succeeds. Incoming ships reserve
their full capacity during the charge and warp, counted once after they spawn.
The fleet has a 22-capacity budget:

| Unit | Capacity |
| --- | --- |
| Six-fighter or interceptor squadron | 1 |
| Arquitens or Munificent escort | 2 |
| Acclamator or Munificent frigate | 3 |
| Venator or Recusant destroyer | 4 |
| Command flagship | 5 |
| Venator carrier | 6 |
| Lucrehulk carrier | 8 |

The starting fleet uses 13 capacity. Losing a unit releases its full capacity;
losing one fighter inside a surviving squadron does not release its squadron slot.

An Arquitens call-in brings two cruisers at separated arrival points for 280
salvage (140 each), using four capacity in total. Both positions must be clear before
salvage is charged. The CIS Munificent escort remains a single-ship call-in.

After a short charge, the ship exits hyperspace with a stretched hull and light
wake, completing its arrival in about three seconds. It cannot fight or take
damage during arrival. If the landing point becomes blocked, the ship tries nearby
space; if no clear position is available, the call-in refunds its salvage.
Repairs and weapon refits remain available between waves.

## Orbital environment

The sector now overlooks Geonosis: rusty desert terrain, rocky ridges, craters,
sparse hive lights, and a rocky orbital ring, following the
[official Geonosis reference](https://www.starwars.com/databank/geonosis).
Lighting follows the sector's starlight, with a day/night boundary, warm thin
atmosphere, and drifting dust haze. The planet uses original procedural terrain
and an artistic orbital map.

The backdrop uses three layers of fine stars with varied brightness and color,
over a subtle teal and violet nebula. It surrounds the camera, so panning and
zooming retain a continuous space background. The detailed sector has 112 irregular
asteroids, with larger rocks and denser belts along its edges, plus two abandoned
orbital shipyards with docking gantries, trusses, service lights, and 24 drifting
scrap pieces. Subtle particulate clouds add depth behind the battle plane. Desert
lighting adds small terrain relief, and the orbital ring has clumps and gaps rather
than uniform stripes. The full battlefield grid is replaced with a thin perimeter
and ticks; the minimap retains the navigational grid. Planet rotation,
dust drift, and asteroids stop during tactical pause. The environment has no colliders and does not
block combat or reinforcement placement.

## Movement and formations

Capital ships accelerate gradually, brake before arriving, and turn through wide
arcs. Cruisers and escorts handle faster; fighters and interceptors make much
tighter turns in combat. Ships fly nose-first, with each imported model aligned
to the game's forward direction.

Idle fighter squadrons fly small staggered holding loops with banking and gentle
vertical movement around their squadron anchor. Orders and combat blend the craft
back into flight formation. Weapon muzzle positions follow the actual animated
craft. Holding flight pauses with the battle and excludes destroyed fighters.

A move order assigns the selected ships separate slots in a shared formation.
The formation travels at the pace of its slowest member and slows further if ships
fall behind. Spacing accounts for the largest hull, and the destination is adjusted
to keep the group inside the sector. Attack-move holds the advance while engaging;
focus fire or a new order can detach selected ships from their previous formation.

Friendly units with move orders retain blue ship silhouettes and heading chevrons
at their final formation destinations, joined to their current positions by thin
route lines. These show final slots rather than the formation's moving intermediate
slots. They disappear on arrival, focus fire, destruction, or leaving the battle,
and a new command updates only the commanded ships. They do not block selection.

## Ships

- Republic: detailed Venator command ship, Venator destroyer/carrier, Acclamator,
  Arquitens, ARC-170, and V-19 Torrent.
- CIS: Providence command ship, Munificent frigate/escort, Recusant,
  Lucrehulk carrier, and Vulture droid squadrons.

The first supplied Venator is preserved in the source project and model preview.
The detailed second Venator is used in gameplay. Carrier hangars supplement the
hyperspace reinforcement panel.

Fighter and interceptor units each contain six individual craft, selected and
ordered as one squadron. Each craft has its own hull allocation and paired guns.
Hull damage removes individual fighters and their gunfire; a squadron survives
until its last fighter is lost. Repairs restore missing craft between waves.
The original squadron damage budget is shared across the six fighters, so
surviving squads lose damage output as they lose members. Squadrons share their
shield pool and maneuver as a group. Fighter models are reduced for the tactical
view; the ships use gameplay scale rather than literal kilometer-to-meter ratios.

## Carrier hangars and squadron roles

Select a Venator or Lucrehulk carrier to open its hangar panel. Republic carriers
have two squadron bays; CIS carriers have three. Command ships have one bay. Launch
Fighter (55 salvage), Interceptor (45), or Strike (75) squadrons. Each contains six
craft and occupies one fleet capacity, even while launching or docked. Launching
takes two seconds; the deck has a 12-second cooldown. Bays remain occupied by their
deployed squads, so the same carrier cannot launch an unlimited fleet.

Select a squadron and press Backspace or Recover to return it to its assigned
carrier. Purchased squadrons can use a free bay on a nearby friendly carrier.
Returning craft remain vulnerable until docking. Docked squads cannot fight or
be targeted; their ship stats remain available through the carrier's bay buttons.
Surviving hull and shields repair gradually. Every six seconds, the hangar can
replace one lost craft for eight salvage; no salvage means no replacement. Wait at
least six seconds after docking and for the launch deck to cool before relaunching.
Relaunch preserves the squadron's identity, refits, and fleet capacity. Losing a
carrier destroys its docked craft, while deployed squadrons remain in the battle.

Interceptors prefer hostile squadrons and deal 1.6x damage to craft, but only .55x
to capitals. Fighters screen the fleet, prefer hostile strike squads, and deal
1.2x damage to craft / .85x to capitals. Strike loadouts fire one slower torpedo per
surviving craft, dealing 1.9x damage to capitals / .35x to craft. Capital guns deal
.6x damage to squadrons. The unit panel shows baseline effective DPS; these target
modifiers apply on firing. Explicit focus-fire orders override target preference.
Strike loadouts use the supplied ARC-170 and Vulture models; dedicated bomber
models are not included. Their refits still add 10% of their own base weapon damage
per purchase and stop at +100%.

Enemy carriers hold at standoff range, escorts screen carriers when threats are
distant, and frigates/destroyers use offset approaches. Later waves introduce
screening escorts, strike loadouts, and carriers. Enemy carriers begin launching
after a 12-second deck delay and have a finite reserve of one squadron per bay for
that wave. The preparation header previews the next wave's main ship roster;
carrier-launched wings are additional reserves. Hangar animation, cooldowns,
servicing, and enemy launches stop during tactical pause.

## Procedural destruction

Imported hull triangles are partitioned into six sections during authoring, with
smaller shared armor fragment meshes for each section. At 65%, 35%, and 15%
remaining hull, the area nearest the damaging hit becomes scorched, small armor
fragments fly away, and breach fires appear. The hull remains visible and ship
performance degrades. Armor fragments have an inner skin and capped borders so
they retain visible thickness as they spin. Debris spins and drifts for a limited
time; tactical pause freezes its motion.
Repairing a surviving ship clears the scorches and fires and restores its stats.

Destroyed capital ships leave recognizable charred wrecks for about 18 seconds.
After the final blast, the husk separates into three larger groups of hull sections
that gently drift apart at less than 0.4 world units per second relative to the wreck,
with slow rotation. Whole-wreck forward drift is capped at 0.25 units per second.
Small wreck armor fragments also drift slowly, rather than being thrown across the
sector. All large hull sections remain visible until the wreck fades and despawns;
the separated parts stop during tactical pause and clear when leaving the battle.
Five staggered secondary explosions shed smaller pieces without hiding half the
hull. About 28% of capital wrecks suffer a reactor failure, showing a 2.6-second
countdown and an orange danger ring before a larger detonation. The blast damages
nearby ships on either side, with distance falloff, and can start further delayed
ship destructions. Ships in hyperspace are immune. Ordinary wreck sequences end
after 1.8 seconds; a wave clears after its pending wreck blasts finish. Tactical
pause freezes countdowns, particles, and debris. Each wreck releases at most 18
armor fragments. Fighter destruction remains a smaller, immediate breakup.

This uses shared pre-baked hull and armor meshes rather than arbitrary cutting at
each impact. The remaining hull stays visible through the staged explosions.
Gameplay textures are capped at 1024 pixels and compressed; original GLBs are
preserved for future art changes. Independent rotating turret targeting is not
implemented in this prototype.

## Edit and build

Open this folder in Unity 2022.3.62f3. Open `Assets/FleetSurvival/FleetSurvival.unity`
and press Play. The saved scene boots the fleets and Canvas interface at runtime.

The editor method `FleetSurvival.Editor.FleetBuilder.Build` creates a Windows build.
`PreviewCompleteFleet` renders the model board and regenerates normalized prefabs.
`HullSectionBaker` builds the shared hull sections and optimized materials. Bootstrap
scripts are editor-only and do not ship in the player.

Model attributions and source links are in `THIRD_PARTY_ASSETS.md`. Providence and
Recusant use CC BY-NC 4.0; the supplied prototype has no monetization. Other supplied
models use CC BY 4.0. This is an unofficial fan prototype.

## Source control

Source repository: https://github.com/JokerIllusive/CloneWarsFleetSurvival

Large GLB source assets use Git LFS. Install Git LFS before cloning so the ship
models download along with the Unity project:

```sh
git lfs install
git clone https://github.com/JokerIllusive/CloneWarsFleetSurvival.git
cd CloneWarsFleetSurvival
git lfs pull
```

Open the cloned folder in Unity 2022.3.62f3. Generated Library files and Windows
builds are excluded from source control.
