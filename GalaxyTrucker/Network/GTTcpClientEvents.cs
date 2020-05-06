using GalaxyTrucker.Model;
using System;

namespace GalaxyTrucker.Network
{
    /// <summary>
    /// Event args for signaling the start of the building stage
    /// </summary>
    public class BuildingBegunEventArgs : EventArgs
    {
        public BuildingBegunEventArgs() { }
    }

    /// <summary>
    /// Event args for signaling the end of the building stage
    /// </summary>
    public class BuildingEndedEventArgs : EventArgs
    {
        public BuildingEndedEventArgs() { }
    }

    /// <summary>
    /// Event args for signaling that another client picked a part
    /// </summary>
    public class PartTakenEventArgs : EventArgs
    {
        public int Row { get; set; }

        public int Column { get; set; }

        public PartTakenEventArgs(int row, int column)
        {
            Row = row;
            Column = column;
        }
    }

    /// <summary>
    /// Event args for signaling that another client put back a part
    /// </summary>
    public class PartPutBackEventArgs : EventArgs
    {
        public int Row { get; set; }

        public int Column { get; set; }

        public Part Part { get; set; }

        public PartPutBackEventArgs(int row, int column, Part part)
        {
            Row = row;
            Column = column;
            Part = part;
        }
    }

    /// <summary>
    /// Event args for signaling that another player toggled their ready state
    /// </summary>
    public class PlayerReadiedEventArgs : EventArgs
    {
        public PlayerColor Player { get; set; }

        public PlayerReadiedEventArgs(PlayerColor player) =>
            Player = player;
    }

    /// <summary>
    /// Event args for sending the response of this client picking a part
    /// </summary>
    public class PartPickedEventArgs : EventArgs
    {
        public Part Part { get; set; }

        public PartPickedEventArgs(Part part) =>
            Part = part;
    }

    /// <summary>
    /// Event args for signaling the start of the flight stage
    /// </summary>
    public class FlightBegunEventArgs : EventArgs
    {
        public FlightBegunEventArgs() { }
    }

    /// <summary>
    /// Event args for signaling that another player connected
    /// </summary>
    public class PlayerConnectedEventArgs : EventArgs
    {
        public string PlayerName { get; set; }

        public PlayerColor Color { get; set; }

        public PlayerConnectedEventArgs(string playerName, PlayerColor color)
        {
            PlayerName = playerName;
            Color = color;
        }
    }

    /// <summary>
    /// Event args for signaling that another player disconnected
    /// </summary>
    public class PlayerDisconnectedEventArgs : EventArgs
    {
        public PlayerColor Color { get; set; }

        public PlayerDisconnectedEventArgs(PlayerColor color)
        {
            Color = color;
        }
    }
}
