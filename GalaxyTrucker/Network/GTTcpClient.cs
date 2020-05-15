using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using GalaxyTrucker.Model;

namespace GalaxyTrucker.Network
{
    /// <summary>
    /// Exception thrown when a server message indicates that the client is out of sync with it.
    /// </summary>
    public class OutOfSyncException : Exception { }

    public class ConnectionRefusedException : Exception { }

    public class UnknownMessageException : Exception
    {
        public UnknownMessageException(string s) : base(s) { }
    }

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

        private const double PingInterval = 500;

        private readonly TcpClient _client;
        private readonly Timer _pingTimer;

        private ServerStage _serverStage;
        private NetworkStream _stream;

        #endregion

        #region properties

        public bool IsConnected => _client.Connected;

        public string DisplayName { get; private set; }

        public PlayerColor Player { get; private set; }

        public GameStage GameStage { get; private set; }

        public PlayerOrderManager OrderManager { get; private set; }

        public bool IsReady { get; private set; }

        public Dictionary<PlayerColor, PlayerInfo> PlayerInfos { get; }

        public List<PlayerColor> PlayerOrder { get; }

        #endregion

        #region events

        /// <summary>
        /// Event raised when the building stage starts
        /// </summary>
        public event EventHandler BuildingBegun;

        /// <summary>
        /// Event raised after everybody finished building
        /// </summary>
        public event EventHandler BuildingEnded;

        /// <summary>
        /// Event raised when the flight stage starts
        /// </summary>
        public event EventHandler FlightBegun;

        /// <summary>
        /// Event raised when this client receives the answer for picking a part
        /// </summary>
        public event EventHandler<PartPickedEventArgs> PartPicked;

        /// <summary>
        /// Event raised when another client picks a part
        /// </summary>
        public event EventHandler<PartTakenEventArgs> PartTaken;

        /// <summary>
        /// Event raised when when another client puts back a part
        /// </summary>
        public event EventHandler<PartPutBackEventArgs> PartPutBack;

        /// <summary>
        /// Event raised when another player toggles their ready state
        /// </summary>
        public event EventHandler<PlayerReadiedEventArgs> PlayerReadied;

        /// <summary>
        /// Event raised when this client toggles their ready state
        /// </summary>
        public event EventHandler ThisPlayerReadied;

        /// <summary>
        /// Event raised when another player connects to the server
        /// </summary>
        public event EventHandler<PlayerConnectedEventArgs> PlayerConnected;

        /// <summary>
        /// Event raised when another client disconnects from the server
        /// </summary>
        public event EventHandler<PlayerDisconnectedEventArgs> PlayerDisconnected;

        /// <summary>
        /// Event raised when this client disconnects from the server
        /// </summary>
        public event EventHandler ThisPlayerDisconnected;

        #endregion

