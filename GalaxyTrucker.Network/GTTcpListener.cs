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
using GalaxyTrucker.Client.Model;
using GalaxyTrucker.Client.Model.PartTypes;

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

        private readonly int _maxPlayerCount = Enum.GetValues(typeof(PlayerColor)).Length;
        private const string _partPath = "Resources/Parts.txt";
        private const string _cardPath = "Resources/Cards.txt";

        private readonly TcpListener _listener;

        private readonly Random _random;

        private readonly ConcurrentDictionary<PlayerColor, ConnectionInfo> _connections;

        private readonly Semaphore _orderSemaphore;

        private volatile ServerStage _stage;

        private List<PlayerColor> _playerOrder;

        private readonly PartAvailability[,] _parts;

        #endregion

        #region properties

        public IEnumerable<PlayerColor> PlayerOrder
            => _playerOrder.AsReadOnly();

        public IEnumerable<PlayerColor> NotReadyPlayers
            => _connections.Keys.Where(p => !_connections[p].IsReady);

        #endregion

        

        public GTTcpListener(IPEndPoint endPonint)
        {
            _listener = new TcpListener(endPonint);
            _connections = new ConcurrentDictionary<PlayerColor, ConnectionInfo>();

            _random = new Random();
            _orderSemaphore = new Semaphore(1, 1);

            _parts = new PartAvailability[14, 10];
        }

        #region public methods

        public void Start()
        {
            try
            {
                _listener.Start(_maxPlayerCount);
                _stage = ServerStage.Lobby;
                Task.Factory.StartNew(() => ShuffleParts());
                while(_connections.Count < _maxPlayerCount && _stage == ServerStage.Lobby)
                {
                    if (_listener.Pending())
                    {
                        TcpClient client = _listener.AcceptTcpClient();
                        PlayerColor assignedColor = Enum.GetValues(typeof(PlayerColor)).Cast<PlayerColor>()
                            .Where(color => !_connections.ContainsKey(color)).First();
                        _connections[assignedColor] = new ConnectionInfo(client);

                        WriteMessageToPlayer(assignedColor, assignedColor.ToString());

                        Task.Factory.StartNew(() => HandleClientMessages(assignedColor), TaskCreationOptions.LongRunning);
                    }
                }
            }
            catch(SocketException e)
            {
                Console.WriteLine("SocketException {0}", e);
            }
            catch(ArgumentException e)
            {
                Console.WriteLine("ArgumentException {0}", e);
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

            _stage = ServerStage.Build;

            _playerOrder = new List<PlayerColor>();


            StringBuilder playerColors = new StringBuilder();
            foreach(PlayerColor color in _connections.Keys)
            {
                playerColors.Append("," + color.ToString());
            }

            foreach(PlayerColor key in _connections.Keys)
            {
                _connections[key].IsReady = false;
                WriteMessageToPlayer(key, "BuildingBegun" + playerColors.ToString());
            }

            Console.WriteLine("StartBuildStage over");
            BuildStage();
        }

        public void Close()
        {
            foreach(ConnectionInfo connection in _connections.Values)
            {
                if(connection.Client != null)
                {
                    connection.Stream.Close();
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
                WriteMessageToPlayer(player, "BuildingEnded," + playerOrder);
                _connections[player].IsReady = false;
            }
            Console.WriteLine("Building stage over, player order: ({0})", string.Join(',', _playerOrder));
            BeginFlightStage();
        }

        private void BeginFlightStage()
        {
            while (_connections.Values.Where(c => !c.IsReady).Any()) ;
            _stage = ServerStage.Flight;

            StringBuilder playerAttributes = new StringBuilder("FlightBegun," + _connections.Count);
            foreach(PlayerColor player in _connections.Keys)
            {
                _connections[player].IsReady = false;
                playerAttributes.Append("," + player.ToString() + _connections[player].Attributes.ToString());
            }

            foreach(PlayerColor player in _connections.Keys)
            {
                WriteMessageToPlayer(player, playerAttributes.ToString());
            }

            FlightStage();
        }

        private void FlightStage()
        {
            Console.WriteLine("FlightStage started");
        }

        private void HandleClientMessages(PlayerColor player)
        {
            string message;
            string[] parts;
            ConnectionInfo connection = _connections[player];
            while (connection.Client.Connected)
            {
                connection.HasMessage = false;
                if(connection.Stream.DataAvailable)
                {
                    connection.HasMessage = true;
                    message = ReadMessageFromPlayer(player);
                    parts = message.Split(',');
                    Console.WriteLine("Message received from {0}: {1}", player, message);
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

                        default:
                            Console.WriteLine("Unhandled client message from {0}: {1}", player, message);
                            break;
                    }
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
            if (_stage == ServerStage.Build)
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
            string announcement = "PlayerToggledReady," + player.ToString();
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
            int ind1 = int.Parse(parts[1]);
            int ind2 = int.Parse(parts[2]);
            _parts[ind1, ind2].Semaphore.WaitOne();

            if (_parts[ind1, ind2].IsAvailable)
            {
                WriteMessageToPlayer(player, "PutBackPartConfirm");
            }
            else
            {
                string partString = _parts[ind1, ind2].PartString;
                _parts[ind1, ind2].IsAvailable = true;
                string response = "PutBackPartConfirm";
                string announcement = "PartPutBack," + parts[1] + "," + parts[2] + "," + partString;
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
            _parts[ind1, ind2].Semaphore.Release();
        }

        /// <summary>
        /// Method called when a clients sends a message that it wants to pick a part at the given indices
        /// </summary>
        /// <param name="player"></param>
        /// <param name="parts"></param>
        private void PickPartResolve(PlayerColor player, string[] parts)
        {
            int ind1 = int.Parse(parts[1]);
            int ind2 = int.Parse(parts[2]);
            _parts[ind1, ind2].Semaphore.WaitOne();

            if (!_parts[ind1, ind2].IsAvailable)
            {
                WriteMessageToPlayer(player, "PickPartResult,null");
            }
            else
            {
                string partString = _parts[ind1, ind2].PartString;
                _parts[ind1, ind2].IsAvailable = false;
                string response = "PickPartResult," + partString;
                string announcement = "PartTaken," + ind1.ToString() + "," + ind2.ToString();
                WriteMessageToPlayer(player, response);
                foreach (PlayerColor key in _connections.Keys)
                {
                    if (player != key)
                    {
                        WriteMessageToPlayer(key, announcement);
                    }
                }
            }
            _parts[ind1, ind2].Semaphore.Release();
        }

        private string ReadMessageFromPlayer(PlayerColor player)
        {
            NetworkStream ns = _connections[player].Stream;
            StringBuilder message = new StringBuilder();
            int character = ns.ReadByte();
            while((char) character != '#')
            {
                message.Append((char)character);
                character = ns.ReadByte();
            }
            return message.ToString();
        }

        private void WriteMessageToPlayer(PlayerColor player, string message)
        {
            _connections[player].SendSemaphore.WaitOne();
            NetworkStream ns = _connections[player].Stream;
            byte[] msg = Encoding.ASCII.GetBytes(message + "#");
            ns.Write(msg, 0, msg.Length);
            _connections[player].SendSemaphore.Release();
        }

        private void ShuffleParts()
        {
            List<string> parts = new List<string>();
            string line;
            StreamReader sr = new StreamReader(_partPath);
            while((line = sr.ReadLine()) != null)
            {
                parts.Add(line);
            }
            sr.Close();

            int n = parts.Count;
            while(n > 1)
            {
                n--;
                int k = _random.Next(n + 1);
                string value = parts[k];
                parts[k] = parts[n];
                parts[n] = value;
            }
            
            for(int i = 0; i < 14; ++i)
            {
                for(int j = 0; j < 10; ++j)
                {
                    _parts[i, j] = new PartAvailability(parts[i * 10 + j]);
                }
            }
        }

        #endregion
    }
}
