using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GalaxyTrucker.Client.Model;

namespace GalaxyTrucker.Network
{
    /// <summary>
    /// Exception thrown when a server message indicates that the client is out of sync with it.
    /// </summary>
    public class OutOfSyncException : Exception{}

    public class GTTcpClient
    {

        #region fields

        private readonly TcpClient _client;

        private readonly IPEndPoint _endPoint;
        private ServerStage _stage;
        private PlayerColor _color;
        private NetworkStream _stream;

        #endregion

        #region properties

        public bool IsReady { get; private set; }

        #endregion

        #region events

        public event EventHandler<BuildingBegunEventArgs> BuildingBegun;

        public event EventHandler<BuildingEndedEventArgs> BuildingEnded;

        public event EventHandler<FlightBegunEventArgs> FlightBegun;

        public event EventHandler<PartPickedEventArgs> PartPicked;

        public event EventHandler<PartTakenEventArgs> PartTaken;

        public event EventHandler<PartPutBackEventArgs> PartPutBack;

        public event EventHandler<PlayerReadiedEventArgs> PlayerReadied;

        #endregion

        public GTTcpClient(IPEndPoint endPoint)
        {
            IsReady = false;
            _stage = ServerStage.Lobby;
            _client = new TcpClient();
            _endPoint = endPoint;
        }

        #region public methods

