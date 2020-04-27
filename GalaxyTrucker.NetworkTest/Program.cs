using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace GalaxyTrucker.NetworkTest
{
    class Program
    {
        static void Main()
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).First();
            IPEndPoint endPoint = new IPEndPoint(ipAddress, 11000);

            GTTcpListener listener = new GTTcpListener(endPoint);
            /*GTTcpClient client1 = new GTTcpClient();
            GTTcpClient client2 = new GTTcpClient();
            GTTcpClient client3 = new GTTcpClient();
            GTTcpClient client4 = new GTTcpClient();*/

            new Thread(() => listener.Start()).Start();

            /*client1.Connect(endPoint, "client1");
            client2.Connect(endPoint, "client2");
            client3.Connect(endPoint, "client3");
            client4.Connect(endPoint, "client4");
            client1.ToggleReady(ServerStage.Lobby);
            client2.ToggleReady(ServerStage.Lobby);
            client3.ToggleReady(ServerStage.Lobby);
            client4.ToggleReady(ServerStage.Lobby);
            client1.ToggleReady(ServerStage.Lobby);
            client1.ToggleReady(ServerStage.Lobby);

            Thread.Sleep(500);

            new Thread(() => listener.StartBuildStage()).Start();

            Thread.Sleep(500);

            client1.PickPart(5, 6);
            client1.PickPart(5, 5);
            client1.PickPart(5, 4);
            client1.PickPart(5, 3);
            client1.PickPart(5, 2);
            client1.PickPart(5, 1);
            client1.PickPart(5, 0);
            client1.PickPart(5, 7);
            client4.PickPart(6, 1);
            client3.PickPart(2, 1);
            client1.ToggleReady(ServerStage.Build);
            client2.PickPart(1, 2);
            client3.PickPart(1, 2);
            client3.PickPart(1, 2);
            client4.PickPart(1, 2);
            client4.PickPart(1, 2);
            client4.PickPart(1, 2);
            client2.PickPart(1, 2);
            client1.PutBackPart(5, 6);
            client2.PutBackPart(5, 4);
            client2.PutBackPart(5, 0);
            client3.PutBackPart(5, 2);
            client3.PutBackPart(1, 2);
            client4.PutBackPart(1, 2);
            client1.PutBackPart(1, 2);
            client4.PutBackPart(1, 2);
            client3.PutBackPart(2, 1);
            client2.PutBackPart(1, 2);
            client4.PutBackPart(1, 2);
            client4.PutBackPart(1, 2);
            client1.PutBackPart(6, 1);
            client2.PutBackPart(1, 2);
            client3.PutBackPart(1, 2);
            client2.PutBackPart(1, 2);
            client4.PutBackPart(1, 2);
            client1.PutBackPart(1, 2);
            client1.PutBackPart(1, 2);
            client4.PutBackPart(1, 2);
            client1.PutBackPart(1, 2);
            client3.PutBackPart(1, 2);
            client3.PickPart(1, 2);
            client4.PickPart(1, 2);
            client4.PickPart(1, 2);
            client2.PickPart(1, 2);
            client2.PickPart(1, 2);
            client3.ToggleReady(ServerStage.Build);
            client4.ToggleReady(ServerStage.Build);
            client1.PickPart(3, 0);
            client2.ToggleReady(ServerStage.Build);

            Thread.Sleep(1000);

            client1.StartFlightStage(1, 1, 1, 1, 1);
            client2.StartFlightStage(1, 1, 1, 1, 1);
            client3.StartFlightStage(1, 1, 1, 1, 1);
            client4.StartFlightStage(1, 1, 1, 1, 1);*/

            Thread.Sleep(1000);
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
