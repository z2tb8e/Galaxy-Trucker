using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GalaxyTrucker.Client.Model;

namespace GalaxyTrucker.Network
{
    public class GTTcpClient
    {
        private readonly TcpClient _client;

        private readonly IPEndPoint _endPoint;
        private readonly Semaphore _canSend;
        private ServerStage _stage;
        private PlayerColor _color;
        private NetworkStream _stream;

        public bool IsReady { get; private set; }

        public EventHandler BuildingBegun;

        public EventHandler BuildingEnded;

        public GTTcpClient(IPEndPoint endPoint)
        {
            IsReady = false;
            _canSend = new Semaphore(0, 1);
            _stage = ServerStage.Lobby;
            _client = new TcpClient();
            _endPoint = endPoint;
        }

        public void Connect()
        {
            try
            {
                _client.Connect(_endPoint);
                _stream = _client.GetStream();

                string msg = ReadMessageFromStream();

                _color = (PlayerColor)Enum.Parse(typeof(PlayerColor), msg);
                Console.WriteLine("Assigned color: {0}", _color);
                _canSend.Release();

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

        public void ToggleReadyToBuild()
        {
            _canSend.WaitOne();
            if(_stage != ServerStage.Lobby)
            {
                _canSend.Release();
                throw new InvalidOperationException();
            }

            WriteMessageToStream("ToggleReadyToBuild");

            IsReady = !IsReady;
            if (IsReady)
            {
                new Task(() => AwaitNextStageStart()).Start();
            }

            string response = ReadMessageFromStream();
            if(response != "Confirm")
            {
                Console.WriteLine("Unexpected server response: {0}", response);
            }
            else
            {
                Console.WriteLine("Client: {0} toggled ready state to {1}", _color, IsReady);
            }
            _canSend.Release();
        }

        public void ToggleReadyToFly()
        {
            _canSend.WaitOne();
            if (_stage != ServerStage.Build)
            {
                _canSend.Release();
                throw new InvalidOperationException();
            }
            WriteMessageToStream("ToggleReadyToFly");

            IsReady = !IsReady;
            if (IsReady)
            {
                new Task(() => AwaitNextStageStart()).Start();
            }

            string response = ReadMessageFromStream();
            if (response != "Confirm")
            {
                Console.WriteLine("Unexpected server response: {0}", response);
            }
            else
            {
                Console.WriteLine("Client: {0} toggled ready state to {1}", _color, IsReady);
            }
            _canSend.Release();
        }

        public void PutBackPart(int ind1, int ind2)
        {
            _canSend.WaitOne();
            if(_stage != ServerStage.Build)
            {
                _canSend.Release();
                throw new InvalidOperationException();
            }

            string msg = "PutBack," + ind1.ToString() + "," + ind2.ToString();
            WriteMessageToStream(msg);

            string response = ReadMessageFromStream();
            if (response != "Confirm")
            {
                Console.WriteLine("Unexpected server response: {0}", response);
            }
            _canSend.Release();
        }

        public bool PickPart(int ind1, int ind2)
        {
            _canSend.WaitOne();
            if (_stage != ServerStage.Build)
            {
                _canSend.Release();
                throw new InvalidOperationException();
            }

            string msg = ind1.ToString() + "," + ind2.ToString();
            WriteMessageToStream(msg);

            string response = ReadMessageFromStream();
            _canSend.Release();
            return bool.Parse(response);
        }

        public void Close()
        {
            _canSend.WaitOne();
            _stream.Close();
            _client.Close();
        }

        private void AwaitNextStageStart()
        {
            while (IsReady)
            {
                if (_stream.DataAvailable)
                {
                    _canSend.WaitOne();
                    string msg = ReadMessageFromStream();

                    if(msg != "Start")
                    {
                        Console.WriteLine("Unexpected server message: {0}", msg);
                    }
                    else
                    {
                        if(_stage == ServerStage.Lobby)
                        {
                            BuildingBegun?.Invoke(this, EventArgs.Empty);
                        }
                        else if(_stage == ServerStage.Build)
                        {
                            BuildingEnded?.Invoke(this, EventArgs.Empty);
                        }
                        IsReady = false;
                        _stage = (ServerStage)((int)_stage + 1);
                    }
                    _canSend.Release();
                }
            }
        }

        private string ReadMessageFromStream()
        {
            byte[] buffer = new byte[128];
            int bytes = _stream.Read(buffer);
            string message = Encoding.ASCII.GetString(buffer, 0, bytes);
            return message;
        }

        private void WriteMessageToStream(string message)
        {
            if (!_stream.CanWrite)
            {
                throw new InvalidOperationException();
            }

            byte[] msg = Encoding.ASCII.GetBytes(message);
            _stream.Write(msg, 0, msg.Length);
        }
    }
}
