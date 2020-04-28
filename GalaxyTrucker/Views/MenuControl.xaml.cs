using System;
using System.Windows;
using System.Windows.Controls;

namespace GalaxyTrucker.Views
{
    /// <summary>
    /// Interaction logic for MenuControl.xaml
    /// </summary>
    public partial class MenuControl : UserControl
    {
        public event EventHandler ConnectClick;
        public event EventHandler HostClick;
        public event EventHandler RulesClick;

        public MenuControl()
        {
            InitializeComponent();
        }

        private void Host_Click(object sender, RoutedEventArgs e)
        {
            HostClick?.Invoke(this, EventArgs.Empty);
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            ConnectClick?.Invoke(this, EventArgs.Empty);
        }

        private void Rules_Click(object sender, RoutedEventArgs e)
        {
            RulesClick?.Invoke(this, EventArgs.Empty);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this).Close();
        }
    }
}
