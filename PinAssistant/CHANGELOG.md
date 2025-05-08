# Changelog

<details open>
<summary><b>
v1.8.0 Compatibility Update
</b></summary>

- Update
	- Now works for Valheim v0.220.
- Changes
	- JsonDotNet - Changed dependency from Tekla JsonDotNet -> ValheimModding JsonDotNet.
	- Track Object and Pin Object now defaults to LeftShift + T/P instead of Left Control, configurable in ConfigManager F1 or Valheim\Bepinex\config\com.WxAxW.PinAssistant.cfg. (Just realized how dumb it is for the character to always crouch when doing it)
	- Track Object UI no longer toggles when Filter UI is open. (Since the new default keybind is Shift + T, it will toggle the Track Object UI when typing texts in the filter ui.)
	- Dependency updates.
- Fixes
	- Fixed a bug where player movement is allowed when both Filter UI is focused and Track Object UI is open, then Track Object UI is closed.
- <details>
	<summary>
	Backend
	</summary>

	- Updated repo to latest JotunnModStub, haven't tested UnityMod project.
	- Not really sure how I fixed the new bug that was encountered on v0.220, but I guess updating my project with the latest JotunnModStub, fixes it? Possibly some compiler issue on my old project.
	- FilterPinsUI
		- Removed ModDisable/ModEnable method, consolidated to OnDisable and OnEnable. (Unsure what my train of thought was here.)
	- MinimapPatches - Decoupled from MinimapAssistant by adding event systems that MinimapAssistant will subscribe to.
	- MinimapAssistant
		- Subscribed to MinimapPatches events instead of having to patch the Minimap class with MinimapAssistant methods.
		- Renamed filter variables and methods to better match its purpose.
		- ModifyPins and SearchPins method now immediately executes FilterOutPins, due to Valheim's new update, the pins are not being filtered out immediately.
	- TrackObjectUI - Change toggle behavior by simply calling 'enabled' fixes looping issue.
	</details>

</details>

<details>
<summary><b>
v1.7.0 More Quality of Life Update
</b></summary>

- New
	- Added configuration for Redundancy distance for any pins. More info on config tip
	- Tracked Objects now automatically sorts by Pin name! (Don't ask me why I never implemented it, even I don't know)
	- Thumbnail change for the mod. (I finally had the motivation to design one)
- Changes
	- Similar pin Redundancy distance logic are now case insensitive. Used to be, comparing for nearby similar pins' based on names, is case sensitive.
	- Changed old versions' changelog's backend entry to its own backend entry.
- Fixes
	- Some warning logs are being logged as errors. (fixed "Minimap not found" error log, hooray)
	- Visual bug for tracked objects. Reloading Tracked Objects no longer show a blank dropdown selection.
- <details>
	<summary>
	Backend
	</summary>

	- PluginComponent
		- Changed from Component
	- TrackingAssistant
		- changed OnTrackedObjectSave to have LooseDicitonary parameter
	- TrackObjectUI
		- Tracked Object Dropdown/List now resets on every Add, Modify, or Remove actions, it's negligibly slower, but it greatly avoids logical errors by having a separate add modify remove logic in the UI.
	- TrackedObject
		- Added IComparable interface
	- LooseDictionary
		- Added IComparable interface
		- Added sort function, sorts dictionary based on TValue
	- Project
		- Used RegexOptions / StringComparison Ignore case, instead of making both strings into ToLower(), (never knew this existed, discovered out of curiosity if comparison methods had overloads, which it had)
	</details>

</details>

<details>
<summary><b>
v1.6.0 Bulk Modify Pins + QoL Update
</b></summary>

- New
	- ZOOOM
		- Normal zoom in minimap is not enough? now you can ZOOM MORE! Configurable in config manager. (applies to both small and large map)
	- Icon searching
		- You can now narrow down your searches more by setting an icon type instead of just names (you can pick all icons still).
	- Bulk pin modification (Took way too long to rewrite code to work with this .-.)
		- Ability to modify existing pins' name and icon when modifying Tracked Object entries.
		- Ability to modify existing pins' name and icon through the search window.
		- This will match Pin's Name and Icon and modify it to the currently modified entry.
