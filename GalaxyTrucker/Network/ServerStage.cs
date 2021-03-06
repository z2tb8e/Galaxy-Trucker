﻿namespace GalaxyTrucker.Network
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
        /// Finished building, not yet flight
        /// </summary>
        PastBuild = 2,
        /// <summary>
        /// Past building flight phase, not accepting connections
        /// </summary>
        Flight = 3,
        /// <summary>
        /// Past flight, deducting winner
        /// </summary>
        PastFlight = 4

    }
}