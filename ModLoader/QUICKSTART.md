# ModLoader - Quick Start Guide

Get started with ModLoader in 5 minutes!

## For Modders

### 1. Setup Your Project

Create a new C# Class Library (.NET Framework 4.7.2):

```bash
dotnet new classlib -f net472 -n MyMod
```

### 2. Add References

Add these DLLs to your project:
- `ModLoader.dll`
- `0Harmony.dll`
- `Scripts.dll`
- `UnityEngine.dll`
- `UnityEngine.CoreModule.dll`
- `UnityEngine.UIElementsModule.dll`

### 3. Create Your Mod

```csharp
using ModLoader;
using HarmonyLib;

namespace MyMod
{
    public class MyMod : ModBase
    {
        public override string ModName => "My Mod";
        public override string ModVersion => "1.0.0";
        public override string ModAuthor => "YourName";

        protected override void OnInitialize()
        {
            Logger.Info("Hello from my mod!");
            PatchAll();
        }
    }
}
```

### 4. Add a Patch

```csharp
using BrokeProtocol.Client.UI;
using ModLoader.Helpers;

[HarmonyPatch(typeof(MainMenu), "Initialize")]
class MainMenu_Patch
{
    static void Postfix(MainMenu __instance)
    {
        // Get UI container (uiClone is protected)
        var uiClone = UIHelper.GetUIClone(__instance);
        if (uiClone == null) return;
        
        var label = UIHelper.CreateLabel("Modded!");
        uiClone.Add(label);
    }
}
```

### 5. Build & Test

1. Build your project
2. Copy `MyMod.dll` to `BrokeProtocol/Mods/`
3. Launch the game
4. Check `Mods/Logs/MyMod.log`

✅ Done! Your mod is running!

---

## For Game Integrators

### 1. Add ModLoader

Copy `ModLoader.dll` to `BrokeProtocol_Data/Managed/`

### 2. Initialize

Add this to your game startup:

```csharp
using ModLoader;

// In your startup code (e.g., SceneManager.Awake)
ModLoader.Core.Initialize();
```

### 3. Test

Run the game and check console for:
```
[ModLoader] ModLoader v1.0.0 Starting...
[ModLoader] Loaded X mod(s)
```

✅ Done! ModLoader is integrated!

---

## Common Tasks

### Show a Message
```csharp
UIHelper.ShowMessage("Hello!");
```

### Get the Player
```csharp
var player = GameHelper.GetLocalPlayer();
```

### Create a Button
```csharp
var btn = UIHelper.CreateButton("Click Me", () => {
    Logger.Info("Clicked!");
});
```

### Patch a Method
```csharp
[HarmonyPatch(typeof(ClassName), "MethodName")]
class MyPatch
{
    static void Postfix()
    {
        // Your code here
    }
}
```

### Log Messages
```csharp
Logger.Info("Info message");
Logger.Warning("Warning message");
Logger.Error("Error message");
```

---

## Folder Structure

```
BrokeProtocol/
├── BrokeProtocol_Data/
│   └── Managed/
│       └── ModLoader.dll        ← Place here
└── Mods/                         ← Place mods here
    ├── MyMod.dll
    └── Logs/
        └── MyMod.log            ← Check logs here
```

---

## Need More Help?

- 📚 [Complete Modding Guide](README_MODDERS.md)
- 🔧 [Integration Guide](INTEGRATION.md)
- 💡 [Example Mod](ExampleMod/)
- 📖 [Main README](README.md)

---

## Quick Reference

### ModBase Methods
- `OnInitialize()` - Override this
- `PatchAll()` - Apply all patches
- `Logger.Info/Warning/Error()` - Logging

### UIHelper
- `GetMainMenu()` / `GetHUD()` - Get UI
- `CreateButton()` / `CreateLabel()` - Create UI
- `ShowMessage()` - Show message
- `FindElement<T>()` - Find element

### GameHelper
- `GetLocalPlayer()` - Get player
- `GetClManager()` - Get manager
- `IsGameStarted()` - Check game state
- `ExecuteCommand()` - Run command

### Harmony Basics
```csharp
[HarmonyPatch(typeof(Class), "Method")]
class Patch
{
    static void Prefix() { }  // Before
    static void Postfix() { } // After
}
```

---

**Happy Modding! 🎮**

For detailed documentation, see [README_MODDERS.md](README_MODDERS.md)

