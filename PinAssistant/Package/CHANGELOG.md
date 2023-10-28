# Changelog

<details>
<summary><b>
v1.5.0 Better Search Update
</b></summary>

- New
	- Search Window Updates
		- RegEx searching of pin names.
		- Whitelist or Blacklist mode if you want to hide everything or show everything but the query you've inputted.
- Changes
	- Thunderstore did not support underline for some reason while bold, so I had to remove it.
	- Changed CHANGELOG.md `Backend` header to not be in bold.
	- Removed issue entry in "Known Issue" section about struck boulders being invalid, thought it wasn't necessary.
- <details>
	<summary>
	Backend
	</summary>
	
	- Plugin
		- Forgot to remove printing of layernames.
	- GUIManagerExtension
		- Moved ApplyToggleStyle from TrackObjectUI to GUIManagerExtension as FilterUI now needs it.
		- refactored some functions to remove unnecessary code and repurposed extension to work with some main methods.
	</details>

</details>

<details>
<summary><b>
v1.4.0 TRACK ALL THE THINGS
</b></summary>

- New
	- Almost everything can now be trackable. 
		- (Couldn't do 'all' cause it will mess up detections of other objects.)
		- Do not attempt to track a boulder that has been struck with a pickaxe, it will not identify it correctly. Track an unstruck one instead.
			- Will notify if it's invalid or not.
- Removed
	- Tracking Type options.
		- Refactored to be extremely less performance impacting compared to the original one (more details in backend).
		- Now almost everything is pinnable.
		- Might have some flaws but I checked and objects of interest should work correctly, did not test on uncommon or unnecessary objects (like a wood pole or something).
- Fixes
	- Fixed when opening and closing the color wheel while the "Exact ID Match" is toggled on, ObjectID will stay as uninteractable instead of being interactable.
	- Fixed error spam when a raid event ended.
	- Fixed a logical error where even if an id is set to exact match only, it can still be found with an almost similar id.
		- "Pickable_Mushroom(Clone)" can be found with "Pickable_Mushroom_Magecap(Clone)"
- <details>
	<summary>
	Backend
	</summary>
	
	- TrackingAssistant
		- Refactored LookAt to not use GetComponentInParent, but instead get root parent and retrieve name.
			- This will significantly increase performance as it wouldn't continuously call "GetComponentInParent" multiple times for each type, every x second per tick
			- Will show invalid target if it's a boulder struck with a pickaxe.
	- MinimapPatches
		- Refactored patching exclusion of special pins from MinimapAssistant to clean up Transpilers (This way is just so much better, I don't know why I didn't thought of this).
	- CHANGELOG.md
		- everything was bold, fixed that now, (was hard to see in visual studio preview, only noticed after the last update where I showed the changelog on thunderstore).
	- Used CodeMaid to clean up entire project.
	- Updated harmony package of project.
	</details>

</details>

<details>
<summary><b>
v1.3.1 Hotfix
</b></summary>

- Changes
	- CHANGELOG.md
		- added in package, so that thunderstore can detect and add it and that users don't have to click here at the description to go to the changelog at github.
		- reversed version order so the latest version is always at the top.
- Fixes
	- Colored shared pins will now fade properly when switching on or off the shared pins.
	- Fixed issues with pings constantly sending an error that it already exists which led to pins freezing as the game thinks the pings aren't being added properly.
		- Overlooked a check which I accidentally removed during clean up of my code as I thought it was unnecessary.
- <details>
	<summary>
	Backend
	</summary>
	
	- TrackingAssistant
		- Removed indent formatting for saving tracked object data.
	- publish.ps1
		- added changelog.md to compressed archive.
	- README.md
		- changed a bit of words.
		- added proper installation manual.
		- added tutorial for colored pins.
		- forgot to add colored pins section.
	</details>

</details>

<details>
<summary><b>
v1.3.0 Colored Pins Update!
</b></summary>

- New
	- Colored Pins! 
		- New option over at the Track Object UI right beside the pin icon. 
		- You can also change its transparency. 
		- Sadly, due to limitations, the pins are colored based on their pin names.
	

- Changed
	- Changed UI Panels to actually fit with the Game's UI's dynamic panel colors that changes depending on the environment.

- Fixes
	- Fixed bug when modifying current tracked object with an existing ID the currently editting object is deleted instead of just sending an error.
- <details>
	<summary>
	Backend
	</summary>
	
	- A lot of backend changes as I've learned to do stuff differently and so it can be update friendly.
	- Added compatibility for Pinnacle's edit feature with colored pins when editting the name.
	- Plugin
		- Created initialization order convention to better manage enabling or disabling and disposing plugin and maybe to have some use for it in the future.
		- In the past I tried to decouple my classes as much as possible but all it led to was somewhat messy coding in Plugin.cs. I figured that I shouldn't just let Plugin.cs be dependent on ModConfig instance so I moved a lot of things away from it to their respective relating classes. 
			- Transferred saving system call to Tracking Assistant instead of having to listen to an event by TrackingAssistant from Plugin.
			- Transferred TrackingAssistant initialization parameters from Plugin to just be managed by TrackingAssistant itself.
			- Transferred config change events to their respective classes like, is filter window open on startup, and type tracking enabled change.
	- TrackingAssistant (PinAssistantScript)
		- Changed to TrackingAssistant
		- Changed Serialization and Deserialization handling.
		- Changed the way modify implies, instead of changing the values of the class, completely replace it with a new class to work well with the new colored pin codes.
	- TrackObjectUI
		- Moved modify logic to TrackingAssistant and just read return value to determine what messages to show.
		- Changed the way modify implies (see tracking assistant)
	- TrackedObject
		- Added helper methods to retrieve pin type by int
	- FilterPinsUI
		- Exposed UI Members
		- Moved Filter logic to MinimapAssistant
	- LooseDictionary
		- Refactored Traverse method to not be in TrieNode but in the LD class.
		- Added Change key method to help with the new colored pin feature
	- GUIMangerExtension (TMPGUIManager)
		- used extension (just learned of this) instead of creating an entirely new class with almost the same codes
		- fixed a situation where the extension will keep on initializing everytime you load the main menu.
	- Mod Config
		- Followed initialization convention.
	- MinimapPatches
		- Changed events from delegates to Action
	- Unity
		- Used Assembly Definitions so that I don't have to replace a new version of the assembly everytime the ui variables changes
	- May have missed some other refactorings and missed on potential refactoring as I've done way too much to remember all of them and I didn't document the changes until the last few days >.>
	</details>
</details>

<details>
<summary><b>
v1.2.2 Valheim v0.217.22 Compatibility Update
</b></summary>

- Changes
	- Slightly changed tracking UI.
	- <details>
		<summary>
		Backend
		</summary>

		- Updated dependency to latest Jotunn 2.14.3 and BepInEx 5.4.2200.
		</details>
- Fixes
	- Fixed UI bug due to latest Valheim update. (disappeared buttons and an error on main menu load)
	- Fixed a logical error existing since initial release. When modifying an object's ID (modifying a tracked object's ID to an existing ID it will work having 2 entries with identical IDs bugging out of the entry (the latest).

</details>

<details>
<summary><b>
v1.2.1
</b></summary>

- Changes
	- Organized CHANGELOG.md.
	- <details>
		<summary>
		Backend
		</summary>

		- Similar to Plugin.cs and FilterPinsUI.cs, refactored TrackObjectUI.cs to use OnDisable when mod is turned off or UI is inactive to not process stuff on every frame.
		</details>
- Fixes
	- Fixed unable to track, modify or untrack objects randomly occuring. Chances increases when you have too many tracked objects.

</details>

<details>
<summary><b>
v1.2.0 Search Update
</b></summary>

- New
	- Added the ability to search Pins on the map for situations when your map is too crowded with Pins.
		- Press Tab while the map is open to show/hide the window.
		- Enclose the search keyword with `"` to search pins with the exact name. ex. `"Mushroom"`.
		- You can also change its visibility on world startup/mod enabled through the config.
		- If you have Pinnacle and want both of them to show/hide together, just disable `Show Search Window on startup` and toggle off and on `Enabled Mod`.

- Changes
	- <details>
		<summary>
		Backend
		</summary>

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
v1.1.0 Trackable Types Update
</b></summary>

- New
	- Option to choose what types of objects you'd like to look for to increase performance (albeit negligible).
		- Hover each type in the config manager to figure out which do you want to be detectable.
- Changes
	- Separated changelog to CHANGELOG.md.
	- <details>
		<summary>
		Backend
		</summary>

		- Added Dictionary class version for whenever there's changes to how tracked objects are saved in future version.
		- Made UI elements public for modders to change its style (although you can probably do that through just Instance property alone).
		- Updated Jotunn library from 2.12.6 - 2.14.0 (didn't think about updating the template I used).
		- Cleaned up some codes.
		</details>
- Fixes
	- Fixed build uploads to not contain versions 1.0.0 and 1.0.1 zips. (sorry for the extra file size).
</details>

<details>
<summary><b>
v1.0.1
</b></summary>

- Changes	- Changed the hover description for "Look Tick Rate" into a more detailed explanation, the prior message might confuse people.	- Changed default Redundancy Distance from 30 to 20 (I found that it might be too big of a distance to check for redundancy).
	- Slightly organized README.md and added a suggestion section.
- Fixes
	- Fixed sub string searching in TrieNode when a prefix exists in the entry.
		- ex. Runestone ID and Copper ID. And your search is "Rock_Copper(Clone)" it only checked R's descendant but didn't check the rest of the letters so it never reached C of the 'Copper ID'.
</details>

**v1.0.0** Initial Release