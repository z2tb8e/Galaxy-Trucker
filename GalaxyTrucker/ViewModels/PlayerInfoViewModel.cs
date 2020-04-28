using GalaxyTrucker.Model;
using GalaxyTrucker.Network;

namespace GalaxyTrucker.ViewModels
{
    public class PlayerInfoViewModel : NotifyBase
    {
        private readonly PlayerInfo _info;

        public PlayerColor Color => _info.Color;

        public string Name => _info.Name;

        public bool IsReady
        {
            get
            {
                return _info.IsReady;
            }
            set
            {
                _info.IsReady = value;
                OnPropertyChanged();
            }
        }

        public bool IsFlying
        {
            get
            {
                return _info.IsFlying;
            }
            set
            {
                _info.IsFlying = value;
                OnPropertyChanged();
            }
        }

        public PlayerAttributes Attributes
        {
            get
            {
                return _info.Attributes;
            }
            set
            {
                if(_info.Attributes != value)
                {
                    _info.Attributes = value;
                    OnPropertyChanged();
                }
            }
        }

        public PlayerInfoViewModel(PlayerInfo info)
        {
            _info = info;
        }
    }
}
