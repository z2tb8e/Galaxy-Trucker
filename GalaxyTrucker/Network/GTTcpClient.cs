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
        private readonly List<PlayerColor> _playerOrder;

        private ServerStage _serverStage;
        private NetworkStream _stream;
        private PlayerOrderManager _orderManager;

        #endregion

        #region properties

        public bool IsConnected => _client.Connected;

        public string DisplayName { get; private set; }

        public PlayerColor Player { get; private set; }

        public GameStage GameStage { get; private set; }

        public bool IsReady { get; private set; }

        public Dictionary<PlayerColor, PlayerInfo> PlayerInfos { get; }

        public List<PlayerColor> PlayerOrder
        {
            get
            {
                return _orderManager?.GetOrder();
            }
        }

        public Dictionary<PlayerColor, PlaceProperty> PlaceProperties
        {
            get
            {
                return _orderManager?.Properties;
            }
        }

        public bool Crashed { get; private set; }

        public CardEvent Card { get; private set; }

        #endregion

        #region events

        public event EventHandler PlacesChanged;

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
        public event EventHandler<PlayerEventArgs> PlayerReadied;

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
        public event EventHandler<PlayerEventArgs> PlayerDisconnected;

        /// <summary>
        /// Event raised when this client disconnects from the server
        /// </summary>
        public event EventHandler ThisPlayerDisconnected;

        /// <summary>
        /// Event raised when the server sends a message that a card was picked
        /// </summary>
        public event EventHandler CardPicked;

        /// <summary>
        /// Event raised when the server sends a message that this player received the effects of the current card
        /// </summary>
        public event EventHandler PlayerTargeted;

        /// <summary>
        /// Event raised when the server sends a message that another player received the effects of the current card
        /// </summary>
        public event EventHandler<PlayerColor> OtherTargeted;

        /// <summary>
        /// Event raised when the server sends a message that another player crashed
        /// </summary>
        public event EventHandler<PlayerColor> PlayerCrashed;

        /// <summary>
        /// Event raised when the server sends a message that the current card has been played through
        /// </summary>
        public event EventHandler CardOver;

        /// <summary>
        /// Event raised when the server sends a message that an option from the current card is not available anymore
        /// </summary>
        public event EventHandler<int> OptionRemoved;

        /// <summary>
        /// Event raised when the server responses to and earlier message from this client attempting to pick an option
        /// </summary>
        public event EventHandler<int> OptionPicked;

        /// <summary>
        /// Event raised when the server sends a message that flight stage ended
        /// </summary>
        public event EventHandler FlightEnded;

        /// <summary>
        /// Event raised when the server sends a message about the final rankings based on cash, after which the client disconnects
        /// </summary>
        public event EventHandler<EndResultEventArgs> GameEnded;

        #endregion

        #region ctor

        public GTTcpClient()
        {
            PlayerInfos = new Dictionary<PlayerColor, PlayerInfo>();
            _playerOrder = new List<PlayerColor>();
            IsReady = false;
            _serverStage = ServerStage.Lobby;
            _client = new TcpClient();
            _pingTimer = new Timer(PingInterval);
            Crashed = false;
        }

        #endregion

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

                _ = Task.Factory.StartNew(() => HandleServerMessages(), TaskCreationOptions.LongRunning | TaskCreationOptions.RunContinuationsAsynchronously);
                _pingTimer.Elapsed += PingTimer_Elapsed;
                _pingTimer.Start();

            }
            catch (Exception)
            {
                throw;
            }
        }

        public void SendCashInfo(int cash)
        {
            if (_serverStage != ServerStage.PastFlight)
            {
                _pingTimer.Stop();
                _pingTimer.Dispose();
                throw new InvalidOperationException();
            }
            WriteMessageToServer($"CashInfo,{cash}");
        }

        public void ToggleReady(ServerStage currentStage)
        {
            if(_serverStage != currentStage)
            {
                _pingTimer.Stop();
                _pingTimer.Dispose();
                throw new InvalidOperationException();
            }

            WriteMessageToServer("ToggleReady");

            IsReady = !IsReady;
            PlayerInfos[Player].IsReady = IsReady;
            ThisPlayerReadied?.Invoke(this, EventArgs.Empty);
        }

        public void SendStardustInfo(int openConnectors)
        {
            if (_serverStage != ServerStage.Flight)
            {
                _pingTimer.Stop();
                _pingTimer.Dispose();
                throw new InvalidOperationException();
            }
            WriteMessageToServer($"StardustInfo,{openConnectors}");
        }

        public void UpdateAttributes(int firepower, int enginepower, int crewCount, int storageSize, int batteries)
        {
            if (_serverStage != ServerStage.Flight)
            {
                _pingTimer.Stop();
                _pingTimer.Dispose();
                throw new InvalidOperationException();
            }
            WriteMessageToServer($"AttributesUpdate,{firepower},{enginepower},{crewCount},{storageSize},{batteries}");
        }

        public void CrashPlayer()
        {
            if (_serverStage != ServerStage.Flight || Crashed)
            {
                _pingTimer.Stop();
                _pingTimer.Dispose();
                throw new InvalidOperationException();
            }
            Crashed = true;
            WriteMessageToServer("PlayerCrash");
        }

        public void SendCardOption(int option)
        {
            if (_serverStage != ServerStage.Flight || Crashed)
            {
                _pingTimer.Stop();
                _pingTimer.Dispose();
                throw new InvalidOperationException();
            }
            WriteMessageToServer($"CardOption,{option}");
        }

        public void StartFlightStage(int firepower, int enginepower, int crewCount, int storageSize, int batteries)
        {
            if(_serverStage != ServerStage.PastBuild || IsReady)
            {
                _pingTimer.Stop();
                _pingTimer.Dispose();
                throw new InvalidOperationException();
            }

            WriteMessageToServer($"StartFlightStage,{firepower},{enginepower},{crewCount},{storageSize},{batteries}");
            ToggleReady(ServerStage.PastBuild);
        }

        public void PutBackPart(int row, int column)
        {
            if(_serverStage != ServerStage.Build)
            {
                _pingTimer.Stop();
                _pingTimer.Dispose();
                throw new InvalidOperationException();
            }

            WriteMessageToServer($"PutBackPart,{row},{column}");
        }

        public void PickPart(int row, int column)
        {
            if (_serverStage != ServerStage.Build)
            {
                _pingTimer.Stop();
                _pingTimer.Dispose();
                throw new InvalidOperationException();
            }

            WriteMessageToServer($"PickPart,{row},{column}");
        }

        public void Close()
        {
            _pingTimer.Stop();
            _pingTimer.Dispose();
            _client?.Close();
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

                    case "CardPicked":
                        CardPickedResolve(parts);
                        break;

                    case "PlayerCrash":
                        PlayerCrashResolve(parts);
                        break;

                    case "TargetedPlayer":
                        TargetedPlayerResolve();
                        break;

                    case "OtherTarget":
                        OtherTargetResolve(parts);
                        break;

                    case "PlayerMoved":
                        PlayerMovedResolve(parts);
                        break;

                    case "CardOver":
                        CardOverResolve();
                        break;

                    case "OptionRemoved":
                        OptionRemovedResolve(parts);
                        break;

                    case "OptionPicked":
                        OptionPickedResolve(parts);
                        break;

                    case "FlightEnded":
                        FlightEndedResolve();
                        break;

                    case "EndResult":
                        EndResultResolve(parts);
                        break;

                    default:
                        throw new UnknownMessageException(message);
                }
            }
        }

        /// <summary>
        /// Method called when the server sends a message telling the end result based on the cash amounts
        /// </summary>
        /// <param name="parts"></param>
        private void EndResultResolve(string[] parts)
        {
            if (_serverStage != ServerStage.PastFlight)
            {
                _pingTimer.Stop();
                _pingTimer.Dispose();
                throw new OutOfSyncException();
            }
            int count = int.Parse(parts[1]);

            List<(PlayerColor, int)> values = new List<(PlayerColor, int)>();

            for (int i = 0; i < count; ++i)
            {
                PlayerColor player = Enum.Parse<PlayerColor>(parts[i * 2 + 2]);
                int cash = int.Parse(parts[i * 2 + 3]);
                values.Add((player, cash));
            }
            GameEnded?.Invoke(this, new EndResultEventArgs(values));

            WriteMessageToServer("EndResultReceived");
            Close();
        }

        /// <summary>
        /// Method called when the server sends a message that flight stage ended, either because there are no more cards left, or all players crashed
        /// </summary>
        private void FlightEndedResolve()
        {
            if (_serverStage != ServerStage.Flight)
            {
                _pingTimer.Stop();
                _pingTimer.Dispose();
                throw new OutOfSyncException();
            }
            _serverStage = ServerStage.PastFlight;
            FlightEnded?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Method called when the client sends the response to an earlier message from this client to take an option
        /// </summary>
        /// <param name="parts"></param>
        private void OptionPickedResolve(string[] parts)
        {
            if (_serverStage != ServerStage.Flight)
            {
                _pingTimer.Stop();
                _pingTimer.Dispose();
                throw new OutOfSyncException();
            }

            int option = int.Parse(parts[1]);
            OptionPicked?.Invoke(this, option);
        }

        /// <summary>
        /// Method called when the server sends a message that an option from the current card got removed
        /// </summary>
        /// <param name="parts"></param>
        private void OptionRemovedResolve(string[] parts)
        {
            if (_serverStage != ServerStage.Flight)
            {
                _pingTimer.Stop();
                _pingTimer.Dispose();
                throw new OutOfSyncException();
            }

            int option = int.Parse(parts[1]);
            OptionRemoved?.Invoke(this, option);
        }

        /// <summary>
        /// Method called when the server sends a message that the current card has been played through
        /// </summary>
        private void CardOverResolve()
        {
            if (_serverStage != ServerStage.Flight)
            {
                _pingTimer.Stop();
                _pingTimer.Dispose();
                throw new OutOfSyncException();
            }
            CardOver?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Method called when the server sends a message that a player moved
        /// </summary>
        /// <param name="parts"></param>
        private void PlayerMovedResolve(string[] parts)
        {
            PlayerColor player = Enum.Parse<PlayerColor>(parts[1]);

            if (_serverStage != ServerStage.Flight)
            {
                _pingTimer.Stop();
                _pingTimer.Dispose();
                throw new OutOfSyncException();
            }

            int distance = int.Parse(parts[2]);

            _orderManager.AddDistance(player, distance);
        }

        /// <summary>
        /// Method called when the server sends a message that this player got targeted
        /// </summary>
        private void TargetedPlayerResolve()
        {
            if (_serverStage != ServerStage.Flight)
            {
                _pingTimer.Stop();
                _pingTimer.Dispose();
                throw new OutOfSyncException();
            }
            PlayerTargeted?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Method called when the server sends a message that another player got targeted
        /// </summary>
        /// <param name="parts"></param>
        private void OtherTargetResolve(string[] parts)
        {
            if(_serverStage != ServerStage.Flight)
            {
                _pingTimer.Stop();
                _pingTimer.Dispose();
                throw new OutOfSyncException();
            }
            PlayerColor target = Enum.Parse<PlayerColor>(parts[1]);
            OtherTargeted?.Invoke(this, target);
        }

        /// <summary>
        /// Method called when the server sends a message that a player crashed
        /// </summary>
        /// <param name="parts"></param>
        private void PlayerCrashResolve(string[] parts)
        {
            if (_serverStage != ServerStage.Flight)
            {
                _pingTimer.Stop();
                _pingTimer.Dispose();
                throw new OutOfSyncException();
            }
            PlayerColor crashedPlayer = Enum.Parse<PlayerColor>(parts[1]);

            _orderManager.Properties.Remove(crashedPlayer);
            PlayerInfos[crashedPlayer].IsFlying = false;
            PlayerCrashed?.Invoke(this, crashedPlayer);
        }

        /// <summary>
        /// Method called when the server sends a message that a new card has been picked
        /// </summary>
        /// <param name="parts"></param>
        private void CardPickedResolve(string[] parts)
        {
            if (_serverStage != ServerStage.Flight)
            {
                _pingTimer.Stop();
                _pingTimer.Dispose();
                throw new OutOfSyncException();
            }
            Card = parts[1].ToCardEvent();
            IsReady = false;
            CardPicked?.Invoke(this, EventArgs.Empty);
        }

        private void PingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            WriteMessageToServer("Ping");
        }

        /// <summary>
        /// Method called when the server sends a message that another player disconnected
        /// </summary>
        /// <param name="parts"></param>
        private void PlayerDisconnectResolve(string[] parts)
        {
            PlayerColor disconnectedPlayer = Enum.Parse<PlayerColor>(parts[1]);
            PlayerInfos.Remove(disconnectedPlayer);
            if (_playerOrder != null)
            {
                _playerOrder.Remove(disconnectedPlayer);
            }
            if (_orderManager != null)
            {
                _orderManager.Properties.Remove(disconnectedPlayer);
                PlacesChanged?.Invoke(this, EventArgs.Empty);
            }
            PlayerDisconnected?.Invoke(this, new PlayerEventArgs(disconnectedPlayer));
        }

        /// <summary>
        /// Method called when the server sends a message that another player connected
        /// </summary>
        /// <param name="parts"></param>
        private void PlayerConnectedResolve(string[] parts)
        {
            if (_serverStage != ServerStage.Lobby)
            {
                _pingTimer.Stop();
                _pingTimer.Dispose();
                throw new OutOfSyncException();
            }
            PlayerColor color = Enum.Parse<PlayerColor>(parts[1]);
            string name = parts[2];
            PlayerInfos[color] = new PlayerInfo(color, name, false);
            PlayerConnected?.Invoke(this, new PlayerConnectedEventArgs(name, color));
        }

        /// <summary>
        /// Method called when the server sends a message that the flight stage started
        /// </summary>
        /// <param name="parts"></param>
        private void FlightBegunResolve(string[] parts)
        {
            if (!IsReady)
            {
                _pingTimer.Stop();
                _pingTimer.Dispose();
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
                _pingTimer.Dispose();
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
                _pingTimer.Dispose();
                throw new OutOfSyncException();
            }
            IsReady = false;
            foreach (PlayerInfo info in PlayerInfos.Values)
            {
                info.IsReady = false;
            }
            for (int i = 1; i < parts.Length; ++i)
            {
                _playerOrder.Add(Enum.Parse<PlayerColor>(parts[i]));
            }

            _orderManager = new PlayerOrderManager(_playerOrder, GameStage);
            
            _orderManager.PlacesChanged += (sender, e) =>
            {
                PlacesChanged?.Invoke(this, e);
            };

            _orderManager.PlayerCrashed += (sender, e) =>
            {
                if(e == Player)
                {
                    Crashed = true;
                }
                PlayerInfos[e].IsFlying = false;
                PlayerCrashed?.Invoke(this, e);
            };

            _serverStage = ServerStage.PastBuild;
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
                _pingTimer.Dispose();
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
                _pingTimer.Dispose();
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
                _pingTimer.Dispose();
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
                _pingTimer.Dispose();
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
            PlayerReadied?.Invoke(this, new PlayerEventArgs(player));
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
                _pingTimer.Dispose();
                ThisPlayerDisconnected?.Invoke(this, EventArgs.Empty);
            }
            catch (ObjectDisposedException) { }
        }

        #endregion
    }
}
