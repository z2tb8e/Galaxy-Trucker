using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GalaxyTrucker.Model;

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

        public bool HasMessage { get; set; }

        public Semaphore SendSemaphore { get; }

        public PlayerAttributes Attributes { get; set; }

        public string DisplayName { get; set; }

        public ConnectionInfo(TcpClient client)
        {
            Client = client;
            Stream = client.GetStream();
            IsReady = false;
            HasMessage = false;
            SendSemaphore = new Semaphore(1, 1);
        }
    }

    public class GTTcpListener
    {
        #region fields

        private const string PartPath = "Resources/Parts.txt";
        private const string CardPath = "Resources/Cards.txt";
        private const double PingInterval = 500;

        private readonly System.Timers.Timer _pingTimer;

        private readonly TcpListener _listener;

        private readonly Random _random;

        private readonly ConcurrentDictionary<PlayerColor, ConnectionInfo> _connections;

        private readonly Semaphore _orderSemaphore;

        private volatile ServerStage _serverStage;

        private readonly GameStage _gameStage;

        private List<PlayerColor> _playerOrder;

        private readonly PartAvailability[,] _parts;

        #endregion

        #region properties

        public IEnumerable<PlayerColor> NotReadyPlayers
            => _connections.Keys.Where(p => !_connections[p].IsReady);

        #endregion

        public GTTcpListener(int port, GameStage gameStage)
        {
            _gameStage = gameStage;

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
                _listener.Start(4);
                _serverStage = ServerStage.Lobby;
                _pingTimer.Start();
                Task shuffle = ShufflePartsAsync();
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
                    }
                }
                Task.Factory.StartNew(() => RefuseFurtherConnections(), TaskCreationOptions.LongRunning);
                shuffle.Wait();
            }
            catch (SocketException e)
            {
                Console.WriteLine($"SocketException {e}");
            }
            catch (ArgumentException e)
            {
                Console.WriteLine($"ArgumentException {e}");
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
                Console.WriteLine("Less than 2 players are connected, BuildStage not started.");
                return;
            }
            else if (_connections.Values.Where(c => !c.IsReady).Any())
            {
                Console.WriteLine("Not all players are ready, BuildStage not started.");
                return;
            }

            foreach (PlayerColor player in _connections.Keys)
            {
                _connections[player].IsReady = false;
                WriteMessageToPlayer(player, "BuildingBegun");
            }
            _serverStage = ServerStage.Build;

            _playerOrder = new List<PlayerColor>();

            Console.WriteLine("StartBuildStage over");
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
        }

        #endregion

        #region private methods

        private void BuildStage()
        {
            while (_connections.Values.Where(c => !c.IsReady || c.HasMessage).Any()) ;

            string playerOrder = string.Join(',', _playerOrder);
            foreach (PlayerColor player in _connections.Keys)
            {
                WriteMessageToPlayer(player, $"BuildingEnded,{playerOrder}");
                _connections[player].IsReady = false;
            }
            Console.WriteLine($"Building stage over, player order: ({playerOrder})");
            BeginFlightStage();
        }

        private void BeginFlightStage()
        {
            while (_connections.Values.Where(c => !c.IsReady).Any()) ;
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
            Console.WriteLine("FlightStage started");
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
                        Console.WriteLine($"Message received from ({connection.DisplayName}, {player}): {message}");
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

                            default:
                                Console.WriteLine($"Unhandled client message from {player}: {message}");
                                break;
                        }
                    }
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Method called when a client sends a message signaling it's ready to enter Flight stage
        /// </summary>
        /// <param name="player"></param>
        /// <param name="parts"></param>
        private void StartFlightStageResolve(PlayerColor player, string[] parts)
        {
            _connections[player].Attributes = new PlayerAttributes
            {
                Firepower = int.Parse(parts[1]),
                Enginepower = int.Parse(parts[2]),
                CrewCount = int.Parse(parts[3]),
                StorageSize = int.Parse(parts[4]),
                Batteries = int.Parse(parts[5])
            };
            _connections[player].IsReady = true;
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
                    client.Close();
                }
            }
        }

        private async Task ShufflePartsAsync()
        {
            List<string> parts = new List<string>();
            using(StreamReader sr = new StreamReader(PartPath))
            {
                string line;
                while ((line = await sr.ReadLineAsync()) != null)
                {
                    parts.Add(line);
                }
            }

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

        #endregion
    }
}
