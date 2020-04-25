using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

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

            client1.Connect();
            client2.Connect();
            client3.Connect();
            client4.Connect();
            client1.ToggleReady(ServerStage.Lobby);
            client2.ToggleReady(ServerStage.Lobby);
            client3.ToggleReady(ServerStage.Lobby);
            client4.ToggleReady(ServerStage.Lobby);
            client1.ToggleReady(ServerStage.Lobby);
            client1.ToggleReady(ServerStage.Lobby);

            Thread.Sleep(1000);

            new Thread(() => listener.StartBuildStage()).Start();

            Thread.Sleep(1000);

            client1.PickPart(5, 6);
            client4.PickPart(6, 1);
            client3.PickPart(2, 1);
            client2.PickPart(13, 2);
            client1.ToggleReady(ServerStage.Build);
            client2.PutBackPart(13, 2);
            client2.PickPart(1, 2);
            client3.ToggleReady(ServerStage.Build);
            client4.ToggleReady(ServerStage.Build);
            client1.PickPart(3, 0);
            client2.ToggleReady(ServerStage.Build);

            Thread.Sleep(1000);

            client1.StartFlightStage(1, 1, 1, 1, 1);
            client2.StartFlightStage(1, 1, 1, 1, 1);
            client3.StartFlightStage(1, 1, 1, 1, 1);
            client4.StartFlightStage(1, 1, 1, 1, 1);

            /*client1.Close();
            client2.Close();
            client3.Close();
            listener.Close();*/
            /*
             * ÚJ FELÉPÍTÉS:
             *  Kliens
             *      - metódusokkal kéréseket küld
             *      - külön szálon figyeli és feldolgozza az üzeneteket, ezekről eventekkel értesíti a külső vezérlőt
             *          - esetleg az event kiváltásakor valami mezővel újra engedélyezi a korábbi metódushívást
             *              - ilyesmi mezővel a szemafor is nélkülözhető (bár talán mégse, mert nem akarjuk hogy egyszerre több üzenet is kimenjen)
             *                  - amennyiben több üzenet is mehet ki egyszerre, üzenetek közti tagolóelem itt is (legyen most #)
             * Szerver
             *      - kliesenként egy szálon üzeneteket figyeli a bejövő üzeneteket, amikre válaszol illetve értesít mindenki mást is rögtön
             *      - kimenő üzenetek végén tagoló karakterek (legyen most #)
             *      
             * ReadFromStream és WriteToStream már megcsinálva
             */
        }
    }
}
