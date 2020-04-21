namespace GalaxyTrucker.Network
{
    /// <summary>
    /// Enumeration for the different stages of the server
    /// </summary>
    public enum ServerStage
    {
        /// <summary>
        /// Accepting attempted connections
        /// </summary>
        Lobby,
        /// <summary>
        /// Building phase, not accepting connections
        /// </summary>
        Build,
        /// <summary>
        /// Past building flight phase, not accepting connections
        /// </summary>
        Flight,
        /// <summary>
        /// Server is closing
        /// </summary>
        Close

    }
}
