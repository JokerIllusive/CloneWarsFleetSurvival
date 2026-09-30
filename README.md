# Clone Wars: Fleet Survival

A free, single-player Windows fleet-command survival prototype for Unity 2022.3.62f3.

## Play

Run `CloneWarsFleetSurvival.exe` from the supplied Windows folder. Keep the executable,
its `_Data` directory, `UnityPlayer.dll`, and `MonoBleedingEdge` together.

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
| Buy fighter / frigate / destroyer | 1 / 2 / 3, between waves |
| Repair fleet | R, between waves |

Ships automatically fire at nearby targets. The command panel includes additional
reinforcement types, weapon refits, sound controls, and a tactical map.

## Movement and formations

Capital ships accelerate gradually, brake before arriving, and turn through wide
arcs. Cruisers and escorts handle faster; fighters and interceptors make much
tighter turns in combat. Ships fly nose-first, with each imported model aligned
to the game's forward direction.

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
