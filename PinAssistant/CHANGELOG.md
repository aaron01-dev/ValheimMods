# Changelog

**v1.0.0** Initial Release

<details>
<summary><b>
v1.0.1
<b></summary>

- Changes	- Changed the hover description for "Look Tick Rate" into a more detailed explanation, the prior message might confuse people.	- Changed default Redundancy Distance from 30 to 20 (I found that it might be too big of a distance to check for redundancy).
	- Slightly organized README.md and added a suggestion section.
- Fixes
	- Fixed sub string searching in TrieNode when a prefix exists in the entry.
		- ex. Runestone ID and Copper ID. And your search is "Rock_Copper(Clone)" it only checked R's descendant but didn't check the rest of the letters so it never reached C of the 'Copper ID'.
</details>

<details>
<summary><b>
v1.1.0 Trackable Types
<b></summary>

- New
	- Option to choose what types of objects you'd like to look for to increase performance (albeit negligible).
		- Hover each type in the config manager to figure out which do you want to be detectable.
- Changes
	- Separated changelog to CHANGELOG.md.
	- <details open>
		<summary><b>
		Backend
		<b></summary>

		- Added Dictionary class version for whenever there's changes to how tracked objects are saved in future version.
		- Made UI elements public for modders to change its style (although you can probably do that through just Instance property alone).
		- Updated Jotunn library from 2.12.6 - 2.14.0 (didn't think about updating the template I used).
		- Cleaned up some codes.
		</details>
- Fixes
	- Fixed build uploads to not contain versions 1.0.0 and 1.0.1 zips. (sorry for the extra file size).
</details>

<details>
<summary><b>v1.2.0 Search Update<b></summary>

- New
	- Added the ability to search Pins on the map for situations when your map is too crowded with Pins.
		- Press Tab while the map is open to show/hide the window.
		- Enclose the search keyword with `"` to search pins with the exact name. ex. `"Mushroom"`.
		- You can also change its visibility on world startup/mod enabled through the config.
		- If you have Pinnacle and want both of them to show/hide together, just disable `Show Search Window on startup` and toggle off and on `Enabled Mod`.

- Changes
	- <details>
		<summary><b>
		Backend
		<b></summary>

		- Plugin.cs 					- refactored to use MonoBehavior OnEnable/Disable (forgot this exists and can be used similarly to my situation).
			- added unsubscription to some missed events on OnDestroy (not really important since plugins don't get destroyed).
		- PinAssistantScripts.cs
			- refactored to not initialize on Instance reference, but instead only create a new instance on Init() (to follow init convention on other classes).
		- MinimapPatches.cs
			- refactored to contain patches in one class only instead of many classes (didn't know you can do it this way.
		- Changed README.md to include new search feature.
	</details>
</details>

<details>
<summary><b>
v1.2.1
<b></summary>

- Changes
	- Organized CHANGELOG.md.
	- <details>
		<summary><b>
		Backend
		<b></summary>

		- Similar to Plugin.cs and FilterPinsUI.cs, refactored TrackObjectUI.cs to use OnDisable when mod is turned off or UI is inactive to not process stuff on every frame.
		</details>
- Fixes
	- Fixed unable to track, modify or untrack objects randomly occuring. Chances increases when you have too many tracked objects.

</details>

<details>
<summary><b>
v1.2.2 Valheim v0.217.22 Compatibility Update
<b></summary>

- Changes
	- Slightly changed tracking UI.
	- <details>
		<summary><b>
		Backend
		<b></summary>

		- Updated dependency to latest Jotunn 2.14.3 and BepInEx 5.4.2200.
		</details>
- Fixes
	- Fixed UI bug due to latest Valheim update. (disappeared buttons and an error on main menu load)
	- Fixed a logical error existing since initial release. When modifying an object's ID (modifying a tracked object's ID to an existing ID it will work having 2 entries with identical IDs bugging out of the entry (the latest).

</details>