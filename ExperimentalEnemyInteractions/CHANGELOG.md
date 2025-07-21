# Experimental Enemy Interactions

## 0.5.29

### Library
- Privatized globalEnemyLists
- added methods for managing globalEnemyLists

## 0.5.28

- Removed unused code
- Added enumerator into LibraryCalls class
- Updated README

### Library
- Rewrote PathfindingLib implementation (Thanks Zaggy!)

## 0.5.27
- Removed junk code
- Enabled Sandworms targetting immortal enemies

### Library
- Rewrote code to use API instead of Jobs

## 0.5.26
- Added toggle for PathfindingLib coroutines
    - While on and PathfindingLib present, PathfindingLib will be used in coroutine
- Added toggle to use Pathfinding to find closest enemy

## 0.5.25
- Added toggle for Library calls

### Library
- Put loggers behind toggles

## 0.5.24

- Mixed generating bad data container ids resulting in enemies sharing single data container
- Moved data container base and data container methods into utillities class
- Enemies when destroyed will destroy their data container aswell

### Library
- FindClosestEnemyCoroutine will work without Pathfindinglib
- Added checks to prevent enumerator errors
- Minor internal changes

### Known issues
- Mass killing baboon hawks by earth leviathan often results in error spam.


## 0.5.23

### Data structures
- Reworked dictionary to use objects of any type as a key
	- Fixes issues getting data structures by spider webs and EnemyAIPatch
	- Merged data structure dictionaries into single dictionary

### Added experimental library and toggle
- Enemies will use a coroutine for finding closest enemy instead of a method
	- May result in better performance but that was not tested

## 0.5.22
- Updated __Blacklists__.
    - **WARNING:** Blacklists will be reset. Check your config for orphaned entries after loading into game once.

## 0.5.21
- Updated ReXuvination compatibility
- Updated logs

### Internal
- Replaced majority of loggers with method
- Rewamped enemy data system

### Library
- Updated DebugStringHead. Now it accepts any type

## 0.5.20
- Redone fix for blob opening doors.

## 0.5.19
- Redone blob door fix.

## 0.5.18
- Optimized and Delobotomized Earth Leviathan

## 0.5.17
- Updated to NaturalSelection 0.4.4

## 0.5.16
- Updated to Natural Selection 0.4.3

## 0.5.15
- Updated to Natural Selection 0.4.2

## 0.5.14
- Fixed collided with corpse spam

### Library
- Updated GetEnemiesInLOS to use OverlapSphere for better optimization

## 0.5.13

- Updated spider behavior
    - Spider ignores Snare fleas on the ceiling
    - Spider won't get alerted by immortal enemies that trigger spider webs

- Implemented compatibility for Sellbodies/Enhanced Monsters
    - Blob now moves towards and consumes enemy bodies from these mods

- Pre-release

## 0.5.12
- Fixed Log spam from Circuit bees

## 0.5.11

- Added compatibility toggles to use only config toggles to load compatibilities
- Spider web no longer gets triggered on dead enemies
- Fixed spider web not stopping the audio
- Spider webs reduce enemy velocity when over target speed
- Updated initialial custom size list generation

## 0.5.10
- Fixed unfinished compatibilities turning on with stable mode on
- Added logs for loading compatibilities

## 0.5.9
- Fixed spider web NRE spam when enemy didn't have animator
- Sandworm now ignores enemies by enemy sizes
	- Done with custom enemy size enum. Vanilla is too limiting
	- New config for enemy sizes ranging from 1 - 5 [Tiny - Giant] and 0 [Undefined]
- Merged Sellbodiesfixed and Enhanced Monsters compatibility into one script
	- still in WIP
- Improved Curcuit Bees
	- Removed old and redundant code
	- Bees now actually prioritize enemies holding its nest
	- More consistent behavior

## 0.5.8
- Added a check for retrieving enemy data
- Slightly updated library
- Added setting for global enemy lists update interval
- Removed localEnemyLists from patches. Enemies use local/temporary lists instead.
- Updated rexuvination compatibility a bit.

## 0.5.7
- Added logs when data containers are created.
- Updated loggers
- Updated scheduling global lists to pass parameters by reference

## 0.5.6
- ~~Updated immortal enemies check to include enemies with set canDie bool to false~~ Reverted

### Config
- Updated descriptions to be more clear
- Orphaned entries will be printed out in console before clearing
- Updated debug bools update event
- Updated Credits

### Fixes
- Fixed bees and blob blacklists not adding entries into internal blacklists


## 0.5.5
- Updated immortal enemies check to include enemies with set canDie bool to false
- Enemies no longer collide with blacklisted enemies (with exception of Sandworm)
- Updated description and README 

### Config
- **Debug bools now update whenever value is changed ingame**
    - Csync not required!
- Updated entries to use AcceptableRange

## 0.5.4

- Updated Config 
- Updated Logs.
    - Some logs were put behind debugTriggerFlag bool.

### Bug fixes
- Fixed enemies not working
- multiple bug fixes for blacklists

### Library
- Updated to work with current build
- Added short form for DebugStringHead

## 0.5.3

- Updated README
    - Added link to stable version
- Added blacklist for bunker spider

### Bug fixes

- Fixed passing blacklist entries to library in unreadable format
- Fixed variables getting blacklist entries from incorrect source
- Few more bugs related to blacklists

### Internal

- Moved Calls to the library to its own class
- Updated config generation
    - Sorted code into functions
    - Enemies are put into secondary list on failing to get their name for later 2nd attempt

### Known issues

- Spider web doesn't visually stick onto enemies

## 0.5.2

- Blacklists now generate automatically after booting up

### Internal changes
- Renamed speedModifierBlacklist to spiderWebBlacklist

### Fixes
- Fixed configs not generating with Code Rebirth

## 0.5.1
- **Updated CHANGELOG Formatting**

#### Internal changes
- Updated strings to use interpolation
- Updated sandworm's networking. It's network connections and variables will now be included for disposal
- Removed game's code from the mod and replaced.
 
#### Experimental
- Added experimental hit registrer

#### Fixes
- Fixed edge case where missing enemy name results in solf lock the game while booting up.

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