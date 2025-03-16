# Experimental Enemy Interactions

## 0.5.1
- **Updated CHANGELOG Formatting**

#### Internal changes
- Updated strings to use interpolation
- Updated sandworm's networking. It's network connections and variables will now be included for disposal
- Replaced game's code from the mod
 
#### Experimental
- Added experimental hit registrer

## 0.5.0
- Updated blacklists to delete empty entries
- Added blacklist for spider webs
- Added web speed modifier config. Config generates automatically
- Spider webs can now be enabled with own toggle

## 0.4.0 
- Prerelease of 0.2.4

## 0.3.0
- See 0.2.0
- NaturalSelectionLib 0.6.0 included

## 0.2.6
- Finally discovered a fix for bees
- Hygrodiges move towards and consume corpses
- Pathfinding broken (WIP)
- Added config options
    - Ignore immortal enemies
    - Set chances for bees to set giants on fire
    - Set chance for giant to extinguish themself

## 0.2.5
- Merged bees fix and global enemy list branches

## 0.2.4
- Added config for the bees fix

## 0.2.3
- Experimental fix for bees

## 0.2.2
- Implemented experimental global enemy list system. Every enemy now shares the same enemy list between eachother. Hopefully this will improve performance

## 0.2.1
- Updated to latest branch
- Implemented experimental global enemyList

## 0.2.0
- Updated to the Stable release!

## 0.1.28
- Finally fixed the root issue. Enemies (Mainly bees) no longer target themselves or their own kind anymore.

## 0.1.27
- This time for real fixed bees targeting themself.

## 0.1.26
- Fixed audio issues with sandworm
- Sandworms revised. They're now using a proper behavior state (Due to technical difficulties they use custom state instead of vanilla when targetting enemies)
- Reenabled Sandworm patches (Make sure you have Load leviathan patches in Initialization settings checked)
- Fixed bees and other entities targetting themselves (unconfirmed. Credit to EnzoTheProtogen for reporting the issue)
- Bee now ignore Locusts and Sandworms (untested)
- Log improvements
- Added new config settings

## 0.1.25
- Added more config options
- ~~Rewamped sandworm to use proper behavior states~~
- Sandworm temporarily disabled due to audio issues (can be manually enabled)

## 0.1.24
- Added log option to include/exclude spammy logs
- Sandworms updated:
    - Addressed a bug with Worms trying to move with disabled agent
    - Added more arguments preventing skipping of vanilla code while emerging/emerged
    - Worms no longer target dead enemies that were previously targetted before death
    - Observed some weird behavior but works for now. Will fix later.

- Fixed ArgumentOutOfRangeException where enemyList tried to check LOS to previously removed enemy.

## 0.1.23
- Giants have a chance to extinguish themselves, though they will be severely weakened by the fire
- Few improvements to bees
    - Bees no longer target dead enemies
    - DoAIInterval is not skipped on behavior state 0
    - fixed an exception regarding dictionary count
- Changed some logs

## 0.1.22
- Fixed bees pathfinding being broken in state 2
- The fire on forest giant no longer extinguishes itself the moment giant dies
- Added special interaction between Red bees and Forest giant

## 0.1.21
- Fixed bees throwing LOS exceptions when no enemy is in LOS
- Fixed typo disabling vanilla code to be disabled and bees (the particles) not change the state
- Temporarily disabled custom state 2 on bees due to bees breaking. Bees will revert to vanilla state 2

## 0.1.20
- Fixed typo causing Sandworms to throw keynotfoundExceptions
- Fixed changelog format

## 0.1.19
- Forgot to upload updated DLL. All changes bellow apply with this version

## 0.1.18
-  an issue where the mob tries to add itself again into dictionary causing error (This fixes the error when paired with lethalmon)
- Fixed the script trying to apply patch via original Spider class instead of the patched one

## 0.1.17
- Fixed safe mode accidentally preventing load of The bee patch

## 0.1.16
- Potencially fixed same exception on blobAI

## 0.1.15
- Emergency fix: Fixed Key not foudn exception causing bees to not work

## 0.1.14
- Added many more config options

- **Safe mode:** 
    - On by default. Prevents unfinished and experimental scripts from loading.
- **Bees added!**
    - Bees now target every enemy in their LOS.
- Several bugs and oversights fixed

## 0.1.13
- Removed code breaking spider with the position fix

## 0.1.12
- Fixed sandworms affecting eachother for good! [Credit to Hamunii]
- Some QOL changes for development

## 0.1.11
- Hygrodere now does not anger Hoarding Bugs
- Hygrodere should anger Bracken less
- **Added a dependency to fix spider getting stuck and other position issues**
- Converted most debug logs to DebugLog
- Fixed Hygrodere hitting enemies with custom hit trigger stupidly fast

## 0.1.10
- Rewritten Earth leviathan patch code. Now sandworms behave as intended.
- **known bugs: Audio not playing when chasing targets. Audio cutting off when chasign player.**

## 0.1.9
- Fixed critical error causing hard crash on load caused by leftover Lobby Compatibility code

## 0.1.8
- Regenerated DLL forgot to regenerate after changing versions

## 0.1.7
- Moved functions and methods of enemyList to EnemyAI. This makes development much faster and gives a potencial for a memory as a side effect, though that has to be implemented first in each enemyAI type.
- Resolved NullException error messages in the Collision patch
- **Earth leviathan** is now implemented. Leviathan now targets and consumes other surface creatures.

## 0.1.6
- fixed typos and formatting in CHANGELOG and README
	
## 0.1.5
- Fixed collisions not working
- Much less logs spam
- **Hygrodere now eats almost everything alive!**
- Spider deals 1 damage when Enemy has 2 or less health
- Added config file
- ~~Spider is now hunting Hoarding bugs~~ Disabled due to sync issues. Available as a toggle in config

## 0.1.4
- Fixed NullReferences, Functioning LOS check, added enemy list and base of assigning target (WIP)

## 0.1.3
- Attempt at custom behavior, fixed README/CHANGELOG, renamed namespaces ect.

## 0.1.2
- Reupload cause I forgot to edit CHANGELOG

## 0.1.1
- Updated description and added credits.

## 0.0.1
- Test upload.