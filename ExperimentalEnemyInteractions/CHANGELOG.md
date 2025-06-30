# _Natural selection_

# 0.4.5
- Optimized and Delobotomized Earth Leviathan

# 0.4.4
- Fixed ReXuvination patch not working sometimes. Now patch prevents ReXuvination from patching enemy colliders.

# 0.4.3
- Fixed some supported mods not registering when not specified with BepInDependency attribute

# 0.4.2
- Updated config. Spider toggle is on by default instead of off.

# 0.4.1
- Fixed compatibility patches not loading automatically with the toggle on
- Added compatibility for LobbyCompatibility
# 0.4.0

### __Bunker spider released__
- Spider webs stick to enemies and alert spider
    - Investigates webs and chases prey. Returns back when the prey gets too far from the nest.

### Added compatibility for SellbodiesFixed, Enhanced Monsters and ReXuvination
- Beware of slimes consuming your profits away!

## Major Changes

- Added checks for enemy data structures
- Added custom enemy sizes
    - Customizable in config file

### Circuit Bees
- Circuit bees properly prioritize enemies holding its nest

### Sandworms
- Sandworms ignores enemies by custom enemy size

### Optimizations, Fixes and QoL
- Optimized networking with generics
- Added setting update interval of global enemy lists
- Updated library to newest version
    - More optimized LOS check
    - Deprecated usage of types
- Fixed spider webs throwing NREs when enemy is missing animator
- Fixed spider webs playing audio when it shouldn't
- Fixed spider webs getting triggered by dead enemies
- Updated logs

## IMPORTANT
- Regenerate and check your config logs

# 0.3.0

## Major changes

### Added support for spider webs
- Spider webs slow down and stick to enemies passing through them

### **Configuration file rewamped**
- Updated Layout and descriptions
- Added more options and variables
- Added **automatically generating entries for blacklists and similar lists**
	- Generates on booting up the game or 2nd time on loading into lobby if some entries fail to generate
- Debug toggles **update automatically if they're changed on runtime** with LethalConfig/LethalSettings

### Misc
- Updated description, readme, changelog and links
- Updated logs
	- More logs are toggled with trigger flags bool

## Bug fixes
- Fixed sandworm's networking connections and variables not being disposed of
- Fixed ememies colliding with blacklisted enemies

# 0.2.6
- Fixed bees throwing error when every enemy in LOS is killed
- Upgraded collision system. Bees and hygrodere now hit multiplle enemies they collide with
- Added config to log the library
- updated README

# 0.2.5
- Reupload because I forgot to upload a file. Woops
- Updated README

# 0.2.4
- Updated README
- Updated mod for newest Library version
	- Finally fixed invisible bees!
	- Enemies can now target enemies outside of their usual domain! Inside enemies can target outside enemies when outsise and vise versa
- Fixed bees not returning to beehives after chasing/killing enemy
- Global enemy lists get cleared at the end of rounds or when client disconnects

# 0.2.3 
- Fixed and simplified config enabling enemy changes
- Updated README
# 0.2.2 
- Fixed shit Networking implementation causing massive lag (Credit to Robyn and Xilo on Discord for pointing it out!)
- Updated logs
- edited README
# 0.2.1 
- Fixed giants always set on fire
- Fixed Error spam on clients
- Small changes to networking which fixes lag
# 0.2.0 
- **Added networking** 
	- Fixes desync and network related issues, mainly enemy sounds not playing on non-host clients
- Fixed sandworm eating non-host players inside the ship with the toggle on
- Fixed giants not using the extinguish chance from config
- Added __blacklists__ to config
- Optimization on clients/server
# 0.1.5 
- Fixed collisions returning NullReferenceException errors
- Added config for sandworms to not eat players inside leaving ship
- Updated logs and configs
# 0.1.4 
- Fixed logs spamming you're on experimental version
- Added option to override vanilla sandworm collisions to possibly fix the lag when colliding with multiple enemies at once
# 0.1.3 
- Implemented global enemy list.
- Fixed bees becoming invisible due to black magic (still dont know how).
- The mod no longer sets all colliders on enemies to be triggers.
- Hydrogires now move and consume dead corpses
- Many more config options
# 0.1.2 
- Fixed logs
- Updated for NaturalSelectionLib 0.4.0
# 0.1.1 
- Fixed NullReferenceException some logs
# 0.1.0 
- Updated manifest, README and library integration
# 0.0.2 
- Made less spammy logs 
# 0.0.1 
- Inital Upload