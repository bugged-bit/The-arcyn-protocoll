namespace ARCYN.Platform
{
    public interface IConfigPathProvider
    {
        /// <summary>
        /// Resolve existing config file path, null if none.
        /// </summary>
        string? ResolvePath();

        /// <summary>
        /// Path to create/write config, preferring user data directory, fallback to app base.
        /// </summary>
        string GetOrCreatePath();
    }
}