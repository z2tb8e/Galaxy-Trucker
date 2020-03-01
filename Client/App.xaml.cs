using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Client.Model;
using Client.Model.PartTypes;
using Client.ViewModels;

namespace Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Part p = new LaserDouble(Connector.None, Connector.Universal, Connector.Double, Connector.Single);
            PartBuilder.GetPartImage(p);
        }
    }
}
