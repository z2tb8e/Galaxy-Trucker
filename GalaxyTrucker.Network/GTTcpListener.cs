using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GalaxyTrucker.Client.Model;

namespace GalaxyTrucker.Network
{
    public class GTTcpListener
    {
        #region fields

        private readonly int _maxPlayerCount = Enum.GetValues(typeof(PlayerColor)).Length;
        private const string _partPath = "Resources/Parts.txt";
        private const string _cardPath = "Resources/Cards.txt";

        private readonly TcpListener _listener;

        private readonly ConcurrentDictionary<PlayerColor, TcpClient> _clients;
        private readonly ConcurrentDictionary<PlayerColor, bool> _playerReady;
        private readonly ConcurrentDictionary<PlayerColor, Semaphore> _canSend;
        private readonly ConcurrentDictionary<PlayerColor, bool> _playerHasMessages;
        private readonly ConcurrentDictionary<PlayerColor, PlayerAttributes> _playerAttributes;

        private volatile ServerStage _stage;

        private Semaphore _orderSemaphore;
        private List<PlayerColor> _playerOrder;

        //Semaphore: limit accessibility to one client at a time
        //bool: whether the tile is not already taken
        private (Semaphore, bool)[,] _partAvailable;
        private Part[,] _parts;

        #endregion

        #region properties

        public IEnumerable<PlayerColor> PlayerOrder
            => _playerOrder.AsReadOnly();

        public IEnumerable<PlayerColor> NotReadyPlayers
            => _playerReady.Keys.Where(p => !_playerReady[p]);

        #endregion

        

        public GTTcpListener(IPEndPoint endPonint)
        {
            _listener = new TcpListener(endPonint);
            _clients = new ConcurrentDictionary<PlayerColor, TcpClient>();
            _playerReady = new ConcurrentDictionary<PlayerColor, bool>();
            _canSend = new ConcurrentDictionary<PlayerColor, Semaphore>();
            _playerHasMessages = new ConcurrentDictionary<PlayerColor, bool>();
            _playerAttributes = new ConcurrentDictionary<PlayerColor, PlayerAttributes>();
        }

