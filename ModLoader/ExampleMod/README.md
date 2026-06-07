# Example Mod

This is a simple example mod that demonstrates how to create mods for Broke Protocol using the ModLoader system.

## What This Mod Does

- Adds a custom green label "Modded with ModLoader!" to the main menu
- Shows a custom message when clicking the Online button
- Shows a goodbye message when quitting the game

## How to Use This as a Template

1. Copy this folder as a starting point for your own mod
2. Rename the namespace and class to match your mod
3. Update `ModName`, `ModVersion`, and `ModAuthor` properties
4. Implement your custom logic in `OnInitialize()`
5. Add Harmony patches to modify game behavior

## Building Your Mod

1. Create a new C# Class Library project (.NET Framework 4.7.2)
2. Add references to:
   - `ModLoader.dll`
   - `0Harmony.dll`
   - `Scripts.dll`
   - Unity DLLs (UnityEngine, UnityEngine.CoreModule, UnityEngine.UIElementsModule)
3. Set your output path to the game's `Mods/` folder for easy testing
4. Build your project

## Testing Your Mod

1. Build your mod DLL
2. Place it in the game's `Mods/` folder
3. Launch the game
4. Check the logs in `Mods/Logs/YourModName.log`

## Common Patterns

### Patching a Method

```csharp
[HarmonyPatch(typeof(ClassName), "MethodName")]
class ClassName_MethodName_Patch
{
    static void Postfix(ClassName __instance)
    {
        // Your code here - runs after the original method
    }
}
```

### Modifying UI

```csharp
// Create a button
var button = UIHelper.CreateButton("Click Me", () => {
    UIHelper.ShowMessage("Button clicked!");
});

// Create a label
var label = UIHelper.CreateStyledLabel("Hello!", Color.yellow, 16);

// Add to a menu
menuInstance.uiClone.Add(button);
```

### Using Helpers

```csharp
// Get the local player
var player = GameHelper.GetLocalPlayer();

// Show a message
UIHelper.ShowMessage("Hello player!");

// Check if in game
if (GameHelper.IsGameStarted())
{
    Logger.Info("Game is running!");
}
```

## Need Help?

Check the main documentation in `README_MODDERS.md` for more detailed information.