        public GTTcpClient()
        {
            PlayerInfos = new Dictionary<PlayerColor, PlayerInfo>();
            PlayerOrder = new List<PlayerColor>();
            IsReady = false;
            _serverStage = ServerStage.Lobby;
            _client = new TcpClient();
            _pingTimer = new Timer(PingInterval);
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

                string colorAndGameStageMessage = ReadMessageFromServer();
                if (colorAndGameStageMessage == "Connection refused")
                {
                    _client.Close();
                    throw new ConnectionRefusedException();
                }
                WriteMessageToServer(DisplayName);

                string[] colorAndGameStage = colorAndGameStageMessage.Split(',');
                Player = Enum.Parse<PlayerColor>(colorAndGameStage[0]);
                GameStage = Enum.Parse<GameStage>(colorAndGameStage[1]);    

                Console.WriteLine("Assigned color: {0}", Player);
                //own client's info
                PlayerInfos[Player] = new PlayerInfo(Player, DisplayName, false);

                string otherPlayerInfo = ReadMessageFromServer();
                string[] parts = otherPlayerInfo.Split(',');
                int otherPlayerCount = int.Parse(parts[0]);
                for (int i = 0; i < otherPlayerCount; ++i)
                {
                    PlayerColor index = Enum.Parse<PlayerColor>(parts[1 + i * 3]);
                    PlayerInfos[index] = new PlayerInfo(index, parts[2 + i * 3], bool.Parse(parts[3 + i * 3]));
                }

                _ = Task.Factory.StartNew(() => HandleServerMessages(), TaskCreationOptions.LongRunning);
                _pingTimer.Elapsed += PingTimer_Elapsed;
                _pingTimer.Start();

            }
            catch (Exception)
            {
                throw;
            }
        }

        public void ToggleReady(ServerStage currentStage)
        {
            if(_serverStage != currentStage)
            {
                _pingTimer.Stop();
                throw new InvalidOperationException();
            }

            WriteMessageToServer("ToggleReady");

            IsReady = !IsReady;
            PlayerInfos[Player].IsReady = IsReady;
            ThisPlayerReadied?.Invoke(this, EventArgs.Empty);
        }

        public void StartFlightStage(int firepower, int enginepower, int crewCount, int storageSize, int batteries)
        {
            if(_serverStage != ServerStage.Build || IsReady)
            {
                _pingTimer.Stop();
                throw new InvalidOperationException();
            }

            ToggleReady(ServerStage.Build);
            WriteMessageToServer($"StartFlightStage,{firepower},{enginepower},{crewCount},{storageSize},{batteries}");
        }

        public void PutBackPart(int row, int column)
        {
            if(_serverStage != ServerStage.Build)
            {
                _pingTimer.Stop();
                throw new InvalidOperationException();
            }

            WriteMessageToServer($"PutBackPart,{row},{column}");
        }

        public void PickPart(int row, int column)
        {
            if (_serverStage != ServerStage.Build)
            {
                _pingTimer.Stop();
                throw new InvalidOperationException();
            }

            WriteMessageToServer($"PickPart,{row},{column}");
        }

        public void Close()
        {
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
                message = ReadMessageFromServer();
                //Disconnected
                if(message == null)
                {
                    return;
                }
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
                        PlayerToggledReadyResolve(parts);
                        break;

                    case "PlayerDisconnect":
                        PlayerDisconnectResolve(parts);
                        break;

                    case "Ping":
                        break;

                    default:
                        throw new UnknownMessageException(message);
                }
            }
        }

        private void PingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            WriteMessageToServer("Ping");
        }

        private void PlayerDisconnectResolve(string[] parts)
        {
            PlayerColor disconnectedPlayer = Enum.Parse<PlayerColor>(parts[1]);
            PlayerInfos.Remove(disconnectedPlayer);
            if (PlayerOrder != null)
            {
                PlayerOrder.Remove(disconnectedPlayer);
            }
            PlayerDisconnected?.Invoke(this, new PlayerDisconnectedEventArgs(disconnectedPlayer));
        }

        private void PlayerConnectedResolve(string[] parts)
        {
            if (_serverStage != ServerStage.Lobby)
            {
                _pingTimer.Stop();
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
                _pingTimer.Stop();
                throw new OutOfSyncException();
            }
            IsReady = false;
            foreach (PlayerInfo info in PlayerInfos.Values)
            {
                info.IsReady = false;
            }
            _serverStage = ServerStage.Flight;
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
            FlightBegun?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Method called when the server sends a message to signal that the building stage started
        /// </summary>
        /// <param name="parts"></param>
        private void BuildingBegunResolve()
        {
            if (!IsReady)
            {
                _pingTimer.Stop();
                throw new OutOfSyncException();
            }
            IsReady = false;
            foreach(PlayerInfo info in PlayerInfos.Values)
            {
                info.IsReady = false;
            }
            _serverStage = ServerStage.Build;
            BuildingBegun?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Method called when the server sends a message to signal that the building stage ended
        /// </summary>
        /// <param name="parts"></param>
        private void BuildingEndedResolve(string[] parts)
        {
            if (!IsReady)
            {
                _pingTimer.Stop();
                throw new OutOfSyncException();
            }
            IsReady = false;
            foreach (PlayerInfo info in PlayerInfos.Values)
            {
                info.IsReady = false;
            }
            for (int i = 1; i < parts.Length; ++i)
            {
                PlayerOrder.Add(Enum.Parse<PlayerColor>(parts[i]));
            }
            OrderManager = new PlayerOrderManager(PlayerOrder, GameStage);
            BuildingEnded?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Method called when the server sends a message to tell the result the client picking a part earlier
        /// </summary>
        /// <param name="parts"></param>
        private void PickPartResultResolve(string[] parts)
        {
            if (_serverStage != ServerStage.Build)
            {
                _pingTimer.Stop();
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
            if (_serverStage != ServerStage.Build)
            {
                _pingTimer.Stop();
                throw new OutOfSyncException();
            }
        }

        /// <summary>
        /// Method called when the server sends a message to signal that another player put back a part
        /// </summary>
        /// <param name="parts"></param>
        private void PartPutBackResolve(string[] parts)
        {
            if (_serverStage != ServerStage.Build)
            {
                _pingTimer.Stop();
                throw new OutOfSyncException();
            }
            int row = int.Parse(parts[1]);
            int column = int.Parse(parts[2]);
            Part putBack = parts[3].ToPart();
            PartPutBack?.Invoke(this, new PartPutBackEventArgs(row, column, putBack));
        }

        /// <summary>
        /// Method called when the server sends a message to signal that another player took a part
        /// </summary>
        /// <param name="parts"></param>
        private void PartTakenResolve(string[] parts)
        {
            if (_serverStage != ServerStage.Build)
            {
                _pingTimer.Stop();
                throw new OutOfSyncException();
            }
            int row = int.Parse(parts[1]);
            int column = int.Parse(parts[2]);
            PartTaken?.Invoke(this, new PartTakenEventArgs(row, column));
        }

        /// <summary>
        /// Method called when the server sends a message signaling that another player toggled their ready state 
        /// </summary>
        /// <param name="parts"></param>
        private void PlayerToggledReadyResolve(string[] parts)
        {
            PlayerColor player = Enum.Parse<PlayerColor>(parts[1]);
            PlayerInfos[player].IsReady = !PlayerInfos[player].IsReady;
            PlayerReadied?.Invoke(this, new PlayerReadiedEventArgs(player));
        }

        private string ReadMessageFromServer()
        {
            try
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
            catch (IOException) {
                _pingTimer.Stop();
                ThisPlayerDisconnected?.Invoke(this, EventArgs.Empty);
                return null;
            }
        }

        private void WriteMessageToServer(string message)
        {
            try
            {
                byte[] msg = Encoding.ASCII.GetBytes($"{message}#");
                _stream.Write(msg, 0, msg.Length);
            }
            catch (IOException)
            {
                _pingTimer.Stop();
                ThisPlayerDisconnected?.Invoke(this, EventArgs.Empty);
            }
            catch (ObjectDisposedException)
            {

            }
        }

        #endregion
    }
}
