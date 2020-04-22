using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using GalaxyTrucker.Client.Model;

namespace GalaxyTrucker.Network
{
    public class GTTcpClient
    {
        private readonly TcpClient _client;

        private readonly IPEndPoint _endPoint;
        private ServerStage _stage;
        private PlayerColor _color;
        private NetworkStream _stream;
        private Semaphore _canSend;

        public GTTcpClient(IPEndPoint endPoint)
        {
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

                byte[] buffer = new byte[128];
                int bytes = _stream.Read(buffer, 0, buffer.Length);
                string msg = Encoding.ASCII.GetString(buffer, 0, bytes);

                _color = (PlayerColor)Enum.Parse(typeof(PlayerColor), msg);
                Console.WriteLine("Assigned color: {0}", _color);

                //Wait build stage start confirmation request
                buffer = new byte[128];
                bytes = _stream.Read(buffer);
                msg = Encoding.ASCII.GetString(buffer, 0, bytes);
                if(msg != "Requesting confirmation")
                {
                    Console.WriteLine("Unexpected server message: {0}", msg);
                }

                byte[] response = Encoding.ASCII.GetBytes("Confirm");
                _stream.Write(response, 0, response.Length);

                //Wait build stage start signal
                buffer = new byte[128];
                bytes = _stream.Read(buffer);
                msg = Encoding.ASCII.GetString(buffer, 0, bytes);
                if(msg != "Start")
                {
                    Console.WriteLine("Unexpected server message: {0}", msg);
                }
                _stage = ServerStage.Build;
                Console.WriteLine("{0} player begins building", _color);
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

        public bool PickPart(int ind1, int ind2)
        {
            _canSend.WaitOne();
            if (_stage != ServerStage.Build)
            {
                throw new InvalidOperationException();
            }

            byte[] msg = Encoding.ASCII.GetBytes(ind1.ToString() + "," + ind2.ToString());
            _stream.Write(msg, 0, msg.Length);

            byte[] buffer = new byte[128];
            int bytes = _stream.Read(buffer);
            string response = Encoding.ASCII.GetString(buffer, 0, bytes);
            _canSend.Release();
            return bool.Parse(response);
        }

        public void ToggleFinishedBuilding()
        {
            _canSend.WaitOne();
            if (_stage != ServerStage.Build && _stage != ServerStage.Flight)
            {
                throw new InvalidOperationException();
            }
            byte[] msg = Encoding.ASCII.GetBytes("ToggleFinished");
            _stream.Write(msg, 0, msg.Length);

            byte[] buffer = new byte[128];
            int bytes = _stream.Read(buffer);
            string response = Encoding.ASCII.GetString(buffer, 0, bytes);
            if(response != "Confirm")
            {
                Console.WriteLine("Unexpected server message: {0}", response);
            }
            _canSend.Release();
        }

        public void Close()
        {
            _client.GetStream().Close();
            _client.Close();
        }
    }
}