        #region public methods
        public void Start()
        {
            try
            {
                _listener.Start(_maxPlayerCount);
                _stage = ServerStage.Lobby;

                while(_clients.Count < _maxPlayerCount && _stage == ServerStage.Lobby)
                {
                    if (_listener.Pending())
                    {
                        TcpClient client = _listener.AcceptTcpClient();
                        PlayerColor assignedColor = Enum.GetValues(typeof(PlayerColor)).Cast<PlayerColor>()
                            .Where(color => !_clients.ContainsKey(color)).First();
                        _clients[assignedColor] = client;
                        _playerReady[assignedColor] = false;
                        NetworkStream stream = client.GetStream();
                        WriteMessageToPlayer(assignedColor, assignedColor.ToString());
                        _playerHasMessages[assignedColor] = false;

                        //new Thread(() => HandleClientMessages(assignedColor)).Start();
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
            if (_clients.Count < 2)
            {
                Console.WriteLine("Less than 2 players are connected, BuildStage not started.");
                return;
            }
            else if (_playerReady.Values.Contains(false))
            {
                Console.WriteLine("Not all players are ready, BuildStage not started.");
                return;
            }
            _parts = new Part[14, 10];
            ShuffleParts();

            _stage = ServerStage.Build;
            _partAvailable = new (Semaphore, bool)[14, 10];
            for (int i = 0; i < 14; ++i)
            {
                for (int j = 0; j < 10; ++j)
                {
                    _partAvailable[i, j] = (new Semaphore(1, 1), true);
                }
            }

            _orderSemaphore = new Semaphore(1, 1);
            _playerOrder = new List<PlayerColor>();


            StringBuilder playerColors = new StringBuilder();
            foreach(PlayerColor color in _clients.Keys)
            {
                _canSend.TryAdd(color, new Semaphore(1, 1));
                playerColors.Append("," + color.ToString());
            }

            foreach(PlayerColor key in _clients.Keys)
            {
                _playerReady[key] = false;
                WriteMessageToPlayer(key, "BuildingBegun" + playerColors.ToString());
            }

            Console.WriteLine("StartBuildStage over");
            BuildStage();
        }

        public void Close()
        {
            foreach(TcpClient client in _clients.Values)
            {
                if(client != null)
                {
                    client.GetStream().Close();
                    client.Close();
                }
            }
            _listener.Stop();
        }

        #endregion

        #region private methods

        private void BuildStage()
        {
            while (_playerReady.Values.Contains(false) || _playerHasMessages.Values.Contains(true)) ;
            string playerOrder = string.Join(',', _playerOrder);
            foreach (PlayerColor player in _clients.Keys)
            {
                WriteMessageToPlayer(player, "BuildingEnded," + playerOrder);
                _playerReady[player] = false;
            }
            Console.WriteLine("Building stage over, player order: ({0})", string.Join(',', _playerOrder));
            Thread.Sleep(5000);
            BeginFlightStage();
        }

        private void BeginFlightStage()
        {
            while (_playerReady.Values.Contains(false)) ;
            _stage = ServerStage.Flight;

            StringBuilder playerAttributes = new StringBuilder("FlightBegun," + _clients.Count);
            foreach(PlayerColor player in _clients.Keys)
            {
                _playerReady[player] = false;
                playerAttributes.Append("," + player.ToString() + _playerAttributes[player].ToString());
            }

            foreach(PlayerColor player in _clients.Keys)
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
            while (_clients[player].Connected)
            {
                _playerHasMessages[player] = false;
                message = ReadMessageFromPlayer(player);
                parts = message.Split(',');
                _playerHasMessages[player] = true;
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
                        Console.WriteLine("Unrecognized client message from {0}: {1}.", player, message);
                        break;
                }
            }
        }

        private void StartFlightStageResolve(PlayerColor player, string[] parts)
        {
            PlayerAttributes attributes = new PlayerAttributes
            {
                Firepower = int.Parse(parts[1]),
                Enginepower = int.Parse(parts[2]),
                CrewCount = int.Parse(parts[3]),
                StorageSize = int.Parse(parts[4]),
                Batteries = int.Parse(parts[5])
            };
            _playerAttributes[player] = attributes;
            _playerReady[player] = true;
        }

        private void ToggleReadyResolve(PlayerColor player)
        {
            _playerReady[player] = !_playerReady[player];
            if (_stage == ServerStage.Build)
            {
                _orderSemaphore.WaitOne();
                if (_playerReady[player])
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
            string announcement = "PlayerReadied," + player.ToString();
            WriteMessageToPlayer(player, response);
            foreach (PlayerColor key in _clients.Keys)
            {
                if (player != key)
                {
                    WriteMessageToPlayer(key, announcement);
                }
            }
        }

        private void PutBackPartResolve(PlayerColor player, string[] parts)
        {
            int ind1 = int.Parse(parts[1]);
            int ind2 = int.Parse(parts[2]);
            _partAvailable[ind1, ind2].Item1.WaitOne();

            if (_partAvailable[ind1, ind2].Item2)
            {
                WriteMessageToPlayer(player, "PutBackPartConfirm");
            }
            else
            {
                _partAvailable[ind1, ind2] = (_partAvailable[ind1, ind2].Item1, true);
                string response = "PutBackPartConfirm";
                string announcement = "PartPutBack," + parts[1] + "," + parts[2] + "," + _parts[ind1, ind2].ToString();
                WriteMessageToPlayer(player, response);
                foreach (PlayerColor key in _clients.Keys)
                {
                    if (player != key)
                    {
                        WriteMessageToPlayer(key, announcement);
                    }
                }
            }
            _partAvailable[ind1, ind2].Item1.Release();
        }

        private void PickPartResolve(PlayerColor player, string[] parts)
        {
            int ind1 = int.Parse(parts[1]);
            int ind2 = int.Parse(parts[2]);
            _partAvailable[ind1, ind2].Item1.WaitOne();

            if (!_partAvailable[ind1, ind2].Item2)
            {
                WriteMessageToPlayer(player, "PickPartResult,null");
            }
            else
            {
                _partAvailable[ind1, ind2] = (_partAvailable[ind1, ind2].Item1, false);
                string response = "PickPartResult," + _parts[ind1, ind2].ToString();
                string announcement = "PartTaken," + ind1.ToString() + "," + ind2.ToString();
                WriteMessageToPlayer(player, response);
                foreach (PlayerColor key in _clients.Keys)
                {
                    if (player != key)
                    {
                        WriteMessageToPlayer(key, announcement);
                    }
                }
            }
            _partAvailable[ind1, ind2].Item1.Release();
        }

        private string ReadMessageFromPlayer(PlayerColor player)
        {
            NetworkStream ns = _clients[player].GetStream();
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
            NetworkStream ns = _clients[player].GetStream();
            byte[] msg = Encoding.ASCII.GetBytes(message + "#");
            ns.Write(msg, 0, msg.Length);
        }

        private void ShuffleParts()
        {
            List<Part> parts = new List<Part>();
            string line;
            StreamReader sr = new StreamReader(_partPath);
            while((line = sr.ReadLine()) != null)
            {
                parts.Add(line.ToPart());
            }
            sr.Close();

            Random rng = new Random();
            int n = parts.Count;
            while(n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                Part value = parts[k];
                parts[k] = parts[n];
                parts[n] = value;
            }

            for(int i = 0; i < _parts.GetLength(0); ++i)
            {
                for(int j = 0; j < _parts.GetLength(1); ++j)
                {
                    _parts[i, j] = parts[i * _parts.GetLength(1) + j];
                }
            }
        }

        #endregion
    }
}
