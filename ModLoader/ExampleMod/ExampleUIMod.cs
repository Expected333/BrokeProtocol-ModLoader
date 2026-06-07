using HarmonyLib;
using ModLoader;
using ModLoader.Helpers;
using BrokeProtocol.Client.UI;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;

namespace ExampleMod
{
    /// <summary>
    /// Example mod that demonstrates how to modify the UI
    /// This mod adds a custom label to the main menu
    /// </summary>
    public class ExampleUIMod : ModBase
    {
        public override string ModName => "Example UI Mod";
        public override string ModVersion => "1.0.0";
        public override string ModAuthor => "YourName";

        protected override void OnInitialize()
        {
            Logger.Info("Example mod is initializing!");

            // Apply all Harmony patches in this assembly
            PatchAll();

            Logger.Info("Example mod initialized successfully!");
        }

        /// <summary>
        /// Patch the MainMenu Initialize method
        /// This runs after the main menu is initialized
        /// </summary>
        [HarmonyPatch(typeof(MainMenu), "Initialize")]
        class MainMenu_Initialize_Patch
        {
            static void Postfix(MainMenu __instance)
            {
                // Get the UI container (uiClone is protected, so we use reflection)
                var uiClone = UIHelper.GetUIClone(__instance);
                if (uiClone == null) return;

                // Create a custom label
                Label customLabel = UIHelper.CreateStyledLabel(
                    "🎮 Modded with ModLoader!",
                    Color.green,
                    20
                );

                // Position it at the top
                customLabel.style.position = Position.Absolute;
                customLabel.style.top = 10;
                customLabel.style.left = 10;

                // Add it to the main menu
                uiClone.Add(customLabel);

                ConsoleBase.WriteLine("[ExampleMod] Added custom label to main menu!");
            }
        }

        /// <summary>
        /// Another example: Patch a button click
        /// This modifies what happens when the Online button is clicked
        /// </summary>
        [HarmonyPatch(typeof(MainMenu), "Online")]
        class MainMenu_Online_Patch
        {
            static void Prefix()
            {
                // This runs BEFORE the original Online method
                UIHelper.ShowMessage("You clicked Online! (Modded message)");
                ConsoleBase.WriteLine("[ExampleMod] Online button clicked!");
            }
        }

        /// <summary>
        /// Example: Modify the quit button behavior
        /// </summary>
        [HarmonyPatch(typeof(MainMenu), "Quit")]
        class MainMenu_Quit_Patch
        {
            static bool Prefix()
            {
                // Show a custom message
                UIHelper.ShowMessage("Goodbye! Thanks for playing with mods!");

                // Return true to continue with the original method
                // Return false to prevent the original method from running
                return true;
            }
        }
    }
}

