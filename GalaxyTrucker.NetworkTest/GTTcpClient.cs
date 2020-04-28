﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GalaxyTrucker.Model;

namespace GalaxyTrucker.NetworkTest
{
    /// <summary>
    /// Exception thrown when a server message indicates that the client is out of sync with it.
    /// </summary>
    public class OutOfSyncException : Exception { }

    public class ConnectionRefusedException : Exception { }

    public class PlayerInfo
    {
        public string Name { get; }

        public PlayerColor Color { get; }

        public PlayerAttributes Attributes { get; set; }

        public bool IsReady { get; set; }

        public bool IsFlying { get; set; }

        public PlayerInfo(PlayerColor color, string name, bool isReady)
        {
            Color = color;
            Name = name;
            IsReady = isReady;
            IsFlying = true;
        }
    }

    public class GTTcpClient
    {

        #region fields

        private readonly TcpClient _client;

        private ServerStage _stage;
        private NetworkStream _stream;

        #endregion

        #region properties

        public bool IsConnected => _client.Connected;

        public string DisplayName { get; private set; }

        public PlayerColor Player { get; private set; }

        public bool IsReady { get; private set; }

        public Dictionary<PlayerColor, PlayerInfo> PlayerInfos { get; }
        public List<PlayerColor> PlayerOrder { get; }

        #endregion

        #region events

        public event EventHandler<BuildingBegunEventArgs> BuildingBegun;

        public event EventHandler<BuildingEndedEventArgs> BuildingEnded;

        public event EventHandler<FlightBegunEventArgs> FlightBegun;

        public event EventHandler<PartPickedEventArgs> PartPicked;

        public event EventHandler<PartTakenEventArgs> PartTaken;

        public event EventHandler<PartPutBackEventArgs> PartPutBack;

        public event EventHandler<PlayerReadiedEventArgs> PlayerReadied;

        public event EventHandler<PlayerConnectedEventArgs> PlayerConnected;

        #endregion

        public GTTcpClient()
        {
            PlayerInfos = new Dictionary<PlayerColor, PlayerInfo>();
            PlayerOrder = new List<PlayerColor>();
            IsReady = false;
            _stage = ServerStage.Lobby;
            _client = new TcpClient();
        }

        #region public methods

