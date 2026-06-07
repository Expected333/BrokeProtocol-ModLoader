using BrokeProtocol.Entities;
using BrokeProtocol.Managers;
using BrokeProtocol.Utility;
using ENet;
using UnityEngine;

namespace ModLoader.Helpers
{
    /// <summary>
    /// Helper class for general game operations
    /// </summary>
    public static class GameHelper
    {
        /// <summary>
        /// Get the local player instance
        /// </summary>
        public static ShPlayer GetLocalPlayer()
        {
            ClManager clManager = MonoBehaviourSingleton<ClManager>.Instance;
            return clManager?.myPlayer;
        }

        /// <summary>
        /// Get the ClManager instance
        /// </summary>
        public static ClManager GetClManager()
        {
            return MonoBehaviourSingleton<ClManager>.Instance;
        }

        /// <summary>
        /// Get the SceneManager instance
        /// </summary>
        public static SceneManager GetSceneManager()
        {
            return MonoBehaviourSingleton<SceneManager>.Instance;
        }

        /// <summary>
        /// Get the ShManager instance
        /// </summary>
        public static ShManager GetShManager()
        {
            return MonoBehaviourSingleton<ShManager>.Instance;
        }

        /// <summary>
        /// Check if the game has started (player is in game)
        /// </summary>
        public static bool IsGameStarted()
        {
            return SceneManager.gameStarted;
        }

        /// <summary>
        /// Check if the player is connected to a server
        /// </summary>
        public static bool IsConnected()
        {
            ClManager clManager = GetClManager();
            return clManager != null && clManager.myPlayer != null;
        }

        /// <summary>
        /// Get the game version
        /// </summary>
        public static string GetGameVersion()
        {
            ShManager shManager = GetShManager();
            return shManager?.Version;
        }

        /// <summary>
        /// Get an entity by ID
        /// </summary>
        public static ShEntity GetEntityByID(int entityID)
        {
            if (GetSceneManager().entityCollection.TryGetValue(entityID, out ShEntity entity))
            {
                return entity;
            }
            return null;
        }

        /// <summary>
        /// Get a player by ID
        /// </summary>
        public static ShPlayer GetPlayerByID(int playerID)
        {
            ShEntity entity = GetEntityByID(playerID);
            return entity as ShPlayer;
        }

        /// <summary>
        /// Send a chat message (can be used to send commands if they start with /)
        /// </summary>
        public static void SendChatMessage(string message)
        {
            ClManager clManager = GetClManager();
            ShPlayer player = GetLocalPlayer();
            if (clManager != null && player != null)
            {
                // Send as chat message - commands typically start with /
                clManager.SendToServer(PacketFlags.Reliable,
                    BrokeProtocol.Utility.Networking.SvPacket.ChatGlobal,
                    message);
            }
        }

        /// <summary>
        /// Log a message to the game console
        /// </summary>
        public static void LogToConsole(string message)
        {
            ConsoleBase.WriteLine($"[Mod] {message}");
        }

        /// <summary>
        /// Get the application data path
        /// </summary>
        public static string GetDataPath()
        {
            return Application.dataPath;
        }

        /// <summary>
        /// Get the persistent data path
        /// </summary>
        public static string GetPersistentDataPath()
        {
            return Application.persistentDataPath;
        }

        /// <summary>
        /// Check if the game is running in editor
        /// </summary>
        public static bool IsInEditor()
        {
            return Application.isEditor;
        }

        /// <summary>
        /// Get the current frame rate
        /// </summary>
        public static float GetFrameRate()
        {
            return 1f / Time.deltaTime;
        }
    }
}

