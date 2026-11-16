/*
 * File: App.xaml.cs
 * Author: Jacob Fonseca
 * Project: HydroFlow SCADA
 * Task: HF-38
 *
 * Starts the device simulation and opens the monitoring window.
 */

using System.IO;
using System.Windows;
using HydroFlow.Core;
using HydroFlow.Devices;

namespace HydroFlow.UI
{
    public partial class App : Application
    {
        /*
         * Purpose: Create the simulation from the copied data files and show the monitoring window.
         */
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // The UI project copies data\*.txt beside the .exe, so files are resolved from there.
            string dataFolder = Path.Combine(AppContext.BaseDirectory, "data");
            var simulation = new Simulation(file => new LoopingFileReader(Path.Combine(dataFolder, file)));

            var window = new MainWindow();
            window.Start(simulation);
            window.Show();
        }
    }

}
