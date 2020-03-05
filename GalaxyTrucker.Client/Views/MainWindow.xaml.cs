using GalaxyTrucker.Client.Model;
using GalaxyTrucker.Client.Model.PartTypes;
using GalaxyTrucker.Client.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GalaxyTrucker.Client.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MessageBox.Show(Directory.GetCurrentDirectory());
            Part p = new LaserDouble(Connector.None, Connector.Universal, Connector.Double, Connector.Single);
            PartBuilder.GetPartImage(p);
        }
    }
}