        public async Task Connect(IPEndPoint endPoint, string displayName)
        {
            if (displayName.Contains('#'))
            {
                throw new FormatException("Argument \"displayName\" contains illegal '#' character.");
            }
            try
            {
                DisplayName = displayName;

                await _client.ConnectAsync(endPoint.Address, endPoint.Port);

                _stream = _client.GetStream();

                string color = ReadMessageFromServer();
                if(color == "Connection refused")
                {
                    _client.Close();
                    throw new ConnectionRefusedException();
                }
                WriteMessageToServer(DisplayName);

                Player = Enum.Parse<PlayerColor>(color);
                Console.WriteLine("Assigned color: {0}", Player);
                //own client's info
                PlayerInfos[Player] = new PlayerInfo(Player, DisplayName, false);

                string otherPlayerInfo = ReadMessageFromServer();
                string[] parts = otherPlayerInfo.Split(',');
                int otherPlayerCount = int.Parse(parts[0]);
                for(int i = 0; i < otherPlayerCount; ++i)
                {
                    PlayerColor index = Enum.Parse<PlayerColor>(parts[1 + i * 3]);
                    PlayerInfos[index] = new PlayerInfo(index, parts[2 + i * 3], bool.Parse(parts[3 + i * 3]));
                }

                _ = Task.Factory.StartNew(() => HandleServerMessages(), TaskCreationOptions.LongRunning);

            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine("ArgumentNullException: {0}", e);
            }
            catch(ArgumentException e)
            {
                Console.WriteLine("ArgumentException: {0}", e);
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
        }

        public void ToggleReady(ServerStage currentStage)
        {
            if(_stage != currentStage)
            {
                throw new InvalidOperationException();
            }

            WriteMessageToServer("ToggleReady");

            IsReady = !IsReady;
            PlayerInfos[Player].IsReady = IsReady;
        }

        public void StartFlightStage(int firepower, int enginepower, int crewCount, int storageSize, int batteries)
        {
            if(_stage != ServerStage.Build || IsReady)
            {
                throw new InvalidOperationException();
            }

            WriteMessageToServer($"StartFlightStage,{firepower},{enginepower},{crewCount},{storageSize},{batteries}");
            IsReady = true;
        }

        public void PutBackPart(int ind1, int ind2)
        {
            if(_stage != ServerStage.Build)
            {
                throw new InvalidOperationException();
            }

            WriteMessageToServer($"PutBackPart,{ind1},{ind2}");
        }

        public void PickPart(int ind1, int ind2)
        {
            if (_stage != ServerStage.Build)
            {
                throw new InvalidOperationException();
            }

            WriteMessageToServer($"PickPart,{ind1},{ind2}");
        }

        public void Close()
        {
            _stream.Close();
            _client.Close();
        }

        #endregion

        #region private methods

        private void HandleServerMessages()
        {
            string message;
            string[] parts;
            while (_client.Connected)
            {
                if (_stream.DataAvailable)
                {
                    message = ReadMessageFromServer();
                    //Console.WriteLine("{0} received message from server: {1}.", _color, message);
                    parts = message.Split(',');
                    switch (parts[0])
                    {
                        case "PlayerConnected":
                            PlayerConnectedResolve(parts);
                            break;

                        case "FlightBegun":
                            FlightBegunResolve(parts);
                            break;

                        case "BuildingBegun":
                            BuildingBegunResolve();
                            break;

                        case "BuildingEnded":
                            BuildingEndedResolve(parts);
                            break;

                        case "PickPartResult":
                            PickPartResultResolve(parts);
                            break;

                        case "ToggleReadyConfirm":
                            break;

                        case "PutBackPartConfirm":
                            PutBackPartResolve();
                            break;

                        case "PartPutBack":
                            PartPutBackResolve(parts);
                            break;

                        case "PartTaken":
                            PartTakenResolve(parts);
                            break;

                        case "PlayerToggledReady":
                            PlayerToggledReady(parts);
                            break;

                        default:
                            Console.WriteLine("Unhandled server message: {0}", message);
                            break;
                    }
                }
            }
        }

        private void PlayerConnectedResolve(string[] parts)
        {
            if(_stage != ServerStage.Lobby)
            {
                throw new OutOfSyncException();
            }
            PlayerColor color = Enum.Parse<PlayerColor>(parts[1]);
            string name = parts[2];
            PlayerInfos[color] = new PlayerInfo(color, name, false);
            PlayerConnected?.Invoke(this, new PlayerConnectedEventArgs(name, color));
        }

        private void FlightBegunResolve(string[] parts)
        {
            if (!IsReady)
            {
                throw new OutOfSyncException();
            }
            IsReady = false;
            _stage = ServerStage.Flight;
            //Format: FlightBegun,PlayerNumber,Color,Firepower,Enginepower,CrewCount,StorageSize,Batteries
            int playerNumber = int.Parse(parts[1]);
            for (int i = 0; i < playerNumber; ++i)
            {
                PlayerColor currentPlayer = Enum.Parse<PlayerColor>(parts[2 + i * 6]);
                PlayerInfos[currentPlayer].Attributes = new PlayerAttributes
                {
                    Firepower = int.Parse(parts[3 + i * 6]),
                    Enginepower = int.Parse(parts[4 + i * 6]),
                    CrewCount = int.Parse(parts[5 + i * 6]),
                    StorageSize = int.Parse(parts[6 + i * 6]),
                    Batteries = int.Parse(parts[7 + i * 6]),
                };
            }
            FlightBegun?.Invoke(this, new FlightBegunEventArgs());
            Console.WriteLine("Client {0}: FlightBegun", Player);
        }

        /// <summary>
        /// Method called when the server sends a message to signal that the building stage started
        /// </summary>
        /// <param name="parts"></param>
        private void BuildingBegunResolve()
        {
            if (!IsReady)
            {
                throw new OutOfSyncException();
            }
            IsReady = false;
            _stage = ServerStage.Build;
            BuildingBegun?.Invoke(this, new BuildingBegunEventArgs());
        }

        /// <summary>
        /// Method called when the server sends a message to signal that the building stage ended
        /// </summary>
        /// <param name="parts"></param>
        private void BuildingEndedResolve(string[] parts)
        {
            if (!IsReady)
            {
                throw new OutOfSyncException();
            }
            IsReady = false;
            for (int i = 1; i < parts.Length; ++i)
            {
                PlayerOrder.Add(Enum.Parse<PlayerColor>(parts[i]));
            }
            BuildingEnded?.Invoke(this, new BuildingEndedEventArgs());
        }

        /// <summary>
        /// Method called when the server sends a message to tell the result the client picking a part earlier
        /// </summary>
        /// <param name="parts"></param>
        private void PickPartResultResolve(string[] parts)
        {
            if (_stage != ServerStage.Build)
            {
                throw new OutOfSyncException();
            }
            Part picked = null;
            if (parts[1] != "null")
            {
                picked = parts[1].ToPart();
            }
            PartPicked?.Invoke(this, new PartPickedEventArgs(picked));
        }

        /// <summary>
        /// Method called when the server sends a message to confirm that the client successfully took a part
        /// </summary>
        private void PutBackPartResolve()
        {
            if (_stage != ServerStage.Build)
            {
                throw new OutOfSyncException();
            }
        }

        /// <summary>
        /// Method called when the server sends a message to signal that another player put back a part
        /// </summary>
        /// <param name="parts"></param>
        private void PartPutBackResolve(string[] parts)
        {
            if (_stage != ServerStage.Build)
            {
                throw new OutOfSyncException();
            }
            int ind1 = int.Parse(parts[1]);
            int ind2 = int.Parse(parts[2]);
            Part putBack = parts[3].ToPart();
            PartPutBack?.Invoke(this, new PartPutBackEventArgs(ind1, ind2, putBack));
        }

        /// <summary>
        /// Method called when the server sends a message to signal that another player took a part
        /// </summary>
        /// <param name="parts"></param>
        private void PartTakenResolve(string[] parts)
        {
            if (_stage != ServerStage.Build)
            {
                throw new OutOfSyncException();
            }
            int ind1 = int.Parse(parts[1]);
            int ind2 = int.Parse(parts[2]);
            PartTaken?.Invoke(this, new PartTakenEventArgs(ind1, ind2));
        }

        /// <summary>
        /// Method called when the server sends a message signaling that another player toggled their ready state 
        /// </summary>
        /// <param name="parts"></param>
        private void PlayerToggledReady(string[] parts)
        {
            PlayerColor player = Enum.Parse<PlayerColor>(parts[1]);
            PlayerInfos[player].IsReady = !PlayerInfos[player].IsReady;
            PlayerReadied?.Invoke(this, new PlayerReadiedEventArgs(player));
        }

        private string ReadMessageFromServer()
        {
            StringBuilder message = new StringBuilder();
            int character = _stream.ReadByte();
            while ((char)character != '#')
            {
                message.Append((char)character);
                character = _stream.ReadByte();
            }
            return message.ToString();
        }

        private void WriteMessageToServer(string message)
        {
            if (!_stream.CanWrite)
            {
                throw new InvalidOperationException();
            }

            byte[] msg = Encoding.ASCII.GetBytes($"{message}#");
            _stream.Write(msg, 0, msg.Length);
        }

        #endregion
    }
}
