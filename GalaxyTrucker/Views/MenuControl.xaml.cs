using System;
using System.Diagnostics;
using System.IO;
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
            try
            {
                string path = Path.GetTempFileName() + ".pdf";
                File.WriteAllBytes(path, Properties.Resources.rules);
                ProcessStartInfo startInfo = new ProcessStartInfo(path)
                {
                    UseShellExecute = true
                };
                Process.Start(startInfo);
            }
            catch (IOException)
            {
                MessageBox.Show("Nem sikerült a szabályfájlt létrehozni!");
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this).Close();
        }
    }
}
