namespace ModLoader
{
    /// <summary>
    /// Interface that all mods must implement
    /// </summary>
    public interface IMod
    {
        /// <summary>
        /// The name of the mod
        /// </summary>
        string ModName { get; }

        /// <summary>
        /// The version of the mod (e.g., "1.0.0")
        /// </summary>
        string ModVersion { get; }

        /// <summary>
        /// The author of the mod
        /// </summary>
        string ModAuthor { get; }

        /// <summary>
        /// Called when the mod is loaded by ModLoader
        /// </summary>
        void OnLoad();
    }
}

