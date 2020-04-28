using System;
using System.Windows;
using System.Windows.Controls;

namespace GalaxyTrucker.Views
{
    /// <summary>
    /// Interaction logic for RulesControl.xaml
    /// </summary>
    public partial class RulesControl : UserControl
    {
        public event EventHandler BackToMenu;

        public RulesControl()
        {
            InitializeComponent();
        }

        private void Menu_Click(object sender, RoutedEventArgs e)
        {
            BackToMenu?.Invoke(this, EventArgs.Empty);
        }
    }
}
