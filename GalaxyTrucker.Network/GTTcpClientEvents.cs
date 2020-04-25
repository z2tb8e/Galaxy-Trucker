using GalaxyTrucker.Client.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace GalaxyTrucker.Network
{
    /// <summary>
    /// Event raised at the end of the lobby containing the attending players's colours
    /// </summary>
    public class BuildingBegunEventArgs : EventArgs
    {
        public IEnumerable<PlayerColor> Players { get; set; }

        public BuildingBegunEventArgs(IEnumerable<PlayerColor> players) =>
            Players = players;
    }

    /// <summary>
    /// Event raised at the end of the building stage containing the players's colours in the starting turn order
    /// </summary>
    public class BuildingEndedEventArgs : EventArgs
    {
        public IList<PlayerColor> PlayerOrder { get; set; }

        public BuildingEndedEventArgs(IList<PlayerColor> playerOrder) =>
            PlayerOrder = playerOrder;
    }

    /// <summary>
    /// Event raised when a player takes the part
    /// </summary>
    public class PartTakenEventArgs : EventArgs
    {
        public int Ind1 { get; set; }

        public int Ind2 { get; set; }

        public PartTakenEventArgs(int ind1, int ind2) =>
            (Ind1, Ind2) = (ind1, ind2);
    }

    /// <summary>
    /// Event raised when a different player puts back a part
    /// </summary>
    public class PartPutBackEventArgs : EventArgs
    {
        public int Ind1 { get; set; }

        public int Ind2 { get; set; }

        public Part Part { get; set; }

        public PartPutBackEventArgs(int ind1, int ind2, Part part) =>
            (Ind1, Ind2, Part) = (ind1, ind2, part);
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
    /// Event raised at the start of flight stage containing attributes about each players's ships
    /// </summary>
    public class FlightBegunEventArgs : EventArgs
    {
        public IDictionary<PlayerColor, PlayerAttributes> PlayerAttributes { get; set; }

        public FlightBegunEventArgs(IDictionary<PlayerColor, PlayerAttributes> playerAttributes) =>
            PlayerAttributes = playerAttributes;
    }
}
