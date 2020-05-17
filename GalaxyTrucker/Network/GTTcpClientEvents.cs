using GalaxyTrucker.Model;
using System;
using System.Collections.Generic;

namespace GalaxyTrucker.Network
{
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
    /// Event args to carry information regarding one player
    /// </summary>
    public class PlayerEventArgs : EventArgs
    {
        public PlayerColor Player { get; set; }

        public PlayerEventArgs(PlayerColor player) =>
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
    /// Event args to carry player - cash value pairs
    /// </summary>
    public class EndResultEventArgs : EventArgs
    {
        public List<(PlayerColor, int)> Results { get; set; }

        public EndResultEventArgs(List<(PlayerColor, int)> results)
        {
            Results = results;
        }
    }

}
