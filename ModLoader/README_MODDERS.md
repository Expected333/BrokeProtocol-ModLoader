# ModLoader - Modding Guide for Broke Protocol

Welcome to the ModLoader system for Broke Protocol! This guide will help you create mods quickly and easily.

## Table of Contents

- [Getting Started](#getting-started)
- [Creating Your First Mod](#creating-your-first-mod)
- [ModLoader API](#modloader-api)
- [Harmony Patching](#harmony-patching)
- [UI Modification](#ui-modification)
- [Helper Classes](#helper-classes)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

## Getting Started

### Prerequisites

- Visual Studio 2019 or later (or any C# IDE)
- .NET Framework 4.7.2
- Broke Protocol installed
- Basic knowledge of C#

### Required DLLs

Your mod project needs references to these DLLs (found in `BrokeProtocol_Data/Managed/`):

- `ModLoader.dll` (this ModLoader)
- `0Harmony.dll` (for patching)
- `Scripts.dll` (game code)
- `UnityEngine.dll`
- `UnityEngine.CoreModule.dll`
- `UnityEngine.UIElementsModule.dll`

## Creating Your First Mod

### Step 1: Create a New Project

1. Create a new **Class Library** project (.NET Framework 4.7.2)
2. Name it something like "MyAwesomeMod"
3. Add references to the required DLLs listed above

### Step 2: Create Your Mod Class

```csharp
using ModLoader;
using HarmonyLib;

namespace MyAwesomeMod
{
    public class MyMod : ModBase
    {
        public override string ModName => "My Awesome Mod";
        public override string ModVersion => "1.0.0";
        public override string ModAuthor => "YourName";

        protected override void OnInitialize()
        {
            Logger.Info("My mod is loading!");
            
            // Apply Harmony patches
            PatchAll();
            
            Logger.Info("My mod loaded successfully!");
        }
    }
}
```

### Step 3: Build and Deploy

1. Build your project
2. Copy the output DLL to `BrokeProtocol/Mods/` folder
3. Launch the game
4. Check `Mods/Logs/MyAwesomeMod.log` for your mod's logs

## ModLoader API

### IMod Interface

All mods must implement the `IMod` interface (or inherit from `ModBase`):

```csharp
public interface IMod
{
    string ModName { get; }
    string ModVersion { get; }
    string ModAuthor { get; }
    void OnLoad();
}
```

### ModBase Class

`ModBase` is a convenient base class that provides:

- **harmony**: Harmony instance for patching
- **Logger**: Logging system
- **PatchAll()**: Helper to apply all patches

```csharp
public class MyMod : ModBase
{
    protected override void OnInitialize()
    {
        // Your initialization code here
        Logger.Info("Initializing...");
        PatchAll();
    }
}
```

### ModLogger

The logger provides three levels of logging:

```csharp
Logger.Info("Information message");
Logger.Warning("Warning message");
Logger.Error("Error message");
Logger.Error(exception); // Log an exception
```

Logs are written to:
- Unity console
- `Mods/Logs/{ModName}.log` file

## Harmony Patching

Harmony allows you to modify existing game methods without changing the original code.

### Basic Patch Structure

```csharp
[HarmonyPatch(typeof(ClassName), "MethodName")]
class ClassName_MethodName_Patch
{
    static void Postfix(ClassName __instance)
    {
        // Runs after the original method
    }
}
```

### Patch Types

#### Prefix Patch
Runs **before** the original method.

```csharp
[HarmonyPatch(typeof(MainMenu), "Quit")]
class MainMenu_Quit_Patch
{
    static bool Prefix()
    {
        Debug.Log("About to quit!");
        return true; // true = continue, false = skip original
    }
}
```

#### Postfix Patch
Runs **after** the original method.

```csharp
[HarmonyPatch(typeof(MainMenu), "Initialize")]
class MainMenu_Initialize_Patch
{
    static void Postfix(MainMenu __instance)
    {
        // __instance is the MainMenu object
        Debug.Log("Menu initialized!");
    }
}
```

#### Transpiler Patch (Advanced)
Modifies the IL code of the method. For advanced users only.

### Accessing Method Parameters

```csharp
[HarmonyPatch(typeof(SomeClass), "SomeMethod")]
class SomeClass_SomeMethod_Patch
{
    // Parameters must match the original method
    static void Postfix(SomeClass __instance, string param1, int param2)
    {
        Debug.Log($"Called with: {param1}, {param2}");
    }
}
```

### Modifying Return Values

```csharp
[HarmonyPatch(typeof(SomeClass), "GetValue")]
class SomeClass_GetValue_Patch
{
    static void Postfix(ref int __result)
    {
        // Modify the return value
        __result = __result * 2;
    }
}
```

## UI Modification

ModLoader provides powerful helpers for modifying the UI.

### Accessing UI Elements

```csharp
// Get the main menu
MainMenu mainMenu = UIHelper.GetMainMenu();

// Get any menu by ID
Menu menu = UIHelper.GetMenu("SomeMenuID");

// Get the HUD
HUD hud = UIHelper.GetHUD();
```

### Creating UI Elements

```csharp
// Create a button
Button myButton = UIHelper.CreateButton("Click Me", () => {
    UIHelper.ShowMessage("Button clicked!");
});

// Create a styled button
Button styledButton = UIHelper.CreateStyledButton(
    "Styled Button",
    Color.blue,      // background
    Color.white,     // text color
    () => Debug.Log("Clicked!")
);

// Create a label
Label label = UIHelper.CreateLabel("Hello World");

// Create a styled label
Label styledLabel = UIHelper.CreateStyledLabel(
    "Styled Text",
    Color.yellow,
    20  // font size
);
```

### Modifying Existing UI

**Important:** The `uiClone` field in Panel is protected, so you need to use `UIHelper.GetUIClone()` to access it:

```csharp
[HarmonyPatch(typeof(MainMenu), "Initialize")]
class MainMenu_Patch
{
    static void Postfix(MainMenu __instance)
    {
        // Get the UI container (uiClone is protected)
        var uiClone = UIHelper.GetUIClone(__instance);
        if (uiClone == null) return;
        
        // Find an existing button
        Button onlineBtn = uiClone.Q<Button>("Online");
        
        // Modify its properties
        onlineBtn.style.backgroundColor = Color.green;
        onlineBtn.text = "PLAY ONLINE";
        
        // Add your own button
        Button customBtn = UIHelper.CreateButton("Custom", () => {
            UIHelper.ShowMessage("Custom button!");
        });
        
        uiClone.Add(customBtn);
    }
}
```

### Styling UI Elements

```csharp
element.style.backgroundColor = Color.blue;
element.style.color = Color.white;
element.style.fontSize = 20;
element.style.width = 200;
element.style.height = 50;
element.style.marginTop = 10;
element.style.paddingLeft = 5;
element.style.position = Position.Absolute;
element.style.top = 100;
element.style.left = 50;
```

## Helper Classes

### UIHelper

Provides quick access to UI systems.

```csharp
// Get managers
ClManager clManager = UIHelper.GetClManager();

// Get UI elements
MainMenu mainMenu = UIHelper.GetMainMenu();
HUD hud = UIHelper.GetHUD();
Menu menu = UIHelper.GetMenu("MenuID");

// Access protected uiClone from Panel
var uiClone = UIHelper.GetUIClone(menuInstance);

// Create elements
Button btn = UIHelper.CreateButton("Text", onClick);
Label lbl = UIHelper.CreateLabel("Text");

// Show messages
UIHelper.ShowMessage("Hello player!");

// Find elements
Button someBtn = UIHelper.FindElement<Button>("ButtonName");

// Check menu state
bool isOpen = UIHelper.IsAnyMenuOpen();
```

### GameHelper

Provides quick access to game systems.

```csharp
// Get managers
ClManager clManager = GameHelper.GetClManager();
SceneManager sceneManager = GameHelper.GetSceneManager();
ShManager shManager = GameHelper.GetShManager();

// Get player
ShPlayer localPlayer = GameHelper.GetLocalPlayer();

// Get entities
ShEntity entity = GameHelper.GetEntityByID(123);
ShPlayer player = GameHelper.GetPlayerByID(456);

// Game state
bool inGame = GameHelper.IsGameStarted();
bool connected = GameHelper.IsConnected();
string version = GameHelper.GetGameVersion();

// Send chat message (use / for commands like /heal)
GameHelper.SendChatMessage("/heal");

// Logging
GameHelper.LogToConsole("Debug message");

// Paths
string dataPath = GameHelper.GetDataPath();
string persistentPath = GameHelper.GetPersistentDataPath();
```

## Best Practices

### 1. Always Use Try-Catch in Critical Code

```csharp
protected override void OnInitialize()
{
    try
    {
        // Your code
        PatchAll();
    }
    catch (Exception ex)
    {
        Logger.Error($"Initialization failed: {ex}");
    }
}
```

### 2. Log Important Events

```csharp
Logger.Info("Loading configuration...");
Logger.Info("Patches applied successfully");
Logger.Warning("Optional feature disabled");
Logger.Error("Failed to load resource");
```

### 3. Use Unique Harmony IDs

The ModBase class automatically creates unique IDs, but if you create Harmony manually:

```csharp
harmony = new Harmony("com.yourname.modname");
```

### 4. Test Your Mod Thoroughly

- Test with other mods installed
- Test in different game scenarios
- Check for errors in the log file

### 5. Handle Null References

```csharp
MainMenu menu = UIHelper.GetMainMenu();
if (menu != null)
{
    // Use menu
}
```

### 6. Don't Block the Main Thread

Avoid long-running operations in patches. Use coroutines or async for heavy tasks.

## Common Patterns

### Adding a Custom Menu Button

```csharp
[HarmonyPatch(typeof(MainMenu), "Initialize")]
class AddCustomButton_Patch
{
    static void Postfix(MainMenu __instance)
    {
        Button customBtn = UIHelper.CreateButton("My Feature", () => {
            UIHelper.ShowMessage("Feature activated!");
        });
        
        __instance.uiClone.Add(customBtn);
    }
}
```

### Accessing UI Elements in Menus

Since `uiClone` is protected in Panel, always use the helper:

```csharp
[HarmonyPatch(typeof(SomeMenu), "SomeMethod")]
class Patch
{
    static void Postfix(SomeMenu __instance)
    {
        var uiClone = UIHelper.GetUIClone(__instance);
        if (uiClone != null)
        {
            // Now you can modify the UI
            var button = uiClone.Q<Button>("SomeButton");
        }
    }
}
```

### Modifying Player Stats

```csharp
ShPlayer player = GameHelper.GetLocalPlayer();
if (player != null)
{
    player.health = 100f;
    player.stamina = 100f;
}
```

### Creating a Settings System

```csharp
public class MySettings
{
    public bool FeatureEnabled { get; set; } = true;
    public int SomeValue { get; set; } = 10;
}

private MySettings settings = new MySettings();

protected override void OnInitialize()
{
    LoadSettings();
    ApplySettings();
}
```

### Hooking Into Game Events

```csharp
[HarmonyPatch(typeof(ShPlayer), "Damage")]
class Player_Damage_Patch
{
    static void Prefix(ShPlayer __instance, ref float amount)
    {
        // Reduce damage by 50%
        amount *= 0.5f;
    }
}
```

## Troubleshooting

### My Mod Isn't Loading

1. Check that the DLL is in the `Mods/` folder
2. Check `Mods/Logs/` for error messages
3. Verify all required DLLs are referenced
4. Make sure your class implements `IMod` or inherits from `ModBase`

### Harmony Patches Not Working

1. Verify the class and method names are correct
2. Check that method parameters match exactly
3. Use `Logger.Info()` to verify your patch is being called
4. Check for exceptions in the log

### Can't Access Game Objects

1. Verify the game is fully loaded (`GameHelper.IsGameStarted()`)
2. Check for null references
3. Make sure you're on the right thread (Unity main thread)

### Building Errors

1. Verify .NET Framework 4.7.2 is installed
2. Check that all DLL references are correct
3. Update the DLL paths in your project references

## Advanced Topics

### Multiple Mods in One DLL

You can have multiple mod classes in one DLL:

```csharp
public class UIMod : ModBase { ... }
public class GameplayMod : ModBase { ... }
```

Both will be loaded automatically.

### Conditional Patching

```csharp
protected override void OnInitialize()
{
    if (SomeCondition())
    {
        harmony.Patch(
            original: AccessTools.Method(typeof(SomeClass), "SomeMethod"),
            postfix: new HarmonyMethod(typeof(MyPatch), "Postfix")
        );
    }
}
```

### Inter-Mod Communication

```csharp
ModManager modManager = Core.GetModManager();
if (modManager.IsModLoaded("OtherModName"))
{
    IMod otherMod = modManager.GetMod("OtherModName");
    // Interact with the other mod
}
```

## Example Projects

Check the `ExampleMod/` folder for complete working examples:

- `ExampleUIMod.cs` - Basic UI modification
- More examples coming soon!

## Getting Help

- Check the log files in `Mods/Logs/`
- Review the example mods
- Debug with `Logger.Info()` statements
- Check Harmony documentation: https://harmony.pardeike.net/

## ModLoader API Reference

### Core Class

```csharp
Core.Initialize()                    // Initialize ModLoader
Core.GetModManager()                 // Get ModManager instance
Core.IsInitialized()                 // Check if initialized
Core.Version                         // ModLoader version
```

### ModManager Class

```csharp
modManager.LoadedMods               // List of all loaded mods
modManager.GetMod(name)             // Get a mod by name
modManager.IsModLoaded(name)        // Check if mod is loaded
```

Happy Modding! 🎮

