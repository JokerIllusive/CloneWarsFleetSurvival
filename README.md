# Clone Wars: Fleet Survival

A free, single-player Windows fleet-command survival prototype for Unity 2022.3.62f3.

## Play

[Download the Windows v0.5.1 prototype](https://github.com/JokerIllusive/CloneWarsFleetSurvival/releases/tag/v0.5.1).
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
| Zoom | Mouse wheel |
| Pause/resume | Space or Esc |
| Choose fighter / frigate / destroyer call-in | 1 / 2 / 3, during combat or between waves |
| Confirm reinforcement arrival point | Left click clear space while placing a ship |
| Cancel reinforcement placement | Right click or Esc |
| Repair fleet | R, between waves |

Ships automatically fire at nearby targets. The command panel includes additional
reinforcement types, weapon refits, sound controls, and a tactical map.

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
a fleet slot, so queued reinforcements count toward the 22-unit capacity. Each
capital ship occupies one slot; each six-fighter squadron occupies one slot.

An Arquitens call-in brings two cruisers at separated arrival points for 280
salvage (140 each), occupying two slots. Both positions must be clear before
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
zooming retain a continuous space background. Thirty-six irregular asteroids
drift and tumble behind the battle plane at varied speeds. Planet rotation,
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

## Ships

- Republic: detailed Venator command ship, Venator destroyer/carrier, Acclamator,
  Arquitens, ARC-170, and V-19 Torrent.
- CIS: Providence command ship, Munificent frigate/escort, Recusant,
  Lucrehulk carrier, and Vulture droid squadrons.

The first supplied Venator is preserved in the source project and model preview.
The detailed second Venator is used in gameplay. Carrier is a durable ship class in
this version; deploying fighter squadrons uses the reinforcement panel.

Fighter and interceptor units each contain six individual craft, selected and
ordered as one squadron. Each craft has its own hull allocation and paired guns.
Hull damage removes individual fighters and their gunfire; a squadron survives
until its last fighter is lost. Repairs restore missing craft between waves.
The original squadron damage budget is shared across the six fighters, so
surviving squads lose damage output as they lose members. Squadrons share their
shield pool and maneuver as a group. Fighter models are reduced for the tactical
view; the ships use gameplay scale rather than literal kilometer-to-meter ratios.

## Procedural destruction

Imported hull triangles are partitioned into six sections during authoring, with
smaller shared armor fragment meshes for each section. At 65%, 35%, and 15%
remaining hull, the area nearest the damaging hit becomes scorched, small armor
fragments fly away, and breach fires appear. The hull remains visible and ship
performance degrades. Only total destruction makes the large hull sections break
away. Debris spins and drifts for a limited time; tactical pause freezes its motion.
Repairing a surviving ship clears the scorches and fires and restores its stats.

This is section-based breakup rather than arbitrary slicing at every impact. It
preserves the supplied meshes and textures while avoiding mesh generation during
combat. Gameplay textures are capped at 1024 pixels and compressed; original GLBs
are preserved for future art changes. Full fracture caps and independent turret
targeting are not implemented in this prototype.

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
