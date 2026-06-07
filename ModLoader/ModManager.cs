using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ModLoader
{
    /// <summary>
    /// Manages loading and lifecycle of all mods
    /// </summary>
    public class ModManager
    {
        private readonly List<IMod> loadedMods = new List<IMod>();
        private readonly string modsDirectory;

        public IReadOnlyList<IMod> LoadedMods => loadedMods.AsReadOnly();

        public ModManager()
        {
            // Set mods directory path (relative to game's Data folder)
            modsDirectory = Path.Combine(Application.dataPath, "..", "Mods");

            // Create Mods directory if it doesn't exist
            if (!Directory.Exists(modsDirectory))
            {
                Directory.CreateDirectory(modsDirectory);
                ConsoleBase.WriteLine($"[ModLoader] Created Mods directory at: {modsDirectory}");
            }

            // Create subdirectories
            CreateSubdirectories();
        }

        private void CreateSubdirectories()
        {
            string logsDir = Path.Combine(modsDirectory, "Logs");
            string configDir = Path.Combine(modsDirectory, "Config");

            if (!Directory.Exists(logsDir))
            {
                Directory.CreateDirectory(logsDir);
            }

            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }
        }

        /// <summary>
        /// Loads all mods from the Mods directory
        /// </summary>
        public void LoadAllMods()
        {
            ConsoleBase.WriteLine($"[ModLoader] Scanning for mods in: {modsDirectory}");

            // Get all DLL files in the Mods directory
            string[] dllFiles = Directory.GetFiles(modsDirectory, "*.dll", SearchOption.TopDirectoryOnly);

            if (dllFiles.Length == 0)
            {
                ConsoleBase.WriteLine("[ModLoader] No mod DLLs found in Mods directory");
                return;
            }

            ConsoleBase.WriteLine($"[ModLoader] Found {dllFiles.Length} DLL file(s)");

            foreach (string dllPath in dllFiles)
            {
                LoadModFromDll(dllPath);
            }

            ConsoleBase.WriteLine($"[ModLoader] Successfully loaded {loadedMods.Count} mod(s)");
        }

        private void LoadModFromDll(string dllPath)
        {
            string fileName = Path.GetFileName(dllPath);

            try
            {
                ConsoleBase.WriteLine($"[ModLoader] Loading {fileName}...");

                // Load the assembly from bytes (not LoadFrom) so the original DLL in Mods/
                // n'est PAS verrouillée par le process. Sinon update/toggle/delete à chaud
                // échouent : Windows refuse de supprimer/déplacer un assembly chargé.
                byte[] rawAssembly = File.ReadAllBytes(dllPath);
                Assembly assembly = Assembly.Load(rawAssembly);

                // Find all types that implement IMod
                Type[] modTypes = assembly.GetTypes()
                    .Where(t => typeof(IMod).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                    .ToArray();

                if (modTypes.Length == 0)
                {
                    ConsoleBase.WriteLine($"[ModLoader] {fileName} does not contain any mod classes implementing IMod");
                    return;
                }

                // Load each mod found in the assembly
                foreach (Type modType in modTypes)
                {
                    LoadModInstance(modType, fileName);
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                ConsoleBase.WriteLine($"[ModLoader] Failed to load {fileName}: ReflectionTypeLoadException");
                foreach (Exception loaderEx in ex.LoaderExceptions)
                {
                    if (loaderEx != null)
                    {
                        ConsoleBase.WriteLine($"[ModLoader] Loader Exception: {loaderEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleBase.WriteLine($"[ModLoader] Failed to load {fileName}: {ex}");
            }
        }

        private void LoadModInstance(Type modType, string fileName)
        {
            try
            {
                // Create instance of the mod
                IMod modInstance = (IMod)Activator.CreateInstance(modType);

                // Call OnLoad
                modInstance.OnLoad();

                // Add to loaded mods list
                loadedMods.Add(modInstance);

                ConsoleBase.WriteLine($"[ModLoader] ✓ Loaded mod: {modInstance.ModName} v{modInstance.ModVersion} by {modInstance.ModAuthor}");
            }
            catch (Exception ex)
            {
                ConsoleBase.WriteLine($"[ModLoader] Failed to instantiate mod from {fileName} (Type: {modType.Name}): {ex}");
            }
        }

        /// <summary>
        /// Gets a loaded mod by name
        /// </summary>
        public IMod GetMod(string modName)
        {
            return loadedMods.FirstOrDefault(m => m.ModName.Equals(modName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Checks if a mod is loaded
        /// </summary>
        public bool IsModLoaded(string modName)
        {
            return GetMod(modName) != null;
        }
    }
}

