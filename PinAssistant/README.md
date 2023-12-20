# If you like Pin Assistant, feel free to support me!
Here's my [Ko-fi](https://ko-fi.com/WxAaRoNxW) page!
and you can [PayPal.me](https://paypal.me/aaronagy?country.x=PH&locale.x=en_US) as well!

I figured I could add a donation page here, but remember, you are not obligated to donate.
I don't know much about foreign wallets, I only know PayPal and ko-fi, let me know if you have other suggestions!

# Preview
[Preview alt](https://i.imgur.com/ntUecfL.mp4)

![Preview gif](https://i.imgur.com/ntUecfL.gif)

# Mod Introduction

Tired of having to manually pin objects and would rather just want to solve it with a click of a button? Maybe you'd rather automaticaly pin the objects you look at. You found an object of interest and you plan to retrieve it later, but got lazy pinning them and lost them? Need a Dandelion or Thistle for your brews but became unfindable cause you're now searching for it? Perhaps you forgot where you mined that chunk of copper and you can't find it anymore cause it blends in with the environment.

Pin Assistant is the game-changing mod you've been waiting for. It's the ultimate solution for all adventurers, explorers, crafters, gatherers, and farmers who want to stay organized without the hassle of manual object pinning.

Ensuring you stay focused on what matters most to you. Whether it's rare herbs for your magical brews or valuable resources for crafting, your Pin Assistant will do the tracking for you.

## User Friendly Design
Tailored for both casual and technical players who wants to modify their pins to exactly do what you want. Not sure what to do? just look at an object press Ctrl + T and it will automatically fill the values for you, then click Track.

## Customizable Pinning
Use your Pin Assistant for your specific needs. 
Customize the entry to how you want the pin to appear, a boss icon? a fire? is the pin crossed off? or maybe even its colors! To easily determine which is which so you won't have a hard time figuring out what pin are you exactly looking at.

## Customizable Behavior
Customize your entries to choose what it wants. Only search a specific object, pin multiple objects on just one entry, delete on pins associated with entry on log out, and many more!
Many configuration combinations for you to think about!

## Lightweight Plugin
Efficient auto pin and tracking system
Even though you might think that this is a pretty heavy system with the complex logic it has. It wouldn't, except for Valheim's pin system, that's not on me anymore. Although, the color system's performance is based on how many colored pins you have, but it's really negligible (I did my best to optimize it a lot). If you worry about performance you can increase the tick rate in the settings, but its performance impact is also negligible.

Technical stuff: Most of the system uses dictionaries especially the tracked entry system which uses TrieNode Dictionary, which means it won't harm the performance when the mod has an object that needs to be ran through the list of entries to check, (imagine those auto complete systems in searching on your computer)

## Features

### GUI
A Valheim style UI for you to easily manage your tracked objects

[GUI alt](https://i.imgur.com/tNbOz5V.mp4)

![GUI](https://i.imgur.com/tNbOz5V.gif)

### Auto Pinning
Too lazy to create pins by yourself? No problem! Just create an entry for an object and this mod will do everything for you.

### Manual Mode/Quick Pin
Don't like the auto pin feature? That's ok! You can disable it and use the 'Pin Object' keybind. Although, be sure to track the object first before you can use the pin key!

### ID Filter
You can also set the entry to only find objects that are named exactly to what you've set, this means you'll be able to choose which specific type of object variation you want to be pinned, Crypt1? Crypt2? or if you want all the variations, just set the ID to Crypt. 

### Blacklisting
However this means Sunken Crypts would also be included in the detection, that's where the Blacklist feature is needed. Add 'sunken' to the entry with crypt ID, and this would only allow non-Sunken Crypts to be pinned on that entry.

[Blacklisting Sunken Crypts from Tracked Crypt Entry alt](https://i.imgur.com/ODqeFo3.mp4)

![Blacklisting Sunken Crypts from Tracked Crypt Entry](https://i.imgur.com/ODqeFo3.gif)

### Redundancy Feature
If you think your pin might clutter cause the objects are grouped together, you can change the redundancy setting to avoid pinning similar objects at a certain distance based on the pinned object.

[Redundancy Distance Showcase alt](https://imgur.com/lerLdHw.mp4)

![Redundancy Distance Showcase](https://imgur.com/lerLdHw.gif)

### Colored Pins
Hard to read the many pins you see? With this mod's tracking system you can change the pins you have into different colors! Due to limitations the pins colors are based on their Pin Names

[Tracking/Coloring Dandelions alt](https://i.imgur.com/WZ3ovBV.mp4)

![Tracking/Coloring Dandelions](https://i.imgur.com/WZ3ovBV.gif)

### Searchable Pins
Overcrowded map? Want to find specific pin/s? Just head over to your map and look to the top left and search away, use double quotes to search for a pin name with your exact search. You can also set it to whitelist mode or not to hide everything except your search or show everything except your search. RegEx is also a feature if you want a more versatile way to search pins!. Search window is too obstructive? Just press 'Tab' to hide it! Don't want it at all? Just go to your config manager and disable "Show search window on startup"!

[Search Window alt](https://i.imgur.com/ovZkUuN.mp4)

![Search Window](https://i.imgur.com/ovZkUuN.gif)

### Bulk Pins Modification
Did you discover that there's actually more than one berry bush, and that all of your berry pins are too ambiguous? Fret not as you can do it over at the Search Window or at an existing Tracked Object entry. You also want to change the icon? You can do that too!

[Tracked Object Modify Existing Pins alt](https://i.imgur.com/SXO5Ro9.mp4)
[Search Window Modify Pins alt](https://i.imgur.com/UAab6UG.mp4)

![Tracked Object Modify Existing Pins](https://i.imgur.com/SXO5Ro9.gif)
![Search Window Modify Pins](https://i.imgur.com/UAab6UG.gif)

### Shareable Entries!
If you have Configuration Manager Installed you can open up the menu, copy the very last field in this mod's section and send that entire text to your friend and press the reload key. Both you and your friend has the same entries you have!. Although, it won't copy the pins you've already made.

## Mod Compatibilities
This is a pretty non-invasive mod so any pin related mods shouldn't break the game, but there might be some conflicts depending on how the mod behaves do report it to make the mod even more non-invasive.

Mods where I made mine compatible:
- [Pinnacle](https://valheim.thunderstore.io/package/ComfyMods/Pinnacle/) (I do recommend this a lot to have more range of features to modify pins)
- [UnderTheRadar](https://valheim.thunderstore.io/package/Kits_Bitz/UnderTheRadar/)

# Tutorial
## How to use
Let's say you want to track Copper deposits, 
- press the GUI key, (default: Ctrl + T) while looking to prefill the proper values, 
- Copper deposits' ID is called MineRock_Copper(Clone), 
- you can just leave it as is and press Track or if you want to track all mineable objects, 
- set the ID to MineRock to track Tin, Meteorite, Iron, Obsidian, and Stone, but you don't want to include stone, no problem!
- go to the Blacklist field and type 'stone', with this, objects that has the name MineRock will be pinned but if it has Stone in their name, it will be excluded.

- if you want the naming conventions for all those objects to not be the same name, then you have to have a different entry for each of them, but if you're too lazy, just wait for the object you want to appear, press the GUI Key while looking at it and press track or enter!

If you want to look for a pin in your map
- just head over to the map.
- check the top left
- type the pin's name (you can enclose it with double quotes, "pin name", to search for pins with the exact entry.
- and then search!

## Installation (manual)
Same as any other mods I assume, extract contents to Bepinex's Plugins folder
More detail:
- PinAssistant	
	- Download by clicking Manual		- contents are plugins folder, and some other files.
	- place it all in BepInEx's plugins folder in a new folder which you can name by yourself like "Pin Assistant"
	- done
- Json.Net
	- Download by clicking Manual
	- content is a single folder named "plugins" just merge it with your BepInEx's folder.
	- done
- Jotunn
	- Download by clicking Manual
	- content is a folder named "Jotunn", just place it inside your BepInEx's plugins folder.
	- done
- Config Manager
	- Download by clicking Manual
	- content is a single dll file called ConfigurationManager, just place it inside your BepInEx's plugin folder or create a new folder in "plugins" folder, named like "ConfigManager" and place it there. (BepInEx > plugins > [newFolder] > ConfigurationManager.dll
	- done

# Dev Introduction
My first ever game mod that involves coding, I made this mod as an automatic pinning feature like [Locator](https://valheim.thunderstore.io/package/purpledxd/Locator/), but after at the release, I thought, "hey this plugin doesn't seem like Auto Pin is the only main feature. Because you can also use the Tracked Object as an object configuration to pin objects easily by pressing a keybind without having to change the icon and naming it and such, so I named it from Auto Pin to Pin Assistant just before release.

I've learned quite a lot with modding valheim and got a lot of experience from using certain libraries and such. A very fruitful journey to program this mod, I hope you enjoy! I started working on this around September 2 midnight, so this mod took around 2 and a half weeks with some days taking a break cause I got lazy trying to understand new apis to make my mod work *cough* gui sucks *cough*. 

I made this mod due to auto pin mods like HotPin or Locator having troubles with their plugin, Locator had a problem with their console commands and HotPin just won't let you open the console. I found Locator from Purps to be pretty good, but it wasn't that user friendly with tracking custom objects, it lacked a few features but was really decent, so I thought about my own ideas.

If not for the many source codes out there like [Locator](https://valheim.thunderstore.io/package/purpledxd/Locator/), [Pinnacle](https://valheim.thunderstore.io/package/ComfyMods/Pinnacle/), [PressurePlate](https://valheim.thunderstore.io/package/MSchmoecker/PressurePlate/) and other plugins, I would have had a hard time making this as it referenced a lot of valheim codes from them Locator mostly, but the logics like dictionary is mostly from Google :v

Also, thank you to the mod developers who helped me setup to start on this project or gave me info with things i had trouble with.

# Technical
## Have a suggestion?
You can contact me on Discord, 'WxAaRoNxW', make a post on [Nexus](https://www.nexusmods.com/valheim/mods/2531) or add a 'New Issue' on [Github](https://github.com/aaron-yang0327-development/ValheimMods/issues).

## Known issues
Check these pages:
- [Nexus](https://www.nexusmods.com/valheim/mods/2531?tab=bugs)
- [Github](https://github.com/aaron-yang0327-development/ValheimMods/issues)
If you have a problem you can post an issue on one of these pages.