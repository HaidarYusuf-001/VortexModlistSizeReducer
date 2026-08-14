# Vortex Modlist Size Reducer

Shrink your Vortex modlist by safely deleting overwritten hardlinked files. Standalone, insanely fast, saves SSD space.

## Description
I made this because SSD prices have absolutely skyrocketed by up to 4x lately, making storage space way too precious to waste on dead files the game isn't even using. 

This tool scans your Vortex staging folder and deletes mod files that get overwritten by other mods in your load order. It cleans out massive amounts of dead weight from your drive in just a few seconds.

**WHAT'S NEW (LATEST UPDATE)**
* **Truly Disabled Mods Detection:** The tool now features an advanced JSON state parser that reads Vortex's database to accurately differentiate between a "100% Overwritten Mod" and a mod you explicitly clicked "Disable" on. It will prompt you (Y/N) before deleting any truly disabled mods. 
* **Optional Live State Extension:** For users who frequently toggle mods on and off, an optional Vortex Extension is now available. It forces an instantaneous Redux state memory dump to guarantee 100% real-time accuracy when detecting disabled mods.
* **Verbose Logging:** Automatically generates a text log file containing the absolute paths of all deleted files, making it easy to track which mods were trimmed.
* **1-Click Vortex Dashboard Integration:** Run this tool directly from your Vortex Dashboard. It automatically detects your active game, profile, and staging folder.
* **Smart Safety Exclusions:** Strictly ignores all configuration files (`.ini`, `.json`, `.xml`), plugin data (`.esp`, `.esm`), UI elements (`.swf`), and `.dll` files. It only deletes heavy assets, making it 100% safe for your script mods and engine stability.

> **IMPORTANT WARNING**
> After using this tool, you can definitely still add new mods later. However, **do not change the load order of your already deployed mods after running this.** Because the overwritten files get physically deleted from your Staging Folder, changing the load order of existing mods later will result in missing files. Keep your downloaded mod archives (`.zip`/`.rar`) as a backup.

## Requirements
* Windows OS
* Deployment Method in Vortex MUST be set to **Hardlink Deployment**.

## Installation

**Main File (Vortex Modlist Size Reducer)**
1. Extract the `.exe` file anywhere on your computer.

**Optional File (Vortex Live State Dumper Extension)**
*Why install this? Vortex normally backs up its loadout state on an hourly schedule. If you disable a mod and immediately run the reducer tool, it might not detect the change unless you install this extension, which forces a real-time state update every time you deploy.*
1. Download the `VortexLiveStateDumper.zip` from the Optional Files section.
2. Open **Vortex** and click on the **Extensions** tab on the bottom left menu.
3. Drag and drop the `.zip` file into the **"Drop File(s)"** zone at the bottom right corner of the Extensions screen.
4. Vortex will install it instantly. Click **Restart Vortex** when prompted.

## How to Use (1-Click Dashboard Method)
1. In Vortex, go to your game's **Dashboard**.
2. On the **Tools** widget, click the **+** button to Add Tool.
3. Set the **Target** to the `VortexModlistReducer.exe` file.
4. Leave **Command Line** and **Start In** completely blank.
5. Save it. Make sure you have clicked **Deploy Mods** in Vortex, then click the Play button next to the tool to run it.

## Antivirus False Positives
Because the release build is a self-contained, single-file C# executable, it bundles the entire .NET runtime inside one `.exe`. Minor antivirus vendors (via VirusTotal heuristics) tend to flag this packaging method as a False Positive. The source code is fully open and auditable on GitHub.