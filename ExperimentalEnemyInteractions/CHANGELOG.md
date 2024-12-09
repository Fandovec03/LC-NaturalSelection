<h1>Natural selection</h1>

0.1.3 <br>
	- Implemented global enemy list.<br>
	- Fixed bees becoming invisible due to black magic (still dont know how).<br>
	- The mod no longer sets all colliders on enemies to be triggers.<br>
	- Hydrogires now move and consume dead corpses<br>
	- Many more config options<br>
0.1.2 <br>
	- Fixed logs<br>
	- Updated for NaturalSelectionLib 0.4.0<br>
0.1.1 <br>
	- Fixed NullReferenceException some logs<br>
0.1.0 <br>
	- Updated manifest, README and library integration<br>
0.0.2 <br>
	- Made less spammy logs <br>
0.0.1 <br>
	- Inital Upload

<h1>Experimental Enemy Interactions</h1>

0.2.6 - <br>
	- Finally discovered a fix for bees <br>
	- Hygrodiges move towards and consume corpses<br>
	--- Pathfinding broken (WIP)<br>
	- Added config options<br>
	--- Ignore immortal enemies<br>
	--- Set chances for bees to set giants on fire<br>
	--- Set chance for giant to extinguish themself<br>
0.2.5 - <br>
	- Merged bees fix and global enemy list branches <br>
0.2.4 -<br>
	- Added config for the bees fix <br>
0.2.3 - <br>
	- Experimental fix for bees <br>
0.2.2 - <br>
	- Implemented experimental global enemy list system. Every enemy now shares the same enemy list between eachother. Hopefully this will improve performance<br>
0.2.1 - <br>
	- Updated to latest branch<br>
	- Implemented experimental global enemyList<br>
0.2.0 - <br>
	- Updated to the Stable release!<br>
0.1.28 - <br>
	- Finally fixed the root issue. Enemies (Mainly bees) no longer target themselves or their own kind anymore.<br>
0.1.27 - <br>
	- This time for real fixed bees targeting themself.<br>
0.1.26 - <br>
	- Fixed audio issues with sandworm<br>
	- Sandworms revised. They're now using a proper behavior state (Due to technical difficulties they use custom state instead of vanilla when targetting enemies)<br>
	- Reenabled Sandworm patches (Make sure you have Load leviathan patches in Initialization settings checked)<br>
	- Fixed bees and other entities targetting themselves (unconfirmed. Credit to EnzoTheProtogen for reporting the issue)<br>
	- Bee now ignore Locusts and Sandworms (untested)<br>
	- Log improvements<br>
	- Added new config settings<br>
0.1.25 - <br>
	- Added more config options<br>
	- ~~Rewamped sandworm to use proper behavior states~~<br>
	- Sandworm temporarily disabled due to audio issues (can be manually enabled)<br>
0.1.24 - <br>
	- Added log option to include/exclude spammy logs<br>
	- Sandworms updated:<br>
		- Addressed a bug with Worms trying to move with disabled agent<br>
		- Added more arguments preventing skipping of vanilla code while emerging/emerged<br>
		- Worms no longer target dead enemies that were previously targetted before death<br>
		- Observed some weird behavior but works for now. Will fix later.<br>
	- Fixed ArgumentOutOfRangeException where enemyList tried to check LOS to previously removed enemy.<br>
0.1.23 - <br>
	- Giants have a chance to extinguish themselves, though they will be severely weakened by the fire<br>
	- Few improvements to bees<br>
		- Bees no longer target dead enemies<br>
		- DoAIInterval is not skipped on behavior state 0<br>
		- fixed an exception regarding dictionary count<br>
	- Changed some logs<br>
0.1.22 - <br>
	- Fixed bees pathfinding being broken in state 2<br>
	- The fire on forest giant no longer extinguishes itself the moment giant dies<br>
	- Added special interaction between Red bees and Forest giant<br>
0.1.21 - <br>
	- Fixed bees throwing LOS exceptions when no enemy is in LOS<br>
	- Fixed typo disabling vanilla code to be disabled and bees (the particles) not change the state<br>
	- Temporarily disabled custom state 2 on bees due to bees breaking. Bees will revert to vanilla state 2<br>
0.1.20 - <br>
	- Fixed typo causing Sandworms to throw keynotfoundExceptions<br>
	- Fixed changelog format<br>
0.1.19 - <br>
	- Forgot to upload updated DLL. All changes bellow apply with this version<br>
0.1.18 - <br>
	- Fixed an issue where the mob tries to add itself again into dictionary causing error (This fixes the error when paired with lethalmon)<br>
	- Fixed the script trying to apply patch via original Spider class instead of the patched one<br>
0.1.17 - <br>
	- Fixed safe mode accidentally preventing load of The bee patch<br>
0.1.16 - <br>
	- Potencially fixed same exception on blobAI
0.1.15 - <br>
	- Emergency fix: Fixed Key not foudn exception causing bees to not work<br>
0.1.14 - <br>
	- Added many more config options
		- **Safe mode:** On by default. Prevents unfinished and experimental scripts from loading.<br>
	- **Bees added!** Bees now target every enemy in their LOS.<br>
	- Several bugs and oversights fixed<br>
0.1.13 - <br>
	- Removed code breaking spider with the position fix
0.1.12 - <br>
	- Fixed sandworms affecting eachother for good! [Credit to Hamunii]
	- Some QOL changes for development
0.1.11 - <br>
	- Hygrodere now does not anger Hoarding Bugs
	- Hygrodere should anger Bracken less
	- **Added a dependency to fix spider getting stuck and other position issues**
	- Converted most debug logs to DebugLog
	- Fixed Hygrodere hitting enemies with custom hit trigger stupidly fast<br>
0.1.10 - <br>
	- Rewritten Earth leviathan patch code. Now sandworms behave as intended.<br>
	- ***known bugs: Audio not playing when chasing targets. Audio cutting off when chasign player.***<br>
0.1.9 - <br>
	- Fixed critical error causing hard crash on load caused by leftover Lobby Compatibility code<br>
0.1.8 - <br>
	- Regenerated DLL - forgot to regenerate after changing versions<br>
0.1.7 - <br>
	- Moved functions and methods of enemyList to EnemyAI. This makes development much faster and gives a potencial for a memory as a side effect, though that has to be implemented first in each enemyAI type.<br>
	- Resolved NullException error messages in the Collision patch<br>
	- **Earth leviathan** is now implemented. Leviathan now targets and consumes other surface creatures.<br>
0.1.6 - <br>
	- fixed typos and formatting in CHANGELOG and README<br>
0.1.5 - <br>
	- Fixed collisions not working<br>
	- Much less logs spam<br>
	- **Hygrodere now eats almost everything alive!**<br>
	- Spider deals 1 damage when Enemy has 2 or less health<br>
	- Added config file<br>
	- ~~Spider is now hunting Hoarding bugs~~ Disabled due to sync issues. Available as a toggle in config<br>
0.1.4 - <br>
	- Fixed NullReferences, Functioning LOS check, added enemy list and base of assigning target (WIP)<br>
0.1.3 - <br>
	- Attempt at custom behavior, fixed README/CHANGELOG, renamed namespaces ect.<br>
0.1.2 - <br>
	- Reupload cause I forgot to edit CHANGELOG<br>
0.1.1 - <br>
	- Updated description and added credits.<br>
0.0.1 - <br>
	- Test upload.<br>