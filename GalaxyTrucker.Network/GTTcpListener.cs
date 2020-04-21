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

        ConcurrentDictionary<PlayerColor, TcpClient> _clients;

        private ServerStage _stage;

        public GTTcpListener(IPEndPoint endPonint)
        {
            _listener = new TcpListener(endPonint);
            _clients = new ConcurrentDictionary<PlayerColor, TcpClient>();
        }

        public void Start()
        {
            try
            {
                _listener.Start(_maxPlayerCount);
                _stage = ServerStage.Lobby;

                while(_stage == ServerStage.Lobby && _clients.Count < _maxPlayerCount)
                {
                    if (_listener.Pending())
                    {
                        TcpClient client = _listener.AcceptTcpClient();
                        PlayerColor assignedColor = Enum.GetValues(typeof(PlayerColor)).Cast<PlayerColor>()
                            .Where(color => !_clients.ContainsKey(color)).First();
                        _clients[assignedColor] = client;
                        NetworkStream stream = client.GetStream();

                        byte[] msg = Encoding.ASCII.GetBytes(assignedColor.ToString());
                        stream.Write(msg, 0, msg.Length);

                        if (_clients.Count == _maxPlayerCount)
                        {
                            _stage = ServerStage.Build;
                        }
                    }
                }
                StartBuildStage();
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
            _stage = ServerStage.Build;
            List<Thread> playerConfirms = new List<Thread>();
            foreach(PlayerColor key in _clients.Keys)
            {
                Thread t = new Thread(() => GetConfirm(key));
                playerConfirms.Add(t);
                t.Start();
            }

            foreach(Thread t in playerConfirms)
            {
                t.Join();
            }

            foreach(PlayerColor key in _clients.Keys)
            {
                new Thread(() => SendStartToPlayer(key)).Start();
            }

            BuildStage();
        }

        private void BuildStage()
        {
            ConcurrentDictionary<PlayerColor, bool> playersFinished = new ConcurrentDictionary<PlayerColor, bool>();
            foreach(PlayerColor key in _clients.Keys)
            {
                playersFinished[key] = false;
            }

            while(playersFinished.Values.Contains(false))
            {
                foreach(PlayerColor key in _clients.Keys)
                {
                    if (_clients[key].GetStream().DataAvailable)
                    {
                        new Thread(() => ManageBuildMessage(key, playersFinished)).Start();
                    }
                }
            }
            Console.WriteLine("Buildstage over");
        }

        private void ManageBuildMessage(PlayerColor player, ConcurrentDictionary<PlayerColor, bool> playersFinished)
        {
            NetworkStream ns = _clients[player].GetStream();
            byte[] buffer = new byte[128];
            int bytes = ns.Read(buffer);
            string message = Encoding.ASCII.GetString(buffer, 0, bytes);
            string[] splits = message.Split(',');

            //Picking part
            if(splits.Length == 2)
            {
                //TODO
                int ind1 = -1;
                int ind2 = -1;
                try { 
                    ind1 = int.Parse(splits[0]);
                    ind2 = int.Parse(splits[1]);
                }
                catch(FormatException e)
                {
                    Console.WriteLine("FormatException {0}", e);
                }
                bool value = true;
                if(ind1 < 0 || ind2 < 0)
                {
                    value = false;
                }
                if (!playersFinished[player])
                {
                    Console.WriteLine("{0} player not shown part due to being finished", player);
                    value = false;
                }
                else
                {
                    Console.WriteLine("{0} player picked part at ({1},{2})", player, ind1, ind2);
                }
                byte[] msg = Encoding.ASCII.GetBytes(value.ToString());
                ns.Write(msg, 0, msg.Length);
            }
            //Toggling finished state
            else
            {
                if(message != "ToggleFinished")
                {
                    Console.WriteLine("Unexpected message from client {0}: {1}", player, message);
                    return;
                }
                playersFinished[player] = !playersFinished[player];
                Console.WriteLine("{0} player changed ready status to {1}", player, playersFinished[player]);
            }
        }

        private void SendStartToPlayer(PlayerColor player)
        {
            NetworkStream stream = _clients[player].GetStream();
            byte[] start = Encoding.ASCII.GetBytes("Start");
            stream.Write(start);
        }

        private void GetConfirm(PlayerColor player)
        {
            NetworkStream stream = _clients[player].GetStream();
            byte[] notification = Encoding.ASCII.GetBytes("Requesting confirmation");

            stream.Write(notification, 0, notification.Length);

            byte[] buffer = new byte[128];
            int bytes = stream.Read(buffer);
            string message = Encoding.ASCII.GetString(buffer, 0, bytes);
            if (message != "Confirm")
            {
                Console.WriteLine("Unexpected confirmation message from {0} : {1}", player, message);
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
    }
}
