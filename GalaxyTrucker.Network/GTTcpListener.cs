using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private readonly int _maxPlayerCount = Enum.GetValues(typeof(PlayerColor)).Length;

        private readonly TcpListener _listener;

        private readonly ConcurrentDictionary<PlayerColor, TcpClient> _clients;
        private readonly ConcurrentDictionary<PlayerColor, bool> _playerReady;

        private volatile ServerStage _stage;

        private Semaphore _orderSemaphore;
        private List<PlayerColor> _playerOrder;

        public IEnumerable<PlayerColor> PlayerOrder
            => _playerOrder.AsReadOnly();

        public IEnumerable<PlayerColor> NotReadyPlayers
            => _playerReady.Keys.Where(p => !_playerReady[p]);

        //Semaphore: limit accessibility to one client at a time
        //bool: whether the tile is not already taken
        private (Semaphore, bool)[,] _tiles;

        public GTTcpListener(IPEndPoint endPonint)
        {
            _listener = new TcpListener(endPonint);
            _clients = new ConcurrentDictionary<PlayerColor, TcpClient>();
            _playerReady = new ConcurrentDictionary<PlayerColor, bool>();
        }

        public void Start()
        {
            try
            {
                _listener.Start(_maxPlayerCount);
                _stage = ServerStage.Lobby;

                new Task(() => WatchReadyToggles()).Start();
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

                        WriteMessageToStream(assignedColor, assignedColor.ToString());
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

        private void WatchReadyToggles()
        {
            List<Task> activeTasks = new List<Task>();
            while(_stage == ServerStage.Lobby)
            {
                foreach(Task t in activeTasks)
                {
                    t.Wait();
                }
                activeTasks.RemoveAll(t => t.IsCompleted);
                foreach(PlayerColor key in _clients.Keys)
                {
                    if (_clients[key].GetStream().DataAvailable)
                    {
                        Task t = new Task(() => ToggleReadyToBuild(key));
                        activeTasks.Add(t);
                        t.Start();
                    }
                }
            }
        }

        private void ToggleReadyToBuild(PlayerColor player)
        {
            string msg = ReadMessageFromStream(player);
            if(msg != "ToggleReadyToBuild")
            {
                Console.WriteLine("Unexpected message from client {0}: {1}", player, msg);
                return;
            }
            _playerReady[player] = !_playerReady[player];

            WriteMessageToStream(player, "Confirm");

            Console.WriteLine("Server: {0} toggled ready state to {1}", player, _playerReady[player]);
        }

        public void StartBuildStage(int ind1, int ind2)
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
            Console.WriteLine("StartBuildStage");
            _stage = ServerStage.Build;
            _tiles = new (Semaphore, bool)[ind1, ind2];
            for (int i = 0; i < ind1; ++i)
            {
                for (int j = 0; j < ind2; ++j)
                {
                    _tiles[i, j] = (new Semaphore(1, 1), true);
                }
            }
            _orderSemaphore = new Semaphore(1, 1);
            _playerOrder = new List<PlayerColor>();

            //Send Start message to all clients
            Parallel.ForEach(_clients.Keys, key =>
            {
                _playerReady[key] = false;
                WriteMessageToStream(key, "Start");
            });

            BuildStage();
        }

        private void BuildStage()
        {
            List<Task> activeTasks = new List<Task>();

            while(_playerReady.Values.Contains(false) || activeTasks.Any())
            {
                foreach(Task t in activeTasks)
                {
                    t.Wait();
                }
                activeTasks.RemoveAll(t => t.IsCompleted);
                foreach(PlayerColor key in _clients.Keys)
                {
                    if (_clients[key].GetStream().DataAvailable)
                    {
                        Task t = new Task(() => ManageBuildMessage(key));
                        activeTasks.Add(t);
                        t.Start();
                    }
                }
            }
            Console.WriteLine("Building stage over, player order: ({0})", string.Join(',', _playerOrder));
            Console.WriteLine("Flight stage begins in 5 seconds...");
            Thread.Sleep(5000);
            BeginFlightStage();
        }

        private void BeginFlightStage()
        {
            if (_playerReady.Values.Contains(false))
            {
                Console.WriteLine("Not all players are ready, FLightStage not started.");
                return;
            }
            _stage = ServerStage.Flight;
            Parallel.ForEach(_clients.Keys, key =>
            {
                _playerReady[key] = false;
                WriteMessageToStream(key, "Start");
            });

            FlightStage();
        }

        private void FlightStage()
        {
            Console.WriteLine("FlightStage started");
        }

        private void ManageBuildMessage(PlayerColor player)
        {
            string message = ReadMessageFromStream(player);
            string[] splits = message.Split(',');

            //Picking part
            if(splits.Length == 2)
            {
                int ind1 = int.Parse(splits[0]);
                int ind2 = int.Parse(splits[1]);
                
                _tiles[ind1, ind2].Item1.WaitOne();
                bool value = _tiles[ind1, ind2].Item2;
                _tiles[ind1, ind2] = (_tiles[ind1, ind2].Item1, false);
                _tiles[ind1, ind2].Item1.Release();


                if (_playerReady[player])
                {
                    Console.WriteLine("{0} player not shown part due to being finished", player);
                    value = false;
                }
                else
                {
                    Console.WriteLine("{0} player picked part at ({1},{2}) with result: {3}", player, ind1, ind2, value);
                }
                WriteMessageToStream(player, value.ToString());
            }
            //Putting back part
            else if(splits.Length == 3)
            {
                int ind1 = int.Parse(splits[1]);
                int ind2 = int.Parse(splits[2]);
                
                _tiles[ind1, ind2].Item1.WaitOne();
                _tiles[ind1, ind2] = (_tiles[ind1, ind2].Item1, true);
                _tiles[ind1, ind2].Item1.Release();

                Console.WriteLine("{0} player put back part at ({1},{2})", player, ind1, ind2);

                WriteMessageToStream(player, "Confirm");
            }
            //Toggling ready state
            else
            {
                if(message != "ToggleReadyToFly")
                {
                    Console.WriteLine("Unexpected message from client {0}: {1}", player, message);
                    return;
                }
                _playerReady[player] = !_playerReady[player];
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
                Console.WriteLine("{0} player changed ready status to {1}", player, _playerReady[player]);
                WriteMessageToStream(player, "Confirm");
            }
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

        private string ReadMessageFromStream(PlayerColor player)
        {
            NetworkStream ns = _clients[player].GetStream();
            byte[] buffer = new byte[128];
            int bytes = ns.Read(buffer);
            string message = Encoding.ASCII.GetString(buffer, 0, bytes);
            return message;
        }

        private void WriteMessageToStream(PlayerColor player, string message)
        {
            NetworkStream ns = _clients[player].GetStream();
            byte[] msg = Encoding.ASCII.GetBytes(message);
            ns.Write(msg, 0, msg.Length);
        }
    }
}
