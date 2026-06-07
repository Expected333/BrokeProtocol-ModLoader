using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace ModLoader.Utility
{
    /// <summary>
    /// Représentation d'un mod tel qu'il apparaît dans l'index agrégé du registry.
    /// </summary>
    [Serializable]
    public class RemoteMod
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("version")] public string Version;
        [JsonProperty("author")] public string Author;
        [JsonProperty("description")] public string Description;
        [JsonProperty("repo")] public string Repo;
        [JsonProperty("homepage")] public string Homepage;
        [JsonProperty("download_url")] public string DownloadUrl;
        [JsonProperty("sha256")] public string Sha256;
        [JsonProperty("size_bytes")] public long SizeBytes;
        [JsonProperty("published_at")] public string PublishedAt;
        [JsonProperty("stars")] public int Stars;
        [JsonProperty("dependencies")] public List<string> Dependencies = new List<string>();
        [JsonProperty("min_modloader_version")] public string MinModLoaderVersion;
        [JsonProperty("tags")] public List<string> Tags = new List<string>();
    }

    [Serializable]
    public class RegistryIndex
    {
        [JsonProperty("schema_version")] public int SchemaVersion;
        [JsonProperty("generated_at")] public string GeneratedAt;
        [JsonProperty("mods")] public List<RemoteMod> Mods = new List<RemoteMod>();
    }

    public class RegistryFetchResult
    {
        public bool Success;
        public RegistryIndex Index;
        public string Error;
    }

    /// <summary>
    /// Couche d'accès au registry distant.
    ///
    /// L'URL est résolue dans l'ordre :
    /// 1. Variable d'environnement MODLOADER_REGISTRY_URL
    /// 2. Fichier [GameDir]/Mods/registry-url.txt (première ligne non vide)
    /// 3. DefaultRegistryUrl ci-dessous
    /// </summary>
    public static class ModRegistry
    {
        /// <summary>
        /// URL par défaut de l'index. À mettre à jour avec le username GitHub du projet.
        /// Forme attendue : https://{user}.github.io/BP-Mods-Registry/index.json
        /// </summary>
        public const string DefaultRegistryUrl =
            "https://expected333.github.io/BP-Mods-Registry/index.json";

        public static string ResolveRegistryUrl()
        {
            try
            {
                string env = Environment.GetEnvironmentVariable("MODLOADER_REGISTRY_URL");
                if (!string.IsNullOrWhiteSpace(env)) return env.Trim();
            }
            catch { /* ignore */ }

            try
            {
                string configPath = System.IO.Path.Combine(ModInspector.ModsDirectory, "registry-url.txt");
                if (System.IO.File.Exists(configPath))
                {
                    foreach (var line in System.IO.File.ReadAllLines(configPath))
                    {
                        string trimmed = line.Trim();
                        if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("#"))
                            return trimmed;
                    }
                }
            }
            catch { /* ignore */ }

            return DefaultRegistryUrl;
        }

        /// <summary>
        /// Coroutine de fetch (UnityWebRequest est asynchrone).
        /// L'appelant doit la lancer via un MonoBehaviour StartCoroutine.
        /// </summary>
        public static IEnumerator Fetch(Action<RegistryFetchResult> callback)
        {
            string url = ResolveRegistryUrl();

            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                req.timeout = 15;
                req.SetRequestHeader("User-Agent", "ModLoader/" + Core.Version);
                req.SetRequestHeader("Accept", "application/json");

                yield return req.SendWebRequest();

                bool error = req.result != UnityWebRequest.Result.Success;

                if (error)
                {
                    callback?.Invoke(new RegistryFetchResult
                    {
                        Success = false,
                        Error = "HTTP " + req.responseCode + " : " + req.error
                    });
                    yield break;
                }

                RegistryIndex index;
                try
                {
                    index = JsonConvert.DeserializeObject<RegistryIndex>(req.downloadHandler.text);
                }
                catch (Exception ex)
                {
                    callback?.Invoke(new RegistryFetchResult
                    {
                        Success = false,
                        Error = "JSON invalide : " + ex.Message
                    });
                    yield break;
                }

                if (index == null)
                {
                    callback?.Invoke(new RegistryFetchResult
                    {
                        Success = false,
                        Error = "Index null après parsing"
                    });
                    yield break;
                }

                if (index.SchemaVersion != 1)
                {
                    callback?.Invoke(new RegistryFetchResult
                    {
                        Success = false,
                        Error = "Schema version non supportée : " + index.SchemaVersion
                    });
                    yield break;
                }

                callback?.Invoke(new RegistryFetchResult
                {
                    Success = true,
                    Index = index
                });
            }
        }
    }
}
