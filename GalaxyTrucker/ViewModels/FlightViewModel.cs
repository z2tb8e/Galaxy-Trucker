using GalaxyTrucker.Network;
using System;
using System.Collections.Generic;
using System.Text;

namespace GalaxyTrucker.ViewModels
{
    public class FlightViewModel : NotifyBase
    {
        private readonly GTTcpClient _client;
        private readonly PlayerListViewModel _playerList;

        public FlightViewModel(GTTcpClient client, PlayerListViewModel playerList)
        {
            _client = client;
            _playerList = playerList;

            _playerList.LostConnection += PlayerList_LostConnection;
        }

        private void PlayerList_LostConnection(object sender, EventArgs e)
        {
            UnsubscribeFromEvents();
        }

        private void UnsubscribeFromEvents()
        {
            _playerList.LostConnection -= PlayerList_LostConnection;
        }
    }
}
