# Possessed Mask

A ghost haunts your masks, the mask will try to get in your active slot and possess you.
Tweaks spawn chances of masks to allow them to spawn on all moons.

# Releases

# Version 2.0.4
- Fixed a bug where inventory would freeze on dropping an item sometimes for some reason.
- Added config option to change the value of masks without the spawn chance. (EnableChangeMaskSpawnChance = 3)

# Version 2.0.3
- Fixed config sync not working correctly, this should fix the issue where you could drop the mask regardless of how many items you had in your inventory.

# Version 2.0.2
- Fixed a leftover bug that was created by the last updates' fix

# Version 2.0.1
- Fixed bug where if the masks item wasn't in a level but the config setting `EnableChangeMaskSpawnChance` wasn't on 2 it would cause an error

# Version 2.0.0
- Rewritten the mod from the ground up with networking to hopefully prevent most desyncs.

# Version 1.1.2

- Fixed deactivation of selected item when switching slots.
- Fixed bug causing stun grenades to activate when switching slots.

# Version 1.1.1

- Fixed item name bug causing the mask value and spawn chance not to be changed.

# Version 1.1.0

- Hopefully fixed a bug where if a masked enemy spawned on a planet that is disallowed in vanilla it would just get stuck.
- Added more config options for controlling mod mechanics and spawn rate of masks.

# Version 1.0.10

- Added delay between preventing client actions and automated actions to prevent errors in edge cases.
- Changed some patches to hopefully prevent client-host desyncs.

# Version 1.0.9

- Fixed bug that caused players to not be able to interact with objects after dying from the mask.

# Version 1.0.8

- Fixed bug that caused dropped 2 handed items to disappear on local client unless he is the host. (Thanks Steven4547466)

# Version 1.0.7

- Stopped mask stuff while at the company building.
- Prevents some more player actions during possession to lower the chance of animation breaking.
- Modified the function for switching inventory slot for better compatibility with other mods.

# Version 1.0.6

- Fixed network related issues, animations not working correctly and clients not transforming into masked.

# Version 1.0.5

- Each day reset scaling variables back to config values.

# Version 1.0.4

- More error catching and validation.

# Version 1.0.3

- Fixed bug where activatable items didn't turn off when switched from.

# Version 1.0.2

- Added option to disable dropping the mask until inventory is full enough (has config option)
- Changed the way the mask price is affected by value multiplier < 1 (more value for masks)
- More config options.
- More bug fixes (YAY!)

# Version 1.0.1

- Fixed bug where 2 handed items were being switched away from.
- Introduced config.

# Version 1.0.0

- Initial release, somewhat broken and has several bugs