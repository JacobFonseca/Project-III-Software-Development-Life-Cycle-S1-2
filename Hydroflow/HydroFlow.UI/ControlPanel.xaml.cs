using System;
using System.Windows;
using System.Windows.Controls;
using HydroFlow.Core;
using HydroFlow.Devices;

namespace HydroFlow.UI;

public partial class ControlPanel : UserControl
{
    private CommandInvoker? _invoker;

    public ControlPanel()
    {
        InitializeComponent();
    }

    public void Initialize(Simulation simulation)
    {
        ArgumentNullException.ThrowIfNull(simulation);

        if (_invoker != null)
            return;

        _invoker = new CommandInvoker(simulation.Bus);

        ActuatorGrid.ItemsSource = simulation.Actuators;

        simulation.Bus.Subscribe(OnPacketReceived);
    }

    private void OnPacketReceived(TelemetryPacket packet)
    {
        if (packet.Type != "Actuator")
            return;

        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.InvokeAsync(() => OnPacketReceived(packet));
            return;
        }

        ActuatorGrid.Items.Refresh();
    }

    private void OnButton_Click(object sender, RoutedEventArgs e)
    {
        ExecuteCommand(sender, true);
    }

    private void OffButton_Click(object sender, RoutedEventArgs e)
    {
        ExecuteCommand(sender, false);
    }

    private void ExecuteCommand(object sender, bool on)
    {
        if (_invoker == null)
            return;

        if (sender is not Button button ||
            button.DataContext is not Actuator actuator)
            return;

        try
        {
            double setting = on ? GetOnSetting(actuator) : 0;

            var command = new ActuatorCommand(
                actuator, on, setting);

            _invoker.Run(command);

            ActuatorGrid.Items.Refresh();

            CommandMessage.Text = command.Description;
        }
        catch (ArgumentException ex)
        {
            CommandMessage.Text = ex.Message;
        }
    }

    private static double GetOnSetting(Actuator actuator)
    {
        return actuator.DeviceId switch
        {
            "FAN-01" => 1000,
            "VLV-01" => 1,
            "PHP-01" => 50,
            "NUT-01" => 50,
            "LED-01" => 80,
            _ => throw new ArgumentException(
                "Unknown actuator.")
        };
    }
}