        public void Connect()
        {
            try
            {
                _client.Connect(_endPoint);
                _stream = _client.GetStream();

                string msg = ReadMessageFromServer();

                _color = Enum.Parse<PlayerColor>(msg);
                Console.WriteLine("Assigned color: {0}", _color);
                Task.Factory.StartNew(() => HandleServerMessages(), TaskCreationOptions.LongRunning);

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

        public void ToggleReady(ServerStage currentStage)
        {
            if(_stage != currentStage)
            {
                throw new InvalidOperationException();
            }

            WriteMessageToServer("ToggleReady");

            IsReady = !IsReady;
        }

        public void StartFlightStage(int firepower, int enginepower, int crewCount, int storageSize, int batteries)
        {
            if(_stage != ServerStage.Build || IsReady)
            {
                throw new InvalidOperationException();
            }

            WriteMessageToServer("StartFlightStage," + firepower + "," + enginepower + "," + crewCount + "," + storageSize + "," + batteries);
            IsReady = true;
        }

        public void PutBackPart(int ind1, int ind2)
        {
            if(_stage != ServerStage.Build)
            {
                throw new InvalidOperationException();
            }

            string msg = "PutBackPart," + ind1 + "," + ind2;
            WriteMessageToServer(msg);
        }

        public void PickPart(int ind1, int ind2)
        {
            if (_stage != ServerStage.Build)
            {
                throw new InvalidOperationException();
            }

            string msg = "PickPart," + ind1 + "," + ind2;
            WriteMessageToServer(msg);
        }

        public void Close()
        {
            _stream.Close();
            _client.Close();
        }

        #endregion

        #region private methods

        private void HandleServerMessages()
        {
            string message;
            string[] parts;
            while (_client.Connected)
            {
                if (_stream.DataAvailable)
                {
                    message = ReadMessageFromServer();
                    //Console.WriteLine("{0} received message from server: {1}.", _color, message);
                    parts = message.Split(',');
                    switch (parts[0])
                    {
                        case "FlightBegun":
                            FlightBegunResolve(parts);
                            break;

                        case "BuildingBegun":
                            BuildingBegunResolve(parts);
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
                            PlayerToggledReady(parts);
                            break;

                        default:
                            Console.WriteLine("Unhandled server message: {0}", message);
                            break;
                    }
                }
            }
        }

        private void FlightBegunResolve(string[] parts)
        {
            if (!IsReady)
            {
                throw new OutOfSyncException();
            }
            IsReady = false;
            _stage = ServerStage.Flight;
            //Format: FlightBegun,PlayerNumber,Color,Firepower,Enginepower,CrewCount,StorageSize,Batteries
            Dictionary<PlayerColor, PlayerAttributes> playerAttributes = new Dictionary<PlayerColor, PlayerAttributes>();
            int playerNumber = int.Parse(parts[1]);
            for (int i = 0; i < playerNumber; ++i)
            {
                PlayerColor currentPlayer = Enum.Parse<PlayerColor>(parts[2 + i * 6]);
                PlayerAttributes attributes = new PlayerAttributes
                {
                    Firepower = int.Parse(parts[3 + i * 6]),
                    Enginepower = int.Parse(parts[4 + i * 6]),
                    CrewCount = int.Parse(parts[5 + i * 6]),
                    StorageSize = int.Parse(parts[6 + i * 6]),
                    Batteries = int.Parse(parts[7 + i * 6]),
                };
                playerAttributes[currentPlayer] = attributes;
            }
            FlightBegun?.Invoke(this, new FlightBegunEventArgs(playerAttributes));
            Console.WriteLine("Client {0}: FlightBegun", _color);
        }

        /// <summary>
        /// Method called when the server sends a message to signal that the building stage started
        /// </summary>
        /// <param name="parts"></param>
        private void BuildingBegunResolve(string[] parts)
        {
            if (!IsReady)
            {
                throw new OutOfSyncException();
            }
            IsReady = false;
            _stage = ServerStage.Build;
            List<PlayerColor> players = new List<PlayerColor>();
            for (int i = 1; i < parts.Length; ++i)
            {
                players.Add(Enum.Parse<PlayerColor>(parts[i]));
            }
            BuildingBegun?.Invoke(this, new BuildingBegunEventArgs(players));
        }

        /// <summary>
        /// Method called when the server sends a message to signal that the building stage ended
        /// </summary>
        /// <param name="parts"></param>
        private void BuildingEndedResolve(string[] parts)
        {
            if (!IsReady)
            {
                throw new OutOfSyncException();
            }
            IsReady = false;
            List<PlayerColor> turnOrder = new List<PlayerColor>();
            for (int i = 1; i < parts.Length; ++i)
            {
                turnOrder.Add(Enum.Parse<PlayerColor>(parts[i]));
            }
            BuildingEnded?.Invoke(this, new BuildingEndedEventArgs(turnOrder));
        }

        /// <summary>
        /// Method called when the server sends a message to tell the result the client picking a part earlier
        /// </summary>
        /// <param name="parts"></param>
        private void PickPartResultResolve(string[] parts)
        {
            if (_stage != ServerStage.Build)
            {
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
            if (_stage != ServerStage.Build)
            {
                throw new OutOfSyncException();
            }
        }

        /// <summary>
        /// Method called when the server sends a message to signal that another player put back a part
        /// </summary>
        /// <param name="parts"></param>
        private void PartPutBackResolve(string[] parts)
        {
            if (_stage != ServerStage.Build)
            {
                throw new OutOfSyncException();
            }
            int ind1 = int.Parse(parts[1]);
            int ind2 = int.Parse(parts[2]);
            Part putBack = parts[3].ToPart();
            PartPutBack?.Invoke(this, new PartPutBackEventArgs(ind1, ind2, putBack));
        }

        /// <summary>
        /// Method called when the server sends a message to signal that another player took a part
        /// </summary>
        /// <param name="parts"></param>
        private void PartTakenResolve(string[] parts)
        {
            if (_stage != ServerStage.Build)
            {
                throw new OutOfSyncException();
            }
            int ind1 = int.Parse(parts[1]);
            int ind2 = int.Parse(parts[2]);
            PartTaken?.Invoke(this, new PartTakenEventArgs(ind1, ind2));
        }

        /// <summary>
        /// Method called when the server sends a message signaling that another player toggled their ready state 
        /// </summary>
        /// <param name="parts"></param>
        private void PlayerToggledReady(string[] parts)
        {
            PlayerColor player = Enum.Parse<PlayerColor>(parts[1]);
            PlayerReadied?.Invoke(this, new PlayerReadiedEventArgs(player));
        }

        private string ReadMessageFromServer()
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

        private void WriteMessageToServer(string message)
        {
            if (!_stream.CanWrite)
            {
                throw new InvalidOperationException();
            }

            byte[] msg = Encoding.ASCII.GetBytes(message + "#");
            _stream.Write(msg, 0, msg.Length);
        }

        #endregion
    }
}
