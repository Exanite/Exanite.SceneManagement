namespace Exanite.SceneManagement
{
    public static class SceneLoadMonitors
    {
        /// <summary>
        /// Ensures only one scene load operation happens at a time.
        /// </summary>
        public static UniTaskMonitor Load { get; } = new();

        /// <summary>
        /// Ensures only one scene activation operation happens at a time.
        /// </summary>
        public static UniTaskMonitor Activation { get; } = new();
    }
}
