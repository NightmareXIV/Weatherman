# Weatherman
Don't wait for perfect weather. Create it yourself!
Weatherman allows you to take precise control over weather and time flow in FFXIV. From as simple things as removing rains or bringing everlasting day to precise selection of weather and time for every single zone individually - Weatherman got you covered.
## Features
* Take control over time, globally for whole world or zone by zone. You can set time to be fixed, replace night cycle with another (optionally reversed) day cycle or vise verasa. Zone specific settings override global ones, so you can put whole world in eternal sunlight while you will be enjoying endless nights at your home.
* Take control over weather zone by zone. Set not only normal weathers but any that zone supports. You can bring back light filled skies in Norvrandt or make it snow in Limsa even at summers. You can even select several weathers from which random one will be selected each time you enter zone.
* Blacklist unwanted weathers. Prevent them from occurring in the whole world. You don't like rains? Never see them again in just couple mouse clicks. Zone specific settings will override blacklist, so if you like it somewhere - you don't have to give it up completely.

Note: Weatherman only changes visuals. Weather-specific mobs, events, fates etc. will not be altered. 
## Note
Some people say that changing weather and time can allow you doing things that aren't possible otherwise without naturally waiting for needed weather and time. However, after continuous usage and testing I was not able to confirm it myself. If you will be able to find something like that, please create issue about it and I'll see what can I do to fix it.
## Vistas
Vistas conditions are checked server-side. It's not possible to cheat them with this plugin. However to prevent any possibility of sending data to servers that would be impossible without this plugin once you approach vista plugin will pause weather and time processing and restore them to normal states. I recommend that you pause plugin execution if you are actively doing vistas though (`/weatherman` -> debug -> pause plugin execution). 
## How to install
First, you need to be using FFXIV Quick Launcher and have in-game features enabled. Type `/xlplugins` in chat, press "Settings" button and go to "Experimental" tab. Paste this url: `https://raw.githubusercontent.com/Eternita-S/MyDalamudPlugins/main/pluginmaster.json` into "Custom plugin repositories" field and click "+" button that will appear at right side of it, then click "Save and close" button and if you did everything correctly you should be able to install Weatherman like any other plugin.
