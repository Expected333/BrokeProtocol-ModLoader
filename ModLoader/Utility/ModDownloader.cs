using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Networking;

namespace ModLoader.Utility
{
    public class DownloadResult
    {
        public bool Success;
        public string Error;
        public string InstalledPath;
    }

    public static class ModDownloader
    {
        /// <summary>
        /// Télécharge le DLL d'un RemoteMod, vérifie le SHA256, et l'installe dans Mods/.
        /// Coroutine — l'appelant doit la lancer via StartCoroutine.
        /// </summary>
        public static IEnumerator Install(RemoteMod mod, Action<float> onProgress, Action<DownloadResult> onComplete)
        {
            if (mod == null)
            {
                onComplete?.Invoke(new DownloadResult { Success = false, Error = "Mod null" });
                yield break;
            }
            if (string.IsNullOrEmpty(mod.DownloadUrl))
            {
                onComplete?.Invoke(new DownloadResult { Success = false, Error = "URL de téléchargement manquante" });
                yield break;
            }
            if (string.IsNullOrEmpty(mod.Sha256) || mod.Sha256.Length != 64)
            {
                onComplete?.Invoke(new DownloadResult { Success = false, Error = "SHA256 manquant ou invalide dans l'index" });
                yield break;
            }

            ModInspector.EnsureDirectory();

            // On télécharge en .tmp puis on rename pour éviter d'installer un DLL partiel
            // si l'utilisateur quitte le jeu pendant le download.
            string fileName = mod.Name + ".dll";
            string destPath = Path.Combine(ModInspector.ModsDirectory, fileName);
            string tmpPath = destPath + ".downloading";

            using (UnityWebRequest req = UnityWebRequest.Get(mod.DownloadUrl))
            {
                req.timeout = 120;
                req.SetRequestHeader("User-Agent", "ModLoader/" + Core.Version);
                req.SetRequestHeader("Accept", "application/octet-stream");
                req.downloadHandler = new DownloadHandlerFile(tmpPath) { removeFileOnAbort = true };

                var op = req.SendWebRequest();
                while (!op.isDone)
                {
                    onProgress?.Invoke(req.downloadProgress);
                    yield return null;
                }

                bool error = req.result != UnityWebRequest.Result.Success;

                if (error)
                {
                    SafeDelete(tmpPath);
                    onComplete?.Invoke(new DownloadResult
                    {
                        Success = false,
                        Error = "HTTP " + req.responseCode + " : " + req.error
                    });
                    yield break;
                }
            }

            onProgress?.Invoke(1f);

            // Vérification SHA256
            string actualHash;
            try
            {
                actualHash = ComputeSha256(tmpPath);
            }
            catch (Exception ex)
            {
                SafeDelete(tmpPath);
                onComplete?.Invoke(new DownloadResult
                {
                    Success = false,
                    Error = "Erreur calcul SHA256 : " + ex.Message
                });
                yield break;
            }

            if (!string.Equals(actualHash, mod.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                SafeDelete(tmpPath);
                onComplete?.Invoke(new DownloadResult
                {
                    Success = false,
                    Error = "Hash SHA256 invalide. Attendu " + mod.Sha256 + ", obtenu " + actualHash + ". DLL rejeté."
                });
                yield break;
            }

            // Tout OK → on remplace
            try
            {
                if (File.Exists(destPath)) File.Delete(destPath);
                File.Move(tmpPath, destPath);
            }
            catch (Exception ex)
            {
                SafeDelete(tmpPath);
                onComplete?.Invoke(new DownloadResult
                {
                    Success = false,
                    Error = "Impossible d'écrire dans Mods/ : " + ex.Message
                });
                yield break;
            }

            onComplete?.Invoke(new DownloadResult
            {
                Success = true,
                InstalledPath = destPath
            });
        }

        private static string ComputeSha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(stream);
                var sb = new System.Text.StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++) sb.Append(hash[i].ToString("x2"));
                return sb.ToString();
            }
        }

        private static void SafeDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { /* ignore */ }
        }
    }
}
