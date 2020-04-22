using System;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace GalaxyTrucker.Network
{
    class Program
    {
        static void Main()
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).First();
            IPEndPoint endPoint = new IPEndPoint(ipAddress, 11000);

            GTTcpListener listener = new GTTcpListener(endPoint);
            GTTcpClient client1 = new GTTcpClient(endPoint);
            GTTcpClient client2 = new GTTcpClient(endPoint);
            GTTcpClient client3 = new GTTcpClient(endPoint);
            GTTcpClient client4 = new GTTcpClient(endPoint);

            new Thread(() => listener.Start()).Start();
            new Thread(() => client1.Connect()).Start();
            new Thread(() => client2.Connect()).Start();
            new Thread(() => client3.Connect()).Start();
            new Thread(() => client4.Connect()).Start();
            new Thread(() => client1.ToggleReadyToBuild()).Start();
            new Thread(() => client2.ToggleReadyToBuild()).Start();
            new Thread(() => client1.ToggleReadyToBuild()).Start();
            new Thread(() => client4.ToggleReadyToBuild()).Start();
            new Thread(() => client1.ToggleReadyToBuild()).Start();
            new Thread(() => client3.ToggleReadyToBuild()).Start();

            Thread.Sleep(1000);

            new Thread(() => listener.StartBuildStage(20,20)).Start();

            Thread.Sleep(1000);

            new Thread(() => client1.PickPart(5, 6)).Start();
            new Thread(() => client4.PickPart(6, 1)).Start();
            new Thread(() => client3.PickPart(2, 1)).Start();
            new Thread(() => client2.PickPart(3, 15)).Start();
            new Thread(() => client1.ToggleReadyToFly()).Start();
            new Thread(() => client2.PutBackPart(3, 15)).Start();
            new Thread(() => client2.PickPart(1,2)).Start();
            new Thread(() => client3.ToggleReadyToFly()).Start();
            new Thread(() => client1.ToggleReadyToFly()).Start();
            new Thread(() => client4.ToggleReadyToFly()).Start();
            new Thread(() => client1.PickPart(3, 15)).Start();
            new Thread(() => client1.ToggleReadyToFly()).Start();
            new Thread(() => client2.ToggleReadyToFly()).Start();


            /*client1.Close();
            client2.Close();
            client3.Close();
            listener.Close();*/
        }
    }
}
