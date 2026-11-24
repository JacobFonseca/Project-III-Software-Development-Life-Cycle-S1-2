/*
 * File: DeviceTile.xaml.cs
 * Author: Jacob Fonseca
 * Project: HydroFlow SCADA
 * Task: HF-97
 *
 * Shows one device's latest reading and alert colour. All values come from its DashboardRow binding.
 */

using System.Windows.Controls;

namespace HydroFlow.UI
{
    public partial class DeviceTile : UserControl
    {
        /*
         * Purpose: Build the tile. MainWindow sets the DataContext to the device's row.
         */
        public DeviceTile()
        {
            InitializeComponent();
        }
    }
}
