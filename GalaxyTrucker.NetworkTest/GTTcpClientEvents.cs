using GalaxyTrucker.Model;
using System;

namespace GalaxyTrucker.NetworkTest
{
    /// <summary>
    /// Event raised at the end of the lobby
    /// </summary>
    public class BuildingBegunEventArgs : EventArgs
    {
        public BuildingBegunEventArgs() { }
    }

    /// <summary>
    /// Event raised at the end of the building stage
    /// </summary>
    public class BuildingEndedEventArgs : EventArgs
    {
        public BuildingEndedEventArgs() { }
    }

    /// <summary>
    /// Event raised when a player takes the part
    /// </summary>
    public class PartTakenEventArgs : EventArgs
    {
        public int Ind1 { get; set; }

        public int Ind2 { get; set; }

        public PartTakenEventArgs(int ind1, int ind2)
        {
            Ind1 = ind1;
            Ind2 = ind2;
        }
    }

    /// <summary>
    /// Event raised when a different player puts back a part
    /// </summary>
    public class PartPutBackEventArgs : EventArgs
    {
        public int Ind1 { get; set; }

        public int Ind2 { get; set; }

        public Part Part { get; set; }

        public PartPutBackEventArgs(int ind1, int ind2, Part part)
        {
            Ind1 = ind1;
            Ind2 = ind2;
            Part = part;
        }
    }

    /// <summary>
    /// Event raised when a player toggles ready state in building, or finishes their part of the turn in flight
    /// </summary>
    public class PlayerReadiedEventArgs : EventArgs
    {
        public PlayerColor Player { get; set; }

        public PlayerReadiedEventArgs(PlayerColor player) =>
            Player = player;
    }

    /// <summary>
    /// Event raised to send the result of the client picking a part
    /// </summary>
    public class PartPickedEventArgs : EventArgs
    {
        public Part Part { get; set; }

        public PartPickedEventArgs(Part part) =>
            Part = part;
    }

    /// <summary>
    /// Event raised at the start of flight stage
    /// </summary>
    public class FlightBegunEventArgs : EventArgs
    {
        public FlightBegunEventArgs() { }
    }

    /// <summary>
    /// Event raised when another client joins during lobby
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
}
