# Natural selection - food chains and ecosystems

Aimed to make the game's ecosystem feel more alive, immersive and semi-realistic.

The mod adds plenty of interactions between entities by modifying their behaviors, adding some and rewamping entities to make them feel more like an actual living organisms rather than just another dangerous liveforms trying to have a piece of you. Or if you just want enemies to kill eachother you can!

## Config options

Many mechanics, behaviors an settings can be toggled and tweaked with config with wide variety of options.

#### Options include:
- Toggles to enable/disable behaviors
    - Applied on loading the mod. Requires restart.
- Blacklists
    - Automatically generated on booting up the game. Requires restart.
- Debug toggles
    - Can be changed at runtime with config mods (LethalConfig, LethalSettings).
    - For better bug reports, turn the toggles on when you encounter bugs or unexpected behaviors.
- __Compatibilities__
    - Enhanced Monsters and SellbodiesFixed
    - Rexuvination

## Branches
###  **Stable branch: [Natural selection - food chains and ecosystems](https://thunderstore.io/c/lethal-company/p/Fandovec03/Natural_selection/)**
###  **Pre-release branch: [Experimental Enemy Interactions](https://thunderstore.io/c/lethal-company/p/Fandovec03/ExperimentalEnemyInteractions)**

## Currently supported entities:

#### Red bees (Circuit bees)

- Bees become aggressive and attack any animal or entity that get too close to their hive.
- Enraged bees swarm any enemy in their LOS to death!
- **Special interaction:** Bees have a chance to set **Forest Keepers** on fire!

#### Hygrodere (Blob)

- Hygrodere seeks and move to nearest organic matter whenever be it be living being or a corpse.
- Rapidly dissolves and consume the matter that makes contact with it.

#### Earth Leviathan (Sandworm)

- Leviathans actively seek and hunt any organism nearby.
- Does not hunt prey that is too small in size (Below the size of an employee).

#### Bunker Spider
- Spider investigates and hunts down creatures caught by it's webs
- Likes to hunt smaller enemies like hoarding bugs and snare fleas

#### Spider web

- Spider webs stick to and slow down entities passing through it.
- Alerts the spider.


## Modified enemies:
#### Forest keeper:

- Forest Keepers have a chance to extinguish and survive being set on fire although severely weakened.
- Forest Keeper's corpse last longer on fire.

### Compatible mods

- **Enhanced Monsters/SellbodiesFixed**
    - Added support for blob and future enemies to interact with corpses
- **LobbyCompatibility**
- **ReXuvination**
- **PathfindingLib**
    - Used in library
- **StarlanceAIFix**
    - Indirect compatibility

### Unfinished and WIP:

## TODO:
- Nutcracker
- Spore Lizard
- Hoarder bug

**Report bugs on either [Github issues](https://github.com/Fandovec03/LC-NaturalSelection) or the [Lethal company modding discord](https://discord.com/channels/1168655651455639582/1293353988913233991).**

## Credits:
- Zesa, TKronix, Swaggies: helping me figure out collision detection
- Hamunii: Helped fixing the variables
- EnzoTheProtogen: Reporting a bug
- TheShyestGuy: Bug reports
- Robyn and Xilophor: Pointing out flawed networking
- DiFFoZ: Finding the problem causing bees to turn invisible
- Quain808: Bug reports
- Lunxara: Playtesting and bug reports
- Zaggy1024: Help debuging and correctly integrating PathfindingLib into library