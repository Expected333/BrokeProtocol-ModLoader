# ModLoader for Broke Protocol

A powerful and easy-to-use mod loading system for Broke Protocol that allows modders to create client-side modifications using Harmony patches.

## Features

- 🔌 **Automatic Mod Loading** - Drop DLLs in the Mods folder and they load automatically
- 🎯 **Harmony Integration** - Full Harmony support for patching game methods
- 📝 **Built-in Logging** - Each mod gets its own log file
- 🛠️ **Helper APIs** - Convenient helpers for UI and game system access
- 🔒 **Error Isolation** - One mod crashing won't break others
- 📚 **Well Documented** - Complete documentation and examples included

## Quick Start

### For Game Developers

1. Build the `ModLoader.dll`
2. Copy it to `BrokeProtocol_Data/Managed/`
3. Add it to `BrokeProtocol_Data/ScriptingAssemblies.json`
4. **Done!** ModLoader loads automatically via `[RuntimeInitializeOnLoadMethod]`
5. Mods will automatically load from the `Mods/` folder

See [INTEGRATION.md](INTEGRATION.md) for detailed integration instructions and [LOADING_EXPLAINED.md](LOADING_EXPLAINED.md) for how automatic loading works.

### For Modders

1. Create a new C# Class Library project (.NET Framework 4.7.2)
2. Reference `ModLoader.dll` and required Unity/game DLLs
3. Create a class inheriting from `ModBase`
4. Implement your mod logic
5. Build and place your DLL in the game's `Mods/` folder

See [README_MODDERS.md](README_MODDERS.md) for the complete modding guide.

## Project Structure

```
ModLoader/
├── Core.cs                  - Main entry point
├── IMod.cs                  - Mod interface
├── ModBase.cs              - Base class for mods
├── ModLogger.cs            - Logging system
├── ModManager.cs           - Mod loading and management
├── Helpers/
│   ├── UIHelper.cs         - UI utility methods
│   └── GameHelper.cs       - Game system utilities
├── ExampleMod/
│   ├── ExampleUIMod.cs    - Example mod implementation
│   └── README.md          - Example documentation
├── README.md              - This file
├── README_MODDERS.md      - Complete modding guide
└── INTEGRATION.md         - Game integration guide
```

## Architecture

### ModLoader Flow

1. **Initialize** - `Core.Initialize()` is called at game startup
2. **Scan** - ModManager scans the `Mods/` folder for DLLs
3. **Load** - Each DLL is loaded and scanned for `IMod` implementations
4. **Instantiate** - Mod instances are created
5. **OnLoad** - Each mod's `OnLoad()` method is called
6. **Patch** - Mods apply Harmony patches to game methods

### Mod Lifecycle

```
Game Starts
    ↓
Core.Initialize()
    ↓
ModManager.LoadAllMods()
    ↓
For each DLL in Mods/:
    Load Assembly
    Find IMod classes
    Create instances
    Call OnLoad()
        ↓
    Mod.OnInitialize()
        Apply Harmony patches
        Set up mod logic
```

## API Overview

### Core Classes

- **Core** - Entry point and initialization
- **ModManager** - Loads and manages all mods
- **ModBase** - Base class for easy mod creation
- **ModLogger** - Per-mod logging system

### Helper Classes

- **UIHelper** - Quick access to UI elements and creation
- **GameHelper** - Quick access to game systems and entities

## Example Mod

```csharp
using ModLoader;
using HarmonyLib;
using BrokeProtocol.Client.UI;

namespace MyMod
{
    public class MyAwesomeMod : ModBase
    {
        public override string ModName => "My Awesome Mod";
        public override string ModVersion => "1.0.0";
        public override string ModAuthor => "YourName";

        protected override void OnInitialize()
        {
            Logger.Info("Loading mod...");
            PatchAll();
        }

        [HarmonyPatch(typeof(MainMenu), "Initialize")]
        class MainMenu_Patch
        {
            static void Postfix(MainMenu __instance)
            {
                // Add custom UI element
                var label = UIHelper.CreateLabel("Modded!");
                __instance.uiClone.Add(label);
            }
        }
    }
}
```

## Requirements

- .NET Framework 4.7.2
- Harmony 2.x
- Unity (version matching Broke Protocol)
- Broke Protocol Scripts.dll

## Building

1. Open the solution in Visual Studio
2. Restore NuGet packages
3. Update DLL reference paths if needed
4. Build in Release mode
5. Output: `bin/Release/ModLoader.dll`

## Installation

### In Broke Protocol

1. Copy `ModLoader.dll` to `BrokeProtocol_Data/Managed/`
2. Patch game startup to call `ModLoader.Core.Initialize()`
3. Create `Mods/` folder in game root (if it doesn't exist)

See [INTEGRATION.md](INTEGRATION.md) for detailed steps.

## Folder Structure After Installation

```
BrokeProtocol/
├── BrokeProtocol.exe
├── BrokeProtocol_Data/
│   └── Managed/
│       ├── ModLoader.dll          ← ModLoader
│       ├── Scripts.dll
│       └── ...
└── Mods/                          ← Mods go here
    ├── MyMod.dll
    ├── AnotherMod.dll
    ├── Logs/                      ← Mod logs
    │   ├── MyMod.log
    │   └── AnotherMod.log
    └── Config/                    ← Future: Mod configs
```

## Documentation

- [README_MODDERS.md](README_MODDERS.md) - Complete guide for mod creators
- [INTEGRATION.md](INTEGRATION.md) - How to integrate ModLoader into the game
- [ExampleMod/README.md](ExampleMod/README.md) - Example mod walkthrough

## Version

**ModLoader v1.0.0**

## License

This ModLoader is designed for use with Broke Protocol. Please respect the game's terms of service and licensing.

## Credits

- Uses [Harmony](https://github.com/pardeike/Harmony) for runtime patching
- Built for [Broke Protocol](https://brokeprotocol.com/)

## Support

For mod development help, see the comprehensive guide in [README_MODDERS.md](README_MODDERS.md).

For integration help, see [INTEGRATION.md](INTEGRATION.md).

