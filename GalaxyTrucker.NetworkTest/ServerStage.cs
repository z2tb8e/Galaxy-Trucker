namespace GalaxyTrucker.NetworkTest
{
    /// <summary>
    /// Enumeration for the different stages of the server
    /// </summary>
    public enum ServerStage
    {
        /// <summary>
        /// Accepting attempted connections
        /// </summary>
        Lobby = 0,
        /// <summary>
        /// Building phase, not accepting connections
        /// </summary>
        Build = 1,
        /// <summary>
        /// Past building flight phase, not accepting connections
        /// </summary>
        Flight = 2,
        /// <summary>
        /// Server is closing
        /// </summary>
        Close = 3

    }
}