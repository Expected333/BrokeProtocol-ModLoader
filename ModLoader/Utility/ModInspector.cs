using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ModLoader.Utility
{
    public enum ModState
    {
        Active,
        Disabled,
        Failed
    }

    public class ModEntry
    {
        public string FilePath;
        public string FileName;
        public string Name;
        public string Version;
        public string Author;
        public ModState State;
        public bool LoadedInSession;
        public string Error;

        public bool IsKnownMod => !string.IsNullOrEmpty(Name);
    }

    /// <summary>
    /// Inspecte les fichiers du dossier Mods/ sans engager Harmony / OnLoad.
    /// Utilise Assembly.LoadFrom + réflexion pour extraire les métadonnées IMod.
    /// </summary>
    public static class ModInspector
    {
        public const string DisabledSuffix = ".disabled";

        public static string ModsDirectory =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Mods"));

        public static List<ModEntry> Scan()
        {
            EnsureDirectory();
            var entries = new List<ModEntry>();

            var files = Directory.GetFiles(ModsDirectory, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f =>
                    f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith(".dll" + DisabledSuffix, StringComparison.OrdinalIgnoreCase));

            ModManager loadedManager = Core.GetModManager();

            foreach (var path in files)
            {
                var entry = InspectFile(path);

                if (entry.State == ModState.Active && loadedManager != null && entry.IsKnownMod)
                {
                    entry.LoadedInSession = loadedManager.IsModLoaded(entry.Name);
                }

                entries.Add(entry);
            }

            return entries
                .OrderBy(e => e.State)
                .ThenBy(e => e.Name ?? e.FileName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static ModEntry InspectFile(string path)
        {
            var entry = new ModEntry
            {
                FilePath = path,
                FileName = Path.GetFileName(path),
                State = path.EndsWith(DisabledSuffix, StringComparison.OrdinalIgnoreCase)
                    ? ModState.Disabled
                    : ModState.Active
            };

            try
            {
                var info = ProbeMetadata(path);
                if (info != null)
                {
                    entry.Name = info.Name;
                    entry.Version = info.Version;
                    entry.Author = info.Author;
                }
            }
            catch (Exception ex)
            {
                entry.State = ModState.Failed;
                entry.Error = ex.Message;
            }

            if (string.IsNullOrEmpty(entry.Name))
            {
                entry.Name = Path.GetFileNameWithoutExtension(entry.FileName).Replace(".dll", string.Empty);
            }

            return entry;
        }

        private class ModMetadata
        {
            public string Name;
            public string Version;
            public string Author;
        }

        // Les mods chargés au démarrage du jeu sont déjà dans AppDomain.
        // On essaie d'abord de retrouver le type sans recharger l'assembly.
        private static ModMetadata ProbeMetadata(string dllPath)
        {
            string fileNameNoExt = Path.GetFileNameWithoutExtension(dllPath).Replace(".dll", string.Empty);

            // 1) Si déjà chargé dans le domaine, lire depuis là (ne fonctionne que pour les .dll actifs)
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (string.Equals(asm.GetName().Name, fileNameNoExt, StringComparison.OrdinalIgnoreCase))
                    {
                        var meta = ExtractFromAssembly(asm);
                        if (meta != null) return meta;
                    }
                }
                catch { /* asm dynamique non inspectable */ }
            }

            // 2) Sinon (mod désactivé OU installé mid-session) : on passe TOUJOURS par
            // une copie temporaire. Ça évite que Assembly.LoadFrom verrouille le .dll
            // original et bloque les opérations ultérieures de Toggle/Delete.
            string tempPath = null;
            try
            {
                tempPath = Path.Combine(Path.GetTempPath(),
                    Guid.NewGuid().ToString("N") + "_" + fileNameNoExt + ".dll");
                File.Copy(dllPath, tempPath, overwrite: true);

                var asm = Assembly.LoadFrom(tempPath);
                return ExtractFromAssembly(asm);
            }
            finally
            {
                // La copie reste verrouillée par le process tant que l'assembly est chargée ;
                // File.Delete va simplement échouer silencieusement, Windows nettoiera %TEMP%.
                if (tempPath != null)
                {
                    try { File.Delete(tempPath); } catch { /* attendu : verrou */ }
                }
            }
        }

        private static ModMetadata ExtractFromAssembly(Assembly asm)
        {
            Type[] types;
            try { types = asm.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).ToArray(); }

            var modType = types.FirstOrDefault(t =>
                t != null &&
                !t.IsAbstract &&
                !t.IsInterface &&
                typeof(IMod).IsAssignableFrom(t));

            if (modType == null) return null;

            try
            {
                var instance = (IMod)Activator.CreateInstance(modType);
                return new ModMetadata
                {
                    Name = instance.ModName,
                    Version = instance.ModVersion,
                    Author = instance.ModAuthor
                };
            }
            catch
            {
                // Fallback : essayer les propriétés statiquement (peu probable mais sûr)
                return new ModMetadata { Name = modType.Name };
            }
        }

        public static void EnsureDirectory()
        {
            if (!Directory.Exists(ModsDirectory))
            {
                Directory.CreateDirectory(ModsDirectory);
            }
        }

        public static bool Toggle(ModEntry entry, out string message)
        {
            try
            {
                if (entry.State == ModState.Active)
                {
                    string newPath = entry.FilePath + DisabledSuffix;
                    if (File.Exists(newPath)) File.Delete(newPath);
                    File.Move(entry.FilePath, newPath);
                    entry.FilePath = newPath;
                    entry.FileName = Path.GetFileName(newPath);
                    entry.State = ModState.Disabled;
                    message = "Mod désactivé. Redémarrage requis pour appliquer.";
                    return true;
                }
                if (entry.State == ModState.Disabled)
                {
                    string newPath = entry.FilePath.Substring(0, entry.FilePath.Length - DisabledSuffix.Length);
                    if (File.Exists(newPath)) File.Delete(newPath);
                    File.Move(entry.FilePath, newPath);
                    entry.FilePath = newPath;
                    entry.FileName = Path.GetFileName(newPath);
                    entry.State = ModState.Active;
                    message = "Mod activé. Redémarrage requis pour appliquer.";
                    return true;
                }
                message = "État inconnu, action ignorée.";
                return false;
            }
            catch (Exception ex)
            {
                message = "Erreur : " + ex.Message;
                return false;
            }
        }

        public static bool Delete(ModEntry entry, out string message)
        {
            try
            {
                File.Delete(entry.FilePath);
                message = "Mod supprimé. Redémarrage recommandé.";
                return true;
            }
            catch (Exception ex)
            {
                message = "Erreur : " + ex.Message;
                return false;
            }
        }

        public static bool Install(string sourceDllPath, out string message)
        {
            try
            {
                if (!File.Exists(sourceDllPath))
                {
                    message = "Fichier introuvable.";
                    return false;
                }
                if (!sourceDllPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    message = "Le fichier doit être un .dll.";
                    return false;
                }

                EnsureDirectory();
                string dest = Path.Combine(ModsDirectory, Path.GetFileName(sourceDllPath));
                File.Copy(sourceDllPath, dest, overwrite: true);
                message = "Mod installé : " + Path.GetFileName(dest) + ". Redémarrage requis.";
                return true;
            }
            catch (Exception ex)
            {
                message = "Erreur : " + ex.Message;
                return false;
            }
        }
    }
}
