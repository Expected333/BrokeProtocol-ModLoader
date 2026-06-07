# ModLoader - How Automatic Loading Works

This document explains how ModLoader loads automatically at game startup.

## 🚀 Automatic Loading

### Step 1: Unity Recognizes the DLL

When you add `ModLoader.dll` to `ScriptingAssemblies.json`:

```json
{
    "names": [
        "Scripts.dll",
        "ModLoader.dll"  ← Your DLL here
    ]
}
```

Unity knows it must load this DLL at startup.

### Step 2: Unity Scans for Attributes

At startup, Unity scans **all loaded DLLs** for methods with the `[RuntimeInitializeOnLoadMethod]` attribute.

### Step 3: Unity Calls `Initialize()` Automatically

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
public static void Initialize()
{
    // This method is called AUTOMATICALLY by Unity
    // No need to call it manually!
}
```

## 📋 Loading Order

```
1. Unity starts
   ↓
2. Unity loads all DLLs from ScriptingAssemblies.json
   ↓
3. Unity scans for [RuntimeInitializeOnLoadMethod]
   ↓
4. Unity calls ModLoader.Core.Initialize()
   ↓
5. ModLoader creates the ModManager
   ↓
6. ModManager scans the Mods/ folder
   ↓
7. ModManager loads all mods (DLLs)
   ↓
8. Each mod.OnLoad() is called
   ↓
9. Mods apply their Harmony patches
   ↓
10. The game continues normally with active mods
```

## 🎯 RuntimeInitializeLoadType variants

```csharp
// Before the first scene loads
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]

// Before Awake() on objects
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]

// Before Start() on objects
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]

// After scene load (RECOMMENDED for ModLoader)
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]

// Scene subsystem
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
```

**For ModLoader we use `AfterSceneLoad`** because:
- Unity systems are initialized
- Game managers exist
- UI is accessible
- Mods can patch systems that are already loaded

## 🔍 Verify It Works

### In the Unity logs

You should see these messages:

```
[ModLoader] ModLoader v1.0.0 Starting...
[ModLoader] Loaded automatically via RuntimeInitializeOnLoadMethod
[ModLoader] Scanning for mods in: C:/Path/To/Game/Mods
[ModLoader] Found X DLL file(s)
[ModLoader] ✓ Loaded mod: ModName v1.0.0 by Author
[ModLoader] ModLoader initialized successfully! Loaded X mod(s)
```

### If nothing shows up

1. **Check ScriptingAssemblies.json**
   - File lives in `BrokeProtocol_Data/`
   - `ModLoader.dll` is in the `names` list
   - JSON syntax is valid

2. **Check the DLL location**
   - `ModLoader.dll` must be in `BrokeProtocol_Data/Managed/`
   - Not in `Mods/` (Unity won't load it from there)

3. **Check dependencies**
   - `0Harmony.dll` is present
   - All required Unity DLLs are present

4. **Check Unity logs**
   - Log file: `BrokeProtocol_Data/output_log.txt`
   - Or in Player.log (see section below)

## 📁 File Layout

```
BrokeProtocol/
├── BrokeProtocol.exe
├── BrokeProtocol_Data/
│   ├── ScriptingAssemblies.json    ← Add ModLoader.dll here
│   ├── output_log.txt              ← Unity logs (sometimes)
│   └── Managed/
│       ├── ModLoader.dll           ← Your DLL HERE
│       ├── !_0Harmony.dll          ← Dependency
│       ├── Scripts.dll
│       └── ... (other Unity DLLs)
└── Mods/                           ← User mods
    ├── MyMod.dll
    └── Logs/
        └── MyMod.log
```

## 🛠️ Debugging

### Add debug logs

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
public static void Initialize()
{
    Debug.Log("[ModLoader] Initialize() was called!");
    Debug.Log($"[ModLoader] Current Time: {DateTime.Now}");
    Debug.Log($"[ModLoader] Data Path: {Application.dataPath}");

    // ... rest of code
}
```

### Check Player.log

**Windows:**
```
C:\Users\[Username]\AppData\LocalLow\[Company]\[Game]\Player.log
```

**For this game:**
```
%USERPROFILE%\AppData\LocalLow\Broke Protocol\Broke Protocol\Player.log
```

## ⚠️ Common Errors

### 1. "ModLoader not found in ScriptingAssemblies.json"

**Solution:** add it manually:

```json
{
    "names": [
        ...,
        "ModLoader.dll"
    ],
    "types": [
        ...,
        16
    ]
}
```

**Note:** the number of `types` must match the number of `names`!

### 2. "Method not found: ModLoader.Core.Initialize"

**Cause:** the method isn't static, or the attribute is misplaced.

**Solution:** make sure you have:
```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
public static void Initialize() // MUST be static
```

### 3. "ModLoader.dll is missing dependencies"

**Cause:** Harmony or other DLLs are missing.

**Solution:** verify that every reference is present in `Managed/`.

### 4. "Initialize() called multiple times"

**Cause:** multiple instances or repeated calls.

**Solution:** the code already guards against this:
```csharp
if (initialized)
{
    Debug.LogWarning("[ModLoader] Already initialized, skipping...");
    return;
}
```

## 🎓 Alternatives to Automatic Loading

If `[RuntimeInitializeOnLoadMethod]` doesn't work, here are other options:

### Option 2: Static constructor

```csharp
public class Core
{
    static Core()
    {
        // Runs the first time the class is referenced
        Initialize();
    }
}
```

**Problem:** only runs if something references the class.

### Option 3: MonoBehaviour with GameObject

```csharp
public class ModLoaderBehaviour : MonoBehaviour
{
    void Awake()
    {
        Core.Initialize();
        DontDestroyOnLoad(gameObject);
    }
}
```

**Problem:** requires a GameObject in the scene.

### Option 4: Harmony bootstrap patch

Create a separate DLL that patches game startup:

```csharp
[HarmonyPatch(typeof(SceneManager), "Awake")]
class Bootstrap
{
    static void Postfix()
    {
        ModLoader.Core.Initialize();
    }
}
```

## ✅ Final Recommendation

**Use `[RuntimeInitializeOnLoadMethod]`**. It is:
- ✅ Automatic
- ✅ Reliable
- ✅ Unity-standard
- ✅ Requires no game modification
- ✅ Works with just `ScriptingAssemblies.json`

## 📞 Support

If automatic loading still doesn't work:

1. Check `Player.log` for errors
2. Add `Debug.Log()` calls to trace execution
3. Confirm Unity actually loads your DLL
4. Try with a minimal test mod first

---

**ModLoader is now fully automatic!** 🎉

Just add `ModLoader.dll` to `ScriptingAssemblies.json` and to `Managed/`, and it will load on game startup.
