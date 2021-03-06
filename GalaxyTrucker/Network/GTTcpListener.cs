﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GalaxyTrucker.Model;
using GalaxyTrucker.Model.CardTypes;
using GalaxyTrucker.Properties;

namespace GalaxyTrucker.Network
{
    #region helper classes

    /// <summary>
    /// Class used to thread-safely contain a part and information regarding whether it's available
    /// </summary>
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

    /// <summary>
    /// Class used to hold information associated with a single connected client
    /// </summary>
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

        public AutoResetEvent OptionSent { get; set; }

        public bool IsWaiting { get; set; }

        public int? OpenConnectors { get; set; }

        public int? Cash { get; set; }

        public ConnectionInfo(TcpClient client)
        {
            Crashed = false;
            Client = client;
            Stream = client.GetStream();
            IsReady = false;
            ReadyToFly = false;
            HasMessage = false;
            IsWaiting = false;
            SendSemaphore = new Semaphore(1, 1);
            Cash = null;
            OpenConnectors = null;
            OptionSent = new AutoResetEvent(false);
        }
    }

    #endregion

    public class GTTcpListener
    {
        #region fields

        private const double PingInterval = 500;

        private readonly TcpListener _listener;
        private readonly ConcurrentDictionary<PlayerColor, ConnectionInfo> _connections;
        private readonly CancellationTokenSource _cts;

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
        private Stack<Card> _deck;
        private Card _currentCard;
        private PlayerOrderManager _playerOrderManager;
        private volatile int _currentOption;
        private volatile PlayerColor _currentPlayer;

        #endregion

        #region properties

        public IEnumerable<PlayerColor> NotReadyPlayers
            => _connections.Keys.Where(p => !_connections[p].IsReady);

        #endregion

        #region ctor

        public GTTcpListener(int port, GameStage gameStage, bool doLogging = true)
        {
            //check if there's an active listener bound to the given port
            IPGlobalProperties iPGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] usedEndPoints = iPGlobalProperties.GetActiveTcpListeners();
            if(usedEndPoints.Any(endpoint => endpoint.Port == port))
            {
                //error code for address already being in use
                throw new SocketException(10048);
            }

            _cts = new CancellationTokenSource();

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

        #endregion

        #region public methods

        /// <summary>
        /// Method to start the lobby stage, allowing connections to be handled
        /// </summary>
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
                while (_serverStage == ServerStage.Lobby)
                {
                    if (_listener.Pending())
                    {
                        if(_connections.Count < 4)
                        {
                            TcpClient client = _listener.AcceptTcpClient();
                            PlayerColor assignedColor = Enum.GetValues(typeof(PlayerColor)).Cast<PlayerColor>()
                                .First(color => !_connections.ContainsKey(color));

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
                        else
                        {
                            TcpClient client = _listener.AcceptTcpClient();
                            NetworkStream stream = client.GetStream();
                            byte[] message = Encoding.ASCII.GetBytes("Connection refused#");
                            stream.Write(message, 0, message.Length);
                            LogAsync($"Connection refused from {client.Client.RemoteEndPoint} due to lobby being full");
                            client.Close();
                        }
                    }
                }
                Task.Factory.StartNew(() => RefuseFurtherConnections(_cts.Token), TaskCreationOptions.LongRunning);
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

        /// <summary>
        /// Method to proceed to the build stage
        /// </summary>
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

        /// <summary>
        /// Method to close the client, including all open sockets, and disposing of the required resources
        /// </summary>
        public void Close()
        {
            try
            {
                _cts.Cancel();
                Thread.Sleep(500);
                _pingTimer.Stop();
                _pingTimer.Dispose();
                _cts.Dispose();
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
            catch(Exception e)
            {
                LogAsync($"Error while closing server: {e.Message}");
            }
        }

        #endregion

        #region private methods

        #region stage transitions

        /// <summary>
        /// Method to manage the build stage, waiting for the end of building into proceed to the next stage
        /// </summary>
        private void BuildStage()
        {
            while (!_connections.All(conn => conn.Value.IsReady && !conn.Value.HasMessage)) ;

            string playerOrder = string.Join(',', _playerOrder);
            foreach (PlayerColor player in _connections.Keys)
            {
                WriteMessageToPlayer(player, $"BuildingEnded,{playerOrder}");
                _connections[player].IsReady = false;
            }
            LogAsync($"Building stage over, player order: ({playerOrder})");
            _serverStage = ServerStage.PastBuild;
            _playerOrderManager = new PlayerOrderManager(_playerOrder, _gameStage);
            _playerOrderManager.PlayerCrashed += (sender, e) =>
            {
                _connections[e].Crashed = true;
                LogAsync($"{e} crashed from movement.");
            };

            BeginFlightStage();
        }

        /// <summary>
        /// Method to initialize for the flight stage
        /// </summary>
        private void BeginFlightStage()
        {
            while (!_connections.All(conn => conn.Value.IsReady && !conn.Value.HasMessage && conn.Value.ReadyToFly)) ;

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

        /// <summary>
        /// Method to manage the flight stage, drawing and resolving cards until the end of the stage
        /// </summary>
        private void FlightStage()
        {
            LogAsync("FlightStage started");

            //while there are cards left and there is at least one player who has not crashed yet
            while (_deck.Count > 0 && _connections.Values.Where(conn => !conn.Crashed).Any())
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
                        GetTargetPlayer(CardCheckAttribute.CrewCount);
                        break;
                    case Warzone _:
                        ResolveWarzone();
                        break;
                    case MeteorShower _:
                        break;
                    case Pirates _:
                        ResolveEncounter();
                        break;
                    case Smugglers _:
                        ResolveEncounter();
                        break;
                    case Slavers _:
                        ResolveEncounter();
                        break;
                    case OpenSpace _:
                        ResolveOpenSpace();
                        break;
                    case Planets _:
                        ResolvePlanets();
                        break;
                    case AbandonedShip _:
                        ResolveAbandoned();
                        break;
                    case AbandonedStation _:
                        ResolveAbandoned();
                        break;
                    case Pandemic _:
                        break;
                    case Stardust _:
                        ResolveStardust();
                        break;
                    default:
                        throw new ArgumentException($"Unknown card {_currentCard}");
                }

                LogAsync($"{_currentCard} is over.");
                foreach (PlayerColor player in _connections.Keys)
                {
                    WriteMessageToPlayer(player, "CardOver");
                }
                //wait until everyone signals that they are ready for the next card
                while (!_connections.All(conn => conn.Value.IsReady || conn.Value.Crashed)) ;
            }

            PastFlightStage();
            //Flight stage over
        }

        /// <summary>
        /// Method called after the flight stage, summarizing the results and closing the server
        /// </summary>
        private void PastFlightStage()
        {
            LogAsync($"Flight stage over, cards left: {_deck.Count}.");
            foreach (PlayerColor key in _connections.Keys)
            {
                WriteMessageToPlayer(key, "FlightEnded");
            }
            LogAsync("Waiting for cash values.");
            _serverStage = ServerStage.PastFlight;
            while (!_connections.All(conn => conn.Value.Cash.HasValue));

            StringBuilder endResult = new StringBuilder($"EndResult,{_connections.Count}");
            foreach (PlayerColor player in _connections.Keys.OrderByDescending(key => _connections[key].Cash.Value))
            {
                endResult.Append($",{player},{_connections[player].Cash.Value}");
            }
            foreach (PlayerColor player in _connections.Keys)
            {
                WriteMessageToPlayer(player, endResult.ToString());
            }
            //wait until everyone signals that they received the message, after which they disconnect
            while (_connections.Count > 0) ;
            Close();
        }

        #endregion

        #region card resolves

        private void ResolveStardust()
        {
            //wait until the open connectors get sent

            while (!_connections.All(conn => conn.Value.Crashed || conn.Value.OpenConnectors.HasValue)) ;

            foreach (PlayerColor player in _playerOrderManager.GetOrder())
            {
                try
                {
                    int distance = -1 * _connections[player].OpenConnectors.Value;
                    //the manager would signal that the player crashed from not moving
                    if (distance != 0)
                    {
                        MovePlayer(player, distance);
                    }
                }
                catch (KeyNotFoundException)
                {
                    LogAsync("Stardust move message skipped to player disconnecting.");
                }
            }
        }

        private void ResolveAbandoned()
        {
            List<PlayerColor> order = _playerOrderManager.GetOrder();
            int current = 0;
            bool taken = false;
            while (current < order.Count && !taken)
            {
                try
                {
                    _currentPlayer = order[current];
                    //check if the player crashed after picking the card, but before it was their turn
                    if (!_connections[_currentPlayer].Crashed)
                    {
                        _connections[_currentPlayer].IsWaiting = true;
                        SignalTargetPlayer(order[current]);
                        _connections[_currentPlayer].OptionSent.WaitOne();
                        /*options:
                         *  0: none
                         *  1: took the offer
                         */
                        _connections[_currentPlayer].IsWaiting = false;
                        //if the current player didn't crash their ship
                        if (!_connections[_currentPlayer].Crashed)
                        {
                            int dayCost = _currentCard switch
                            {
                                AbandonedShip ship => ship.DayCost,
                                AbandonedStation station => station.DayCost,
                                _ => throw new InvalidOperationException()
                            };
                            switch (_currentOption)
                            {
                                case 0:
                                    break;
                                case 1:
                                    taken = true;
                                    MovePlayer(_currentPlayer, -1 * dayCost);
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException($"Invalid option number {_currentOption}.");
                            }

                            LogAsync($"{_currentPlayer} selected option {_currentOption}.");
                            WriteMessageToPlayer(_currentPlayer, $"OptionPicked,{_currentOption}");
                        }
                    }
                    ++current;
                }
                catch (KeyNotFoundException)
                {
                    LogAsync("Abandoned ship/station turn abruptly ended due to target player disconnecting.");
                    ++current;
                }
            }
        }

        private void ResolvePlanets()
        {
            List<PlayerColor> order = _playerOrderManager.GetOrder();
            int current = 0;
            int numberOfOffers = (_currentCard as Planets).Offers.Count();
            List<int> validOffers = new List<int>();
            for(int i = 1; i <= numberOfOffers; ++i)
            {
                validOffers.Add(i);
            }
            while (current < order.Count && validOffers.Count > 0)
            {
                try
                {
                    _currentPlayer = order[current];
                    //check if the player crashed after picking the card, but before it was their turn
                    if (!_connections[_currentPlayer].Crashed)
                    {
                        _connections[_currentPlayer].IsWaiting = true;
                        SignalTargetPlayer(order[current]);
                        _connections[_currentPlayer].OptionSent.WaitOne();
                        /*options:
                         *  0: none
                         *  [1..numberOfOffers]: the specific offer
                         */
                        _connections[_currentPlayer].IsWaiting = false;
                        //if the current player didn't their ship
                        if (!_connections[_currentPlayer].Crashed)
                        {
                            switch (_currentOption)
                            {
                                case 0:
                                    break;
                                case int i when (i > 0 && i <= numberOfOffers):
                                    if (!validOffers.Remove(i))
                                    {
                                        //The offer is already taken
                                        throw new ArgumentException($"Offer {i} was already taken!");
                                    }
                                    RemoveOption(i);
                                    MovePlayer(_currentPlayer, -1 * (_currentCard as Planets).DayCost);
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException($"Invalid option number {_currentOption}.");
                            }

                            LogAsync($"{_currentPlayer} selected option {_currentOption}.");
                            WriteMessageToPlayer(_currentPlayer, $"OptionPicked,{_currentOption}");
                        }
                    }
                    
                    ++current;
                }
                catch (KeyNotFoundException)
                {
                    LogAsync("Planets turn abruptly ended due to the target player disconnecting.");
                    ++current;
                }
            }
        }

        private void ResolveOpenSpace()
        {
            //wait until the stats get sent
            while (!_connections.All(conn => conn.Value.ReadyToFly || conn.Value.Crashed)) ;

            foreach (PlayerColor player in _playerOrderManager.GetOrder())
            {
                try
                {
                    int distance = _connections[player].Attributes.Enginepower;
                    MovePlayer(player, distance);
                }
                //player disconnected
                catch (KeyNotFoundException)
                {
                    LogAsync("OpenSpace move message skipped due to player disconnecting.");
                }
            }
        }

        private void ResolveEncounter()
        {
            bool defeated = false;
            List<PlayerColor> order = _playerOrderManager.GetOrder();
            int current = 0;
            while (!defeated && current < order.Count)
            {
                try
                {
                    _currentPlayer = order[current];
                    //check if the player crashed after picking the card, but before it was their turn
                    if (!_connections[_currentPlayer].Crashed)
                    {
                        _connections[_currentPlayer].IsWaiting = true;
                        SignalTargetPlayer(order[current]);
                        _connections[_currentPlayer].OptionSent.WaitOne();
                        /*options:
                         *  0: player got beaten
                         *  1: encounter got beaten
                         *  2: player could've beaten encounter, but ignored it or draw
                         */
                        _connections[_currentPlayer].IsWaiting = false;
                        //if the current player decides to crash their ship 
                        if (!_connections[_currentPlayer].Crashed)
                        {
                            int dayCost = _currentCard switch
                            {
                                Pirates pirate => pirate.DayCost,
                                Smugglers smuggler => smuggler.DayCost,
                                Slavers slaver => slaver.DayCost,
                                _ => throw new InvalidOperationException()
                            };

                            switch (_currentOption)
                            {
                                case 0:
                                    break;
                                case 1:
                                    defeated = true;
                                    MovePlayer(_currentPlayer, -1 * dayCost);
                                    break;
                                case 2:
                                    defeated = true;
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }

                            LogAsync($"{_currentPlayer} selected option {_currentOption}.");
                            WriteMessageToPlayer(_currentPlayer, $"OptionPicked,{_currentOption}");
                        }
                    }

                    ++current;
                }
                //the current player got disconnected
                catch (KeyNotFoundException)
                {
                    LogAsync("Encounter turn abruptly ended due to the target player disconnecting.");
                    ++current;
                }
            }
        }

        private void ResolveWarzone()
        {
            try
            {
                Warzone card = _currentCard as Warzone;

                LogAsync("Resolving warzone first event.");
                PlayerColor? target = GetTargetPlayer(card.Event1.Attribute);
                if (target == null)
                {
                    return;
                }

                foreach (PlayerColor player in _connections.Keys)
                {
                    if (!_connections[player].Crashed)
                    {
                        _connections[player].ReadyToFly = false;
                    }
                }

                if (card.Event1.PenaltyType == CardPenalty.Delay)
                {
                    MovePlayer(target.Value, -1 * card.Event1.Penalty);
                }

                LogAsync("Resolving warzone second event.");
                target = GetTargetPlayer(card.Event2.Attribute);
                if (target == null)
                {
                    return;
                }

                foreach (PlayerColor player in _connections.Keys)
                {
                    if (!_connections[player].Crashed)
                    {
                        _connections[player].ReadyToFly = false;
                    }
                }

                if (card.Event2.PenaltyType == CardPenalty.Delay)
                {
                    MovePlayer(target.Value, -1 * card.Event2.Penalty);
                }

                LogAsync("Resolving warzone third event.");
                GetTargetPlayer(card.Event3.Attribute);
            }
            catch (KeyNotFoundException)
            {
                LogAsync("Warzone abruptly ended due to a disconnect.");
            }
        }

        private PlayerColor? GetTargetPlayer(CardCheckAttribute checkAttribute)
        {
            while (!_connections.All(conn => conn.Value.ReadyToFly || conn.Value.Crashed)) ;
            if(_connections.All(conn => conn.Value.Crashed))
            {
                return null;
            }

            int minValue = _connections.Values.Where(conn => !conn.Crashed)
                .Select(conn => checkAttribute switch {
                    CardCheckAttribute.CrewCount => conn.Attributes.CrewCount,
                    CardCheckAttribute.Enginepower => conn.Attributes.Enginepower,
                    CardCheckAttribute.Firepower => conn.Attributes.Firepower,
                    _ => throw new InvalidEnumArgumentException()
                }).Min();

            PlayerColor target = _playerOrderManager.GetOrder()
                .First(player => !_connections[player].Crashed && checkAttribute switch
                {
                    CardCheckAttribute.CrewCount => _connections[player].Attributes.CrewCount == minValue,
                    CardCheckAttribute.Enginepower => _connections[player].Attributes.Enginepower == minValue,
                    CardCheckAttribute.Firepower => _connections[player].Attributes.Firepower == minValue,
                    _ => throw new InvalidEnumArgumentException()
                });

            SignalTargetPlayer(target);

            return target;
        }

        #endregion

        #region player notification

        /// <summary>
        /// Method to add the given distance to the given player, and notify all players of it
        /// </summary>
        /// <param name="player"></param>
        /// <param name="distance"></param>
        private void MovePlayer(PlayerColor player, int distance)
        {
            LogAsync($"{player} moved {distance}.");
            _playerOrderManager.AddDistance(player, distance);
            foreach (PlayerColor key in _connections.Keys)
            {
                WriteMessageToPlayer(key, $"PlayerMoved,{player},{distance}");
            }
        }

        /// <summary>
        /// Method to notify all players of the given option getting removed
        /// </summary>
        /// <param name="option"></param>
        private void RemoveOption(int option)
        {
            foreach(PlayerColor key in _connections.Keys)
            {
                WriteMessageToPlayer(key, $"OptionRemoved,{option}");
            }
        }

        /// <summary>
        /// Method to signal the given player, that they are the target, and notify the others, who the target is
        /// </summary>
        /// <param name="target"></param>
        private void SignalTargetPlayer(PlayerColor target)
        {
            LogAsync($"Card {_currentCard} target is {target}.");

            WriteMessageToPlayer(target, "TargetedPlayer");

            foreach (PlayerColor key in _connections.Keys)
            {
                if (key != target)
                {
                    WriteMessageToPlayer(key, $"OtherTarget,{target}");
                }
            }
        }

        #endregion

        #region incoming message handling

        /// <summary>
        /// Main message handling method running on separate threads for each client to minimize response times
        /// </summary>
        /// <param name="player"></param>
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

                            case "StardustInfo":
                                StardustInfoResolve(player, parts);
                                break;

                            case "AttributesUpdate":
                                StartFlightStageResolve(player, parts);
                                break;

                            case "PlayerCrash":
                                PlayerCrashResolve(player);
                                break;

                            case "CardOption":
                                CardOptionResolve(player, parts);
                                break;

                            case "CashInfo":
                                CashInfoResolve(player, parts);
                                break;

                            case "EndResultReceived":
                                //client disconnects right after sending this, which will be detected by the next attempted ping
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
        /// Method called when a client sends a message to resolve a stardust card, containing the number of open connectors
        /// </summary>
        /// <param name="player"></param>
        /// <param name="parts"></param>
        private void StardustInfoResolve(PlayerColor player, string[] parts)
        {
            _connections[player].OpenConnectors = int.Parse(parts[1]);
        }

        /// <summary>
        /// Method called when a clients sends a message signaling the amount of cash they collected
        /// </summary>
        /// <param name="player"></param>
        /// <param name="parts"></param>
        private void CashInfoResolve(PlayerColor player, string[] parts)
        {
            int cash = int.Parse(parts[1]);
            _connections[player].Cash = cash;
        }

        /// <summary>
        /// Method called when a client sends a message signaling which option they took
        /// </summary>
        /// <param name="player"></param>
        /// <param name="parts"></param>
        private void CardOptionResolve(PlayerColor player, string[] parts)
        {
            int option = int.Parse(parts[1]);

            if(player != _currentPlayer)
            {
                LogAsync($"{player} sent their selected option ({option}) while not being on turn.");
                return;
            }
            _currentOption = option;
            LogAsync($"{player} sent their selected option: {option}.");
            _connections[player].OptionSent.Set();
        }

        /// <summary>
        /// Method called when a client sends a message signaling that they crashed
        /// </summary>
        /// <param name="player"></param>
        private void PlayerCrashResolve(PlayerColor player)
        {
            LogAsync($"{player} crashed their ship.");
            _connections[player].Crashed = true;
            _playerOrderManager.Properties.Remove(player);
            foreach(PlayerColor key in _connections.Keys)
            {
                WriteMessageToPlayer(key, $"PlayerCrash,{player}");
            }
            if (_connections[player].IsWaiting)
            {
                _connections[player].OptionSent.Set();
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
                return;
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
            LogAsync($"{player} added/updated attributes with value ({_connections[player].Attributes}).");
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

            LogAsync($"{player} picked part at ({row},{column}) with response: {(_parts[row, column].IsAvailable ? _parts[row, column].PartString : "null")}.");
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

        #endregion

        #region misc

        /// <summary>
        /// The method called by the pingtimer, pinging each client
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PingTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            foreach (PlayerColor key in _connections.Keys)
            {
                WriteMessageToPlayer(key, "Ping");
            }
        }

        /// <summary>
        /// Method to read a message from the given player, ending in a hashmark, which is removed from the actual message
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
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
                if (_connections[player].IsWaiting)
                {
                    _connections[player].OptionSent.Set();
                }
                _connections.Remove(player, out _);
                LogAsync($"{player} player disconnected.");
                if (_playerOrder != null)
                {
                    _playerOrder.Remove(player);
                }
                if (_playerOrderManager != null)
                {
                    _playerOrderManager.Properties.Remove(player);
                }
                if(_serverStage != ServerStage.PastFlight)
                {
                    foreach (PlayerColor otherPlayer in _connections.Keys)
                    {
                        WriteMessageToPlayer(otherPlayer, $"PlayerDisconnect,{player}");
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Method to send the given message to the given player, in the required, hashmark - closed format
        /// </summary>
        /// <param name="player"></param>
        /// <param name="message"></param>
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
                if (_connections[player].IsWaiting)
                {
                    _connections[player].OptionSent.Set();
                }
                _connections.Remove(player, out _);
                LogAsync($"{player} player disconnected.");
                if (_playerOrder != null)
                {
                    _playerOrder.Remove(player);
                }
                if (_playerOrderManager != null)
                {
                    _playerOrderManager.Properties.Remove(player);
                }

                if (_serverStage != ServerStage.PastFlight)
                {
                    foreach (PlayerColor otherPlayer in _connections.Keys)
                    {
                        WriteMessageToPlayer(otherPlayer, $"PlayerDisconnect,{player}");
                    }
                }
            }
        }

        /// <summary>
        /// Method to refuse all connection attempts
        /// </summary>
        /// <param name="token"></param>
        private void RefuseFurtherConnections(CancellationToken token)
        {
            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }
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

        /// <summary>
        /// Method to create and shuffle the part collection, using the Fisher-Yates shuffle
        /// </summary>
        private void ShuffleParts()
        {
            string allParts = Resources.Parts.Trim();
            List<string> parts = allParts.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();

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
                    _parts[i, j] = new PartAvailability(parts[j * 10 + i]);
                }
            }
        }

        /// <summary>
        /// Method to make the deck for the current gamestage
        /// </summary>
        private void MakeDeck()
        {
            List<Card>[] cardsByStage = new List<Card>[3]
            {
                new List<Card>(),
                new List<Card>(),
                new List<Card>()
            };

            string allCards = Resources.Cards.Trim();
            List<string> cardStrings = allCards.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
            foreach (string cardString in cardStrings)
            {
                Card card = cardString.ToCardEvent();
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

            List<Card> cardList = new List<Card>();
            for (int i = 0; i < 4; ++i)
            {
                foreach (int stage in deckComposition)
                {
                    List<Card> stageCards = cardsByStage[stage];
                    Card item = stageCards[_random.Next(stageCards.Count)];
                    int index = _random.Next(stageCards.Count);
                    cardList.Add(item);
                    stageCards.Remove(item);
                }
            }

            _deck = new Stack<Card>();
            while(cardList.Count > 0)
            {
                Card item = cardList[_random.Next(cardList.Count)];
                _deck.Push(item);
                cardList.Remove(item);
            }

            LogAsync($"Deck assembled, cards:\n {string.Join("\n ", _deck)}");
        }

        /// <summary>
        /// Method to asynchronously write the given message to the end of the logfile
        /// </summary>
        /// <param name="message"></param>
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

        #endregion
    }
}
