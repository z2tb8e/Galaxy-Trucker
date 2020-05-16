using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GalaxyTrucker.Model;
using GalaxyTrucker.Model.CardEventTypes;
using GalaxyTrucker.Properties;

namespace GalaxyTrucker.Network
{
    public class PartAvailability
    {
        public Semaphore Semaphore { get; }

        public bool IsAvailable { get; set; }

        public string PartString { get; set; }

        public PartAvailability(string partString)
        {
            Semaphore = new Semaphore(1, 1);
            IsAvailable = true;
            PartString = partString;
        }
    }

    public class ConnectionInfo
    {
        public TcpClient Client { get; }

        public NetworkStream Stream { get; }

        public bool IsReady { get; set; }

        public bool ReadyToFly { get; set; }

        public bool HasMessage { get; set; }

        public Semaphore SendSemaphore { get; }

        public PlayerAttributes Attributes { get; set; }

        public string DisplayName { get; set; }

        public bool Crashed { get; set; }

        public ConnectionInfo(TcpClient client)
        {
            Crashed = false;
            Client = client;
            Stream = client.GetStream();
            IsReady = false;
            ReadyToFly = false;
            HasMessage = false;
            SendSemaphore = new Semaphore(1, 1);
        }
    }

    public class GTTcpListener
    {
        #region fields

        private const double PingInterval = 500;

        private readonly TcpListener _listener;
        private readonly ConcurrentDictionary<PlayerColor, ConnectionInfo> _connections;

        private readonly AutoResetEvent _proceedStageEvent;
        private readonly Semaphore _proceedStageSemaphore;

        private readonly AutoResetEvent _proceedRoundEvent;
        private readonly Semaphore _proceedRoundSemaphore;

        private readonly string _logPath;
        private readonly Semaphore _logSemaphore;
        private readonly bool _doLogging;

        private readonly System.Timers.Timer _pingTimer;
        private readonly Random _random;

        private volatile ServerStage _serverStage;
        private readonly GameStage _gameStage;

        private List<PlayerColor> _playerOrder;
        private readonly Semaphore _orderSemaphore;

        private readonly PartAvailability[,] _parts;
        private Stack<CardEvent> _deck;
        private CardEvent _currentCard;
        private PlayerOrderManager _playerOrderManager;

        #endregion

        #region properties

        public IEnumerable<PlayerColor> NotReadyPlayers
            => _connections.Keys.Where(p => !_connections[p].IsReady);

        #endregion

        public GTTcpListener(int port, GameStage gameStage, bool doLogging = true)
        {
            _proceedStageEvent = new AutoResetEvent(false);
            _proceedStageSemaphore = new Semaphore(1, 1);

            _proceedRoundEvent = new AutoResetEvent(false);
            _proceedRoundSemaphore = new Semaphore(1, 1);

            _gameStage = gameStage;
            Directory.CreateDirectory("logs");
            _logPath = $"logs/log_{DateTime.Now:yyyyMMddHHmmss}.log";
            _logSemaphore = new Semaphore(1, 1);
            _doLogging = doLogging;

            _pingTimer = new System.Timers.Timer(PingInterval);
            _pingTimer.Elapsed += PingTimer_Elapsed;

            _listener = new TcpListener(IPAddress.Any, port);
            _connections = new ConcurrentDictionary<PlayerColor, ConnectionInfo>();

            _random = new Random();
            _orderSemaphore = new Semaphore(1, 1);

            _parts = new PartAvailability[10, 14];
        }

        #region public methods

        public void Start()
        {
            try
            {
                LogAsync($"Server started listening, GameStage: {((int)_gameStage) + 1}");
                _listener.Start(4);
                _serverStage = ServerStage.Lobby;
                _pingTimer.Start();
                Task shuffle = new Task(ShuffleParts);
                Task makeDeck = new Task(MakeDeck);
                shuffle.Start();
                makeDeck.Start();
                while (_connections.Count < 4 && _serverStage == ServerStage.Lobby)
                {
                    if (_listener.Pending())
                    {
                        TcpClient client = _listener.AcceptTcpClient();
                        PlayerColor assignedColor = Enum.GetValues(typeof(PlayerColor)).Cast<PlayerColor>()
                            .Where(color => !_connections.ContainsKey(color)).First();

                        ConnectionInfo newConnection = new ConnectionInfo(client);

                        //Send the assigned color and the selected gamestage
                        byte[] colorAndStageMessage = Encoding.ASCII.GetBytes($"{assignedColor},{_gameStage}#");
                        newConnection.Stream.Write(colorAndStageMessage, 0, colorAndStageMessage.Length);

                        //Receive the client's display name
                        StringBuilder name = new StringBuilder();
                        int character = newConnection.Stream.ReadByte();
                        while ((char)character != '#')
                        {
                            name.Append((char)character);
                            character = newConnection.Stream.ReadByte();
                        }
                        newConnection.DisplayName = name.ToString();

                        //Send the other connected clients' information
                        StringBuilder otherPlayers = new StringBuilder($"{_connections.Count}");
                        string announcement = $"PlayerConnected,{assignedColor},{name}";
                        foreach (PlayerColor key in _connections.Keys)
                        {
                            ConnectionInfo connection = _connections[key];
                            otherPlayers.Append($",{key},{connection.DisplayName},{connection.IsReady}");
                            WriteMessageToPlayer(key, announcement);
                        }
                        byte[] otherPlayersMessage = Encoding.ASCII.GetBytes($"{otherPlayers}#");
                        newConnection.Stream.Write(otherPlayersMessage, 0, otherPlayersMessage.Length);

                        _connections[assignedColor] = newConnection;
                        Task.Factory.StartNew(() => HandleClientMessages(assignedColor), TaskCreationOptions.LongRunning);
                        LogAsync($"{assignedColor} player with name \"{name}\" connected.");
                    }
                }
                Task.Factory.StartNew(() => RefuseFurtherConnections(), TaskCreationOptions.LongRunning);
                shuffle.Wait();
                makeDeck.Wait();
            }
            catch (SocketException e)
            {
                LogAsync($"SocketException {e}");
            }
            catch (ArgumentException e)
            {
                LogAsync($"ArgumentException {e}");
            }
            catch (ObjectDisposedException)
            {
                return;
            }
        }

        public void StartBuildStage()
        {
            if (_connections.Count < 2)
            {
                LogAsync("Less than 2 players are connected, BuildStage not started.");
                return;
            }
            else if (_connections.Values.Where(c => !c.IsReady).Any())
            {
                LogAsync("Not all players are ready, BuildStage not started.");
                return;
            }

            foreach (PlayerColor player in _connections.Keys)
            {
                _connections[player].IsReady = false;
                WriteMessageToPlayer(player, "BuildingBegun");
            }
            _serverStage = ServerStage.Build;

            _playerOrder = new List<PlayerColor>();

            LogAsync("StartBuildStage over");
            BuildStage();
        }

        public void Close()
        {
            _pingTimer.Stop();
            foreach (ConnectionInfo connection in _connections.Values)
            {
                if (connection.Client != null)
                {
                    connection.Client.Close();
                }
            }
            _listener.Stop();
            LogAsync("Server successfully closed.");
        }

        #endregion

        #region private methods

        private void BuildStage()
        {
            _proceedStageEvent.WaitOne();

            string playerOrder = string.Join(',', _playerOrder);
            foreach (PlayerColor player in _connections.Keys)
            {
                WriteMessageToPlayer(player, $"BuildingEnded,{playerOrder}");
                _connections[player].IsReady = false;
            }
            LogAsync($"Building stage over, player order: ({playerOrder})");
            _serverStage = ServerStage.PastBuild;
            _playerOrderManager = new PlayerOrderManager(_playerOrder, _gameStage);
            BeginFlightStage();
        }

        private void BeginFlightStage()
        {
            _proceedStageEvent.WaitOne();
            //while (_connections.Values.Where(c => !c.IsReady || !c.ReadyToFly).Any()) ;
            _serverStage = ServerStage.Flight;

            StringBuilder playerAttributes = new StringBuilder($"FlightBegun,{_connections.Count}");
            foreach (PlayerColor player in _connections.Keys)
            {
                _connections[player].IsReady = false;
                playerAttributes.Append($",{player}{_connections[player].Attributes}");
            }

            foreach (PlayerColor player in _connections.Keys)
            {
                WriteMessageToPlayer(player, playerAttributes.ToString());
            }

            FlightStage();
        }

        private void FlightStage()
        {
            LogAsync("FlightStage started");

            //while there are cards left and there is at least one player who has not crashed yet
            while(_deck.Count > 0 && _connections.Values.Where(conn => !conn.Crashed).Any())
            {
                _currentCard = _deck.Pop();
                LogAsync($"Server picked card {_currentCard}.");
                foreach (PlayerColor player in _connections.Keys)
                {
                    if (!_connections[player].Crashed)
                    {
                        _connections[player].ReadyToFly = false;
                        _connections[player].IsReady = false;
                    }
                    WriteMessageToPlayer(player, $"CardPicked,{_currentCard},{_deck.Count}");
                }

                switch (_currentCard)
                {
                    case Sabotage _:
                        ResolveSabotage();
                        break;
                }
                //wait until everyone signals that they are ready for the next card
                _proceedStageEvent.WaitOne();
            }
        }

        private void GetTargetPlayer(Func<PlayerColor> condition)
        {
            _proceedRoundEvent.WaitOne();

            PlayerColor target = condition();

            LogAsync($"Target for current card resolved to be {target}.");

            WriteMessageToPlayer(target, "TargetedPlayer");

            foreach(PlayerColor key in _connections.Keys)
            {
                if (key != target)
                {
                    WriteMessageToPlayer(key, $"OtherTarget,{target}");
                }
            }
        }

        private void ResolveSabotage()
        {
            PlayerColor getLowestCrewPlayer()
            {
                int minCrewValue = _connections.Values.Where(conn => !conn.Crashed).Select(conn => conn.Attributes.CrewCount).Min();

                PlayerColor target = _playerOrderManager.GetCurrentOrder()
                .Where(player => !_connections[player].Crashed && _connections[player].Attributes.CrewCount == minCrewValue)
                .First();

                return target;
            }

            GetTargetPlayer(getLowestCrewPlayer);
        }

        private void PingTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            foreach (PlayerColor key in _connections.Keys)
            {
                WriteMessageToPlayer(key, "Ping");
            }
        }

        private void HandleClientMessages(PlayerColor player)
        {
            string message;
            string[] parts;
            ConnectionInfo connection = _connections[player];
            while (connection.Client.Connected)
            {
                try
                {
                    connection.HasMessage = false;
                    if (connection.Stream.DataAvailable)
                    {
                        connection.HasMessage = true;
                        message = ReadMessageFromPlayer(player);

                        //Connection closed
                        if (message == null)
                        {
                            return;
                        }
                        parts = message.Split(',');
                        switch (parts[0])
                        {
                            case "ToggleReady":
                                ToggleReadyResolve(player);
                                break;

                            case "PickPart":
                                PickPartResolve(player, parts);
                                break;

                            case "PutBackPart":
                                PutBackPartResolve(player, parts);
                                break;

                            case "StartFlightStage":
                                StartFlightStageResolve(player, parts);
                                break;

                            case "Ping":
                                break;

                            case "AttributesUpdate":
                                StartFlightStageResolve(player, parts);
                                break;

                            case "PlayerCrash":
                                PlayerCrashResolve(player);
                                break;

                            default:
                                LogAsync($"Unhandled client message from {player}: {message}");
                                break;
                        }
                    }
                }
                catch (ObjectDisposedException) { }
            }
            LogAsync($"Handler thread for client {player} finished.");
        }

        /// <summary>
        /// Method called when a client sends a message signaling that they crashed
        /// </summary>
        /// <param name="player"></param>
        private void PlayerCrashResolve(PlayerColor player)
        {
            _connections[player].Crashed = true;
            _playerOrderManager.Properties[player].CanMove = false;
            foreach(PlayerColor key in _connections.Keys)
            {
                if (key != player)
                {
                    WriteMessageToPlayer(key, $"OtherPlayerCrash,{player}");
                }
            }
        }

        /// <summary>
        /// Method called when a client sends a message signaling it's ready to enter Flight stage,
        /// or in flight stage, when they are ready to resolve a round requiring attribute checks
        /// </summary>
        /// <param name="player"></param>
        /// <param name="parts"></param>
        private void StartFlightStageResolve(PlayerColor player, string[] parts)
        {
            if (_connections[player].ReadyToFly)
            {
                LogAsync($"{player} tried updating attributes while being readied.");
            }
            _connections[player].Attributes = new PlayerAttributes
            {
                Firepower = int.Parse(parts[1]),
                Enginepower = int.Parse(parts[2]),
                CrewCount = int.Parse(parts[3]),
                StorageSize = int.Parse(parts[4]),
                Batteries = int.Parse(parts[5])
            };
            _connections[player].ReadyToFly = true;
            if(_serverStage == ServerStage.Flight)
            {
                Task.Run(CheckIfProceedRound);
            }
            LogAsync($"{player} added/updated attributes with value ({_connections[player].Attributes}).");
        }

        /// <summary>
        /// Method to check if the proceed round event should be signaled
        /// </summary>
        private void CheckIfProceedRound()
        {
            if (!_proceedRoundSemaphore.WaitOne(0))
            {
                return;
            }
            bool proceed = true;
            foreach (var item in _connections.Values)
            {
                proceed &= item.ReadyToFly || item.Crashed;
            }
            if (proceed)
            {
                _proceedRoundEvent.Set();
            }
            _proceedRoundSemaphore.Release();
        }

        /// <summary>
        /// Method to check if the proceed stage event should be signaled
        /// </summary>
        private void CheckIfProceedStage()
        {
            if (!_proceedStageSemaphore.WaitOne(0))
            {
                return;
            }
            bool proceed = true;
            foreach(var item in _connections.Values)
            {
                proceed &= _serverStage switch
                {
                    ServerStage.PastBuild => item.IsReady && item.ReadyToFly,
                    ServerStage.Flight => item.IsReady || item.Crashed,
                    _ => item.IsReady
                };
            }
            if (proceed)
            {
                _proceedStageEvent.Set();
            }
            _proceedStageSemaphore.Release();
        }

        /// <summary>
        /// Method called when a client sends a message to toggle its readied state
        /// </summary>
        /// <param name="player"></param>
        private void ToggleReadyResolve(PlayerColor player)
        {
            _connections[player].IsReady = !_connections[player].IsReady;

            if (_serverStage == ServerStage.Build)
            {
                _orderSemaphore.WaitOne();
                if (_connections[player].IsReady)
                {
                    _playerOrder.Add(player);
                }
                else
                {
                    _playerOrder.Remove(player);
                }
                _orderSemaphore.Release();
            }
            LogAsync($"{player} toggled ready, new value: {_connections[player].IsReady}.");
            string response = "ToggleReadyConfirm";
            string announcement = $"PlayerToggledReady,{player}";
            WriteMessageToPlayer(player, response);
            foreach (PlayerColor key in _connections.Keys)
            {
                if (player != key)
                {
                    WriteMessageToPlayer(key, announcement);
                }
            }
            if (_connections[player].IsReady && _serverStage != ServerStage.Lobby)
            {
                Task.Run(CheckIfProceedStage);
            }
        }

        /// <summary>
        /// Method called when a client sends a message signaling it put back a part into the shared collection
        /// </summary>
        /// <param name="player"></param>
        /// <param name="parts"></param>
        private void PutBackPartResolve(PlayerColor player, string[] parts)
        {
            int row = int.Parse(parts[1]);
            int column = int.Parse(parts[2]);
            _parts[row, column].Semaphore.WaitOne();

            LogAsync($"{player} put back at ({row},{column})");
            if (_parts[row, column].IsAvailable)
            {
                WriteMessageToPlayer(player, "PutBackPartConfirm");
            }
            else
            {
                string partString = _parts[row, column].PartString;
                _parts[row, column].IsAvailable = true;
                string response = "PutBackPartConfirm";
                string announcement = $"PartPutBack,{parts[1]},{parts[2]},{partString}";
                WriteMessageToPlayer(player, response);
                Task.Factory.StartNew(() =>
                {
                    foreach (PlayerColor key in _connections.Keys)
                    {
                        if (player != key)
                        {
                            WriteMessageToPlayer(key, announcement);
                        }
                    }
                });
            }
            _parts[row, column].Semaphore.Release();
        }

        /// <summary>
        /// Method called when a clients sends a message that it wants to pick a part at the given indices
        /// </summary>
        /// <param name="player"></param>
        /// <param name="parts"></param>
        private void PickPartResolve(PlayerColor player, string[] parts)
        {
            int row = int.Parse(parts[1]);
            int column = int.Parse(parts[2]);
            _parts[row, column].Semaphore.WaitOne();

            LogAsync($"{player} picked part at ({row},{column}) with response: {(_parts[row, column].IsAvailable ? _parts[row, column].PartString : null)}.");
            if (!_parts[row, column].IsAvailable)
            {
                WriteMessageToPlayer(player, "PickPartResult,null");
            }
            else
            {
                string partString = _parts[row, column].PartString;
                _parts[row, column].IsAvailable = false;
                string response = $"PickPartResult,{partString}";
                string announcement = $"PartTaken,{row},{column}";
                WriteMessageToPlayer(player, response);
                foreach (PlayerColor key in _connections.Keys)
                {
                    if (player != key)
                    {
                        WriteMessageToPlayer(key, announcement);
                    }
                }
            }
            _parts[row, column].Semaphore.Release();
        }

        private string ReadMessageFromPlayer(PlayerColor player)
        {
            try
            {
                NetworkStream ns = _connections[player].Stream;
                StringBuilder message = new StringBuilder();
                int character = ns.ReadByte();
                while ((char)character != '#')
                {
                    message.Append((char)character);
                    character = ns.ReadByte();
                }
                return message.ToString();
            }
            //Connection closed
            catch (IOException)
            {
                _connections.Remove(player, out _);
                LogAsync($"{player} player disconnected.");
                if (_playerOrder != null)
                {
                    _playerOrder.Remove(player);
                }
                foreach (PlayerColor otherPlayer in _connections.Keys)
                {
                    WriteMessageToPlayer(otherPlayer, $"PlayerDisconnect,{player}");
                }
                return null;
            }
        }

        private void WriteMessageToPlayer(PlayerColor player, string message)
        {
            try
            {
                _connections[player].SendSemaphore.WaitOne();
                NetworkStream ns = _connections[player].Stream;
                byte[] msg = Encoding.ASCII.GetBytes($"{message}#");
                ns.Write(msg, 0, msg.Length);
                _connections[player].SendSemaphore.Release();
            }
            //Connection closed
            catch (IOException)
            {
                _connections.Remove(player, out _);
                LogAsync($"{player} player disconnected.");
                if (_playerOrder != null)
                {
                    _playerOrder.Remove(player);
                }
                foreach (PlayerColor otherPlayer in _connections.Keys)
                {
                    WriteMessageToPlayer(otherPlayer, $"PlayerDisconnect,{player}");
                }
            }
        }

        private void RefuseFurtherConnections()
        {
            while (true)
            {
                if (_listener.Pending())
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    NetworkStream stream = client.GetStream();
                    byte[] message = Encoding.ASCII.GetBytes("Connection refused#");
                    stream.Write(message, 0, message.Length);
                    LogAsync($"Connection refused from {client.Client.RemoteEndPoint}");
                    client.Close();
                }
            }
        }

        private void ShuffleParts()
        {
            List<string> parts = Resources.Parts.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
            parts.Remove(parts.Last());

            int n = parts.Count;
            while (n > 1)
            {
                n--;
                int k = _random.Next(n + 1);
                string value = parts[k];
                parts[k] = parts[n];
                parts[n] = value;
            }

            for (int i = 0; i < 10; ++i)
            {
                for (int j = 0; j < 14; ++j)
                {
                    _parts[i, j] = new PartAvailability(parts[i * 10 + j]);
                }
            }
        }

        private void MakeDeck()
        {
            List<CardEvent>[] cardsByStage = new List<CardEvent>[3]
            {
                new List<CardEvent>(),
                new List<CardEvent>(),
                new List<CardEvent>()
            };

            List<string> cardStrings = Resources.Cards.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
            cardStrings.Remove(cardStrings.Last());
            foreach (string cardString in cardStrings)
            {
                CardEvent card = cardString.ToCardEvent();
                cardsByStage[(int)card.Stage].Add(card);
            }

            List<int> deckComposition = new List<int>();
            switch (_gameStage)
            {
                case GameStage.First:
                    deckComposition.Add(0);
                    deckComposition.Add(0);
                    break;
                case GameStage.Second:
                    deckComposition.Add(1);
                    deckComposition.Add(1);
                    deckComposition.Add(0);
                    break;
                case GameStage.Third:
                    deckComposition.Add(2);
                    deckComposition.Add(2);
                    deckComposition.Add(1);
                    deckComposition.Add(0);
                    break;
                default:
                    throw new InvalidEnumArgumentException(nameof(_gameStage), (int)_gameStage, typeof(GameStage));
            }

            List<CardEvent> cardList = new List<CardEvent>();
            for (int i = 0; i < 4; ++i)
            {
                foreach (int stage in deckComposition)
                {
                    List<CardEvent> stageCards = cardsByStage[stage];
                    int index = _random.Next(stageCards.Count);
                    cardList.Add(stageCards[index]);
                }
            }

            _deck = new Stack<CardEvent>();
            while(cardList.Count > 0)
            {
                CardEvent item = cardList[_random.Next(cardList.Count)];
                _deck.Push(item);
                cardList.Remove(item);
            }

            LogAsync($"Deck assembled, cards:\n {string.Join("\n ", _deck)}");
        }

        private async void LogAsync(string message)
        {
            if (!_doLogging)
            {
                return;
            }
            _logSemaphore.WaitOne();
            using (StreamWriter sr = new StreamWriter(_logPath, true))
            {
                await sr.WriteLineAsync($"{ DateTime.Now:H:mm:ss}: {message}");
            }
            _logSemaphore.Release();
        }

        #endregion
    }
}
