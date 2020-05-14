using System.Windows.Controls;

namespace GalaxyTrucker.Views
{
    /// <summary>
    /// Interaction logic for FlightControl.xaml
    /// </summary>
    public partial class FlightControl : UserControl
    {
        public FlightControl()
        {
            InitializeComponent();

            int[] shipGridHeaders = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            ShipGridColumnHeaders.ItemsSource = shipGridHeaders;
            ShipGridRowHeaders.ItemsSource = shipGridHeaders;
        }
    }
}