- Changes
	- Pins are now colored based on their names and icons, instead of just names.
	- Searching is no longer case sensitive (since the pins' characters' cases can't be determined due to the font style).
	- Updated Showcase previews to match the new version, also made it viewable instead of clicking the link.
- Fixes
	- If you have a mod that can change a pin's position you'll be able to overlap a new pin if the positions are absolutely the same to the prior position.
	- When a filter is active, newly created pins (from double clicking in the map) will be invisible.
	- Changing an entry's Object ID will stop detecting objects with the currently editted matching id (oversight).
	- Blacklist Word not working properly all the time (happens when the blacklist word is before the id).
		- ex. `SunkenCrypt4(Clone)` | entry: objectID > Crypt, blacklist > Sunken
		- Looking at a structure named `SunkenCrypt4(Clone)` will still get detected because the 'Sunken' word is before the 'Crypt' word.

- <details>
	<summary>
	Backend
	</summary>

	- MinimapAssistant
		- Added PinName + Icon key storage to distinguish between different combinations.
		- Refactored to be more streamlined and to work with new version. Used TryGetValue and reusing other methods.
		- Moved edittting pin variables to MinimapPatches (so both MinimapAssistant and TrackingAssistant can make use of it)
		- Optimized filtering through pins greatly compared to before. I had no idea IEnumerable acted the way it is (deferred execution). Made it .ToList() and then updates the filter whenever a pin is modified or added.
		- Added compatibility for pin type searching instead of just names.
		- Renamed methods to better match the new code.
	- TrackObjectUI
		- Moved PopulateIcons, FormatSpriteName and m_dictionaryPinType(m_dictionaryPinIcons)variable to MinimapAssistant and added string name to dictionary as well. FilterPinsUI now uses this dictionary as well for overhauled UI.
	- MinimapPatches
		- Added isManualPin token to determine if the newly added pin is made by a player (to avoid it from being filtered out immediately while editting it)
	- Plugin
		- Slightly cleaned up code.
	- LooseDictionary
		- In ChangeKey() instead of using dictionary changekey extension, remove the original node and add the new cloned node instead (dictionary still contains the old node instead of being new if Changekey was used)
		- Added original key parameter to TraverseDetails and TryGetValueLooseLite to fix the new search method with node's blacklistword
	</details>

</details>

<details>
<summary><b>
v1.5.2 Mod Compatibility fixes + Search fixes
</b></summary>

- New
	- You can press enter when the search window field is selected to immediately submit (find) now.
	- Updated description / README.md
		- Compatibility section
		- Slightly fixed up known issues page.
- Fixes
	- Search Window
		- Typing in the search field, M (or the minimap key) closes the minimap
		- Toggling start up config when the mod is disabled won't reflect the changes.
		- The search window will always be opened or closed whenever you open the minimap depending on the "Show Search Window on startup" toggle.
	- Pinnacle
		- Pins not being colored permanently until relog when editting pin name and not clicking out (to deselect pin) after an edit or pressing enter, and then followed by editing the name once more.
	- Under the Radar
		- Pins not being filtered out permanently until mod re-enable, when pinning an object using PinAssistant that is temporarily pinned by Under The Radar and then unloading it by going away from it.
- <details>
	<summary>
	Backend
	</summary>

	- TrackingAssistant
		- Added a check on RemovePin to double check if the removed pin is actually in the dictionary.
	- MinimapAssistant
		- Updates old_PinName whenever OnPinUpdate gets called. (for when user changed the pin name once more. Doesn't apply to Vanilla)
	- OtherModPatches
		- Slightly refactored PinnaclePatches events to use MinimapPatches event
	- GUIExtension
		- Moved to GUIManagerExtensions
	</details>

</details>

<details>
<summary><b>
v1.5.1
</b></summary>

- New
	- Compatibility for Under The Radar
		- You can now add pins where temporary pins made by Under The Radar are located.
- Changes
	- Tracked objects searching
		- Changes id searching behaviour where if you loosely (not exact id match) search for an id, you can find something even though it may not make sense.
			- ex. entry = copper | search key = c_o_p_p_e_r | you'll still find copper.
			- ex. entry = copper, entry = runestone | search key = c_runestone_opper | you'll still find copper instead of runestone
			- this is to avoid similar ids to be detected, especially mushrooms magecap, jotun puffs, etc. can be found through Pickable_Mushroom(Clone), cause their difference is having a text between Mushroom and (Clone).
		- Refactored searching in the backend for a slightly more optimized way. Found out it's doing some meaningless searches.
		- Disabled being able to search crypts' (sunken and forest) interior structure with Crypts in their name (mudpile, torches, are not included, only walls, chests, loot, even doors are disabled.).
			- This is to avoid pinning unintended objects whenever having an entry for Crypt to only track the entrance location.
	- Capitalized previously lowercased Pin names' words when mod pre-fills a looked objects
- Fixes
	- Tracked objects searching
		- Fixed logical error for changing the object id (would've broke the data, but could be fixed through pressing the reload tracked objects key)
	- v1.4.0 Changelog message typo
		- "Pickable_Mushroom(Clone)" can be found with "Pickable_Mushroom_Magecap(Clone)"
		- should've been:
		- "Pickable_Mushroom_Magecap(Clone)" can be found with "Pickable_Mushroom(Clone)"
- <details>
	<summary>
	Backend
	</summary>
	
	- Plugin
		- Forgot to remove printing of layernames.
	- GUIManagerExtension
		- Moved ApplyToggleStyle from TrackObjectUI to GUIManagerExtension as FilterUI now needs it.
		- refactored some functions to remove unnecessary code and repurposed extension to work with some main methods.
	- LooseDictionary
		- Fixed missing code in method, ChangeKey. alternate dictionary deletes key, but does not add new key.
		- Refactored searching for keys, which avoids nonsense conditional checkings(kept on checking validity of the same node)
		- Created a new try get method to avoid unintended found results. (c_o_p_p_e_r finds copper key)
		- left old try get method alone but might remove it next time.
	- TrackingAssistant
		- OnPinAdd will now exclude special pins as well. I thought it was necessary to include them so pins won't overlap, but most special pins have different locations, so it would overlap regardless.
		- ModifyTrackedObject
			- If ChangeKey failed, will fail entirely. This would cause a bug where TrackedObject ID is changed but dictionary key is not change.
		- Slightly refactored FormatObjectNames method.
	</details>

</details>

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
		- "Pickable_Mushroom_Magecap(Clone)" can be found with "Pickable_Mushroom(Clone)"
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
- Fixes
	- Fixed UI bug due to latest Valheim update. (disappeared buttons and an error on main menu load)
	- Fixed a logical error existing since initial release. When modifying an object's ID (modifying a tracked object's ID to an existing ID it will work having 2 entries with identical IDs bugging out of the entry (the latest).
- <details>
	<summary>
	Backend
	</summary>
	
	- Updated dependency to latest Jotunn 2.14.3 and BepInEx 5.4.2200.
	</details>

</details>

<details>
<summary><b>
v1.2.1
</b></summary>

- Changes
	- Organized CHANGELOG.md.
- Fixes
	- Fixed unable to track, modify or untrack objects randomly occuring. Chances increases when you have too many tracked objects.
- <details>
	<summary>
	Backend
	</summary>
	
	- Similar to Plugin.cs and FilterPinsUI.cs, refactored TrackObjectUI.cs to use OnDisable when mod is turned off or UI is inactive to not process stuff on every frame.
	</details>

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

- <details>
	<summary>
	Backend
	</summary>

	- Plugin.cs 		
		- refactored to use MonoBehavior OnEnable/Disable (forgot this exists and can be used similarly to my situation).
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
- Fixes
	- Fixed build uploads to not contain versions 1.0.0 and 1.0.1 zips. (sorry for the extra file size).
- <details>
	<summary>
	Backend
	</summary>

	- Added Dictionary class version for whenever there's changes to how tracked objects are saved in future version.
	- Made UI elements public for modders to change its style (although you can probably do that through just Instance property alone).
	- Updated Jotunn library from 2.12.6 - 2.14.0 (didn't think about updating the template I used).
	- Cleaned up some codes.
	</details>
</details>

<details>
<summary><b>
v1.0.1
</b></summary>

- Changes
	- Changed the hover description for "Look Tick Rate" into a more detailed explanation, the prior message might confuse people.
	- Changed default Redundancy Distance from 30 to 20 (I found that it might be too big of a distance to check for redundancy).
	- Slightly organized README.md and added a suggestion section.
- Fixes
	- Fixed sub string searching in TrieNode when a prefix exists in the entry.
		- ex. Runestone ID and Copper ID. And your search is "Rock_Copper(Clone)" it only checked R's descendant but didn't check the rest of the letters so it never reached C of the 'Copper ID'.
</details>

**v1.0.0** Initial Release