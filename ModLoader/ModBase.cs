using HarmonyLib;
using System;

namespace ModLoader
{
    /// <summary>
    /// Base class for mods with helper utilities
    /// Mods can inherit from this class for easy setup
    /// </summary>
    public abstract class ModBase : IMod
    {
        public abstract string ModName { get; }
        public abstract string ModVersion { get; }
        public abstract string ModAuthor { get; }

        /// <summary>
        /// Harmony instance for this mod
        /// Use this to apply patches: harmony.PatchAll()
        /// </summary>
        protected Harmony harmony;

        /// <summary>
        /// Logger instance for this mod
        /// Use Logger.Info(), Logger.Warning(), Logger.Error()
        /// </summary>
        protected ModLogger Logger;

        /// <summary>
        /// Called by ModLoader when loading the mod
        /// DO NOT OVERRIDE - Override OnInitialize() instead
        /// </summary>
        public virtual void OnLoad()
        {
            try
            {
                // Create Harmony instance with unique ID
                harmony = new Harmony($"com.{ModAuthor}.{ModName}");

                // Create logger for this mod
                Logger = new ModLogger(ModName);

                Logger.Info($"Loading {ModName} v{ModVersion} by {ModAuthor}");

                // Call mod initialization
                OnInitialize();

                Logger.Info($"{ModName} loaded successfully!");
            }
            catch (Exception ex)
            {
                if (Logger != null)
                {
                    Logger.Error($"Failed to load {ModName}: {ex}");
                }
                else
                {
                    ConsoleBase.WriteLine($"[ModLoader] Failed to load {ModName}: {ex}");
                }
                throw;
            }
        }

        /// <summary>
        /// Override this method to initialize your mod
        /// This is where you should set up your patches, hooks, etc.
        /// </summary>
        protected abstract void OnInitialize();

        /// <summary>
        /// Helper method to apply all Harmony patches in the mod assembly
        /// </summary>
        protected void PatchAll()
        {
            try
            {
                harmony.PatchAll(GetType().Assembly);
                Logger.Info("All Harmony patches applied successfully");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to apply Harmony patches: {ex}");
                throw;
            }
        }
    }
}

