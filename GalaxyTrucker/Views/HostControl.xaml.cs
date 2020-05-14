using GalaxyTrucker.Model;
using System;
using System.Windows.Controls;

namespace GalaxyTrucker.Views
{
    /// <summary>
    /// Interaction logic for HostControl.xaml
    /// </summary>
    public partial class HostControl : UserControl
    {
        public HostControl()
        {
            InitializeComponent();

            gameStageComboBox.ItemsSource = Enum.GetValues(typeof(GameStage));
        }
    }
}
