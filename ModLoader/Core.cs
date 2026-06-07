using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace ModLoader
{
    /// <summary>
    /// Main entry point for the ModLoader system
    /// Automatically loads when Unity starts
    /// </summary>
    public class Core
    {
        private static ModManager modManager;
        private static bool initialized = false;
        public static Harmony harmony;

        /// <summary>
        /// Version of the ModLoader
        /// </summary>
        public const string Version = "1.0.0";

        /// <summary>
        /// Unity calls this automatically at game startup
        /// This is the entry point for ModLoader
        /// </summary>
        /// 
        [RuntimeInitializeOnLoadMethod]
        private static void Start()
        {
            if (initialized)
            {
                ConsoleBase.WriteLine("[ModLoader] Already initialized, skipping...");
                return;
            }

            initialized = true;

            ConsoleBase.WriteLine($"[ModLoader] ModLoader v{Version} Starting...");
            ConsoleBase.WriteLine("[ModLoader] Loaded automatically via RuntimeInitializeOnLoadMethod");

            try
            {
                // Initialize Harmony for ModLoader itself (optional)
                harmony = new Harmony("com.modloader.core");

                // Create and initialize mod manager
                modManager = new ModManager();

                // Load all mods from Mods directory
                modManager.LoadAllMods();

                // Appliquer les patches internes du ModLoader (UI de gestion des mods, etc.)
                try
                {
                    harmony.PatchAll(Assembly.GetExecutingAssembly());
                    ConsoleBase.WriteLine("[ModLoader] Patches internes appliqués");
                }
                catch (Exception ex)
                {
                    ConsoleBase.WriteError("[ModLoader] Échec application patches internes : " + ex);
                }

                ConsoleBase.WriteSucces($"[ModLoader] ModLoader initialized successfully! Loaded {modManager.LoadedMods.Count} mod(s)\n");
            }
            catch (Exception ex)
            {
                ConsoleBase.WriteError($"[ModLoader] Failed to initialize ModLoader: {ex}");
            }
        }

        /// <summary>
        /// Get the ModManager instance
        /// </summary>
        public static ModManager GetModManager()
        {
            return modManager;
        }

        /// <summary>
        /// Check if ModLoader is initialized
        /// </summary>
        public static bool IsInitialized()
        {
            return initialized;
        }
    }
}
