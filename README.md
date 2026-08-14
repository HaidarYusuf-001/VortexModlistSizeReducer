# Vortex Modlist Size Reducer

Shrink your Vortex modlist by safely deleting overwritten hardlinked files. Standalone, insanely fast, saves SSD space.

## Description
I made this because SSD prices have absolutely skyrocketed by up to 4x lately, making storage space way too precious to waste on dead files the game isn't even using. 

This tool scans your Vortex staging folder and deletes mod files that get overwritten by other mods in your load order. It cleans out massive amounts of dead weight from your drive in just a few seconds.

This is highly recommended if you run a ton of texture mods that override each other, or if you use VRAMr (run this tool *after* VRAMr finishes).

**WHAT'S NEW**
* **1-Click Vortex Dashboard Integration:** Run this tool directly from your Vortex Dashboard. It automatically detects your active game and staging folder.
* **Smart Safety Exclusions:** Strictly ignores all configuration files (`.ini`, `.json`, `.txt`, etc.) and `.dll` plugins (like SKSE/F4SE). It only deletes heavy assets, making it 100% safe for your script mods and engine stability.
* **Verbose Logging:** Automatically generates a text log file containing the absolute paths of all deleted files, making it easy to track which mods were trimmed.

> **IMPORTANT WARNING**
> You don't need a 100% "final" modlist to use this—you can definitely still add new mods later. However, **do not change the load order of your already deployed mods after running this.** Because the overwritten files get physically deleted from your Staging Folder, changing the load order of existing mods later will result in missing files. Keep your downloaded mod archives (`.zip`/`.rar`) as a backup.

*Note: This tool relies entirely on NTFS Hardlinks and is built strictly for Vortex. If you use Mod Organizer 2 (MO2), use the ConflictDeleter plugin by LostDragonist instead.*

## Requirements
* Windows OS
* Deployment Method in Vortex MUST be set to **Hardlink Deployment**.

## Building from Source
If you want to compile the binary yourself instead of downloading the release:
1. Ensure you have the .NET 8.0 SDK (or newer) installed.
2. Clone this repository.
3. Open a terminal in the project root and run:
   ```bash
   dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
   ```
4. The executable will be generated in `bin\Release\net8.0\win-x64\publish\`.

## Usage Instructions

### Method 1: 1-Click Vortex Dashboard (Recommended)
1. In Vortex, go to your game's **Dashboard**.
2. On the **Tools** widget, click the **+** button to Add Tool.
3. Set the **Target** to the `VortexModlistReducer.exe` file.
4. Leave **Command Line** and **Start In** completely blank.
5. Save it, click the Play button, and it will run and auto-detect your staging folder automatically.

### Method 2: Manual PowerShell
1. Open Windows PowerShell.
2. Run the tool using the Call Operator (`&`) and provide the path to your Vortex Mod Staging Folder in quotes.
   ```powershell
   & "D:\Tools\VortexModlistReducer.exe" "C:\Vortex Mods\skyrimse"
   ```

## Antivirus False Positives
Because the release build is a self-contained, single-file C# executable, it bundles the entire .NET runtime inside one `.exe`. Minor antivirus vendors (via VirusTotal heuristics) tend to flag this packaging method as a False Positive. The source code is fully open and auditable here.