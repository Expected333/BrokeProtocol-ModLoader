# ModLoader Integration Guide

This guide explains how to integrate ModLoader into Broke Protocol to enable mod support.

## Prerequisites

- Harmony patching knowledge (or willingness to learn)
- Access to game startup code
- Ability to modify and rebuild game DLLs

## Integration Steps

### Step 1: Add ModLoader.dll to the Game

1. Build the ModLoader project
2. Copy `ModLoader.dll` to `BrokeProtocol_Data/Managed/`

### Step 2: Register in ScriptingAssemblies.json

Add `ModLoader.dll` to `BrokeProtocol_Data/ScriptingAssemblies.json`:

```json
{
    "names": [
        "Scripts.dll",
        "ModLoader.dll"
    ],
    "types": [
        16,
        16
    ]
}
```

**That's it!** ModLoader uses `[RuntimeInitializeOnLoadMethod]` to load automatically.

### Step 3 (Optional): Manual Initialization

If automatic loading doesn't work, you can call initialization manually.

### Automatic Loading (Default)

ModLoader uses Unity's `[RuntimeInitializeOnLoadMethod]` attribute, which means **it loads automatically** when you add it to `ScriptingAssemblies.json`.

You should see in the console:
```
[ModLoader] ModLoader v1.0.0 Starting...
[ModLoader] Loaded automatically via RuntimeInitializeOnLoadMethod
[ModLoader] Loaded X mod(s)
```

See [LOADING_EXPLAINED.md](LOADING_EXPLAINED.md) for detailed explanation.

### Manual Loading (If Automatic Doesn't Work)

#### Option A: Patch via Harmony

If automatic loading fails, create a simple loader DLL that patches the game startup:

```csharp
using HarmonyLib;
using BrokeProtocol.Managers;

namespace ModLoaderBootstrap
{
    public class Bootstrap
    {
        static Bootstrap()
        {
            var harmony = new Harmony("com.modloader.bootstrap");
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(SceneManager), "Awake")]
    class SceneManager_Awake_Patch
    {
        static void Postfix()
        {
            ModLoader.Core.Initialize();
        }
    }
}
```

Then use a DLL injector to load this bootstrap DLL.

#### Option B: Direct Integration (If You Have Source Access)

If you have access to modify the game source code:

```csharp
// In SceneManager.cs or similar startup class
using ModLoader;

public class SceneManager : MonoBehaviour
{
    private void Awake()
    {
        // ... existing code ...
        
        // Initialize ModLoader
        ModLoader.Core.Initialize();
        
        // ... rest of existing code ...
    }
}
```

#### Option C: Unity MonoBehaviour (Alternative)

Create a simple Unity script that loads early:

```csharp
using UnityEngine;

public class ModLoaderStarter : MonoBehaviour
{
    void Awake()
    {
        ModLoader.Core.Initialize();
        Destroy(gameObject); // Remove after initialization
    }
}
```

Attach this to a GameObject in the first scene, or create it programmatically.

### Step 3: Verify Integration

After integration, when the game starts you should see in the console:

```
[ModLoader] ModLoader v1.0.0 Starting...
[ModLoader] Scanning for mods in: C:/Path/To/Game/Mods
[ModLoader] Found X DLL file(s)
[ModLoader] ModLoader initialized successfully! Loaded X mod(s)
```

### Step 4: Test with Example Mod

1. Build the ExampleMod from `ExampleMod/ExampleUIMod.cs`
2. Copy the DLL to the `Mods/` folder
3. Start the game
4. You should see "Modded with ModLoader!" on the main menu

## Initialization Timing

### When to Initialize

✅ **Good Times:**
- After Unity initialization
- After core systems are set up
- Before UI loads
- In `SceneManager.Awake()`
- In `GameManager.Start()`

❌ **Bad Times:**
- Before Unity initialization
- During static constructors
- Too late (after menus are already created)

### Recommended Initialization Point

The best place is typically in the main scene's manager Awake/Start:

```csharp
[HarmonyPatch(typeof(SceneManager), "Awake")]
class InitializeModLoader
{
    static void Postfix()
    {
        if (!ModLoader.Core.IsInitialized())
        {
            ModLoader.Core.Initialize();
        }
    }
}
```

## Folder Structure

After integration, your game structure should look like:

```
BrokeProtocol/
├── BrokeProtocol.exe
├── BrokeProtocol_Data/
│   ├── Managed/
│   │   ├── ModLoader.dll          ← Add this
│   │   ├── 0Harmony.dll          ← Should already exist
│   │   ├── Scripts.dll
│   │   └── ... (other DLLs)
│   └── ... (other Unity data)
└── Mods/                          ← Created automatically
    ├── Logs/                      ← Created automatically
    └── Config/                    ← Created automatically
```

## Troubleshooting Integration

### ModLoader Not Initializing

**Symptom:** No ModLoader messages in console

**Solutions:**
1. Verify `ModLoader.dll` is in the Managed folder
2. Check that `Core.Initialize()` is being called
3. Add debug logs before the call to verify code path

```csharp
Debug.Log("About to initialize ModLoader...");
ModLoader.Core.Initialize();
Debug.Log("ModLoader initialization call completed");
```

### DLL Not Found Errors

**Symptom:** `FileNotFoundException` or `DllNotFoundException`

**Solutions:**
1. Verify all dependencies are present:
   - `0Harmony.dll`
   - `UnityEngine.UIElementsModule.dll`
   - All other Unity modules
2. Check DLL paths and versions match

### Mods Not Loading

**Symptom:** ModLoader initializes but no mods load

**Solutions:**
1. Check that `Mods/` folder exists in the correct location
2. Verify mods are valid DLLs
3. Check `Mods/Logs/` for error messages
4. Ensure mods implement `IMod` interface

### Initialization Too Early/Late

**Symptom:** Crashes or mods don't work properly

**Solutions:**
1. Move initialization to a later point (e.g., after UI systems init)
2. Or move it earlier (before UI creation if you need to patch early)
3. Test different initialization points

## Advanced: Custom Initialization

You can customize ModLoader initialization:

```csharp
// Initialize with custom logging
ModLoader.Core.Initialize();

// Get the mod manager
var modManager = ModLoader.Core.GetModManager();

// Check what mods loaded
foreach (var mod in modManager.LoadedMods)
{
    Debug.Log($"Loaded: {mod.ModName} v{mod.ModVersion}");
}

// Check if specific mod is loaded
if (modManager.IsModLoaded("SomeMod"))
{
    Debug.Log("SomeMod is active!");
}
```

## Bootstrap Loader (For Client Distribution)

If you want to distribute ModLoader without modifying game files, create a bootstrap DLL:

```csharp
// ModLoaderBootstrap.dll
using HarmonyLib;
using BrokeProtocol.Managers;

public class Bootstrap
{
    // This static constructor runs when the DLL is loaded
    static Bootstrap()
    {
        var harmony = new Harmony("com.modloader.bootstrap");
        harmony.PatchAll();
    }

    [HarmonyPatch(typeof(SceneManager), "Awake")]
    class LoadModLoader
    {
        static void Postfix()
        {
            ModLoader.Core.Initialize();
        }
    }
}
```

Then inject this DLL at game startup using a launcher or injector.

## Verification Checklist

After integration, verify:

- ✅ ModLoader.dll is in Managed folder
- ✅ Game starts without errors
- ✅ Console shows ModLoader initialization messages
- ✅ Mods/ folder is created
- ✅ Example mod loads successfully
- ✅ Mod logs are created in Mods/Logs/
- ✅ Mods can patch game methods
- ✅ UI modifications work

## Performance Considerations

ModLoader has minimal performance impact:

- **Initialization:** ~50-200ms (depends on number of mods)
- **Runtime:** No overhead (Harmony patches are native speed)
- **Memory:** ~1-5MB per mod (depends on mod complexity)

## Security Considerations

⚠️ **Important:** ModLoader executes arbitrary code from DLLs in the Mods folder.

**Recommendations:**
1. Only load mods from trusted sources
2. Consider code signing for official mods
3. Implement a whitelist system if needed
4. Scan mods for malicious code before distribution

## Support for Modders

Once integrated, direct modders to:

1. [README_MODDERS.md](README_MODDERS.md) - Complete modding guide
2. [ExampleMod/](ExampleMod/) - Working example
3. Game logs in `Mods/Logs/` for debugging

## Example Integration Projects

### Simple Harmony Bootstrap

```csharp
// File: ModLoaderInit.cs
using HarmonyLib;

[HarmonyPatch(typeof(BrokeProtocol.Managers.SceneManager), "Awake")]
public class ModLoaderInit
{
    static void Postfix()
    {
        ModLoader.Core.Initialize();
    }
}
```

Compile this separately and inject it, or include it in your game build.

## Questions?

If you encounter issues during integration:

1. Check console logs for errors
2. Verify all DLLs are in place
3. Test initialization timing
4. Review this guide again
5. Check Harmony documentation

## Next Steps

After successful integration:

1. Test with multiple mods
2. Create documentation for your modding community
3. Set up a mod repository or workshop
4. Consider creating official example mods
5. Join the modding community!

Happy Modding! 🎮

