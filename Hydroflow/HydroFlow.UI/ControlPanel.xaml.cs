using System;
using System.Windows;
using System.Windows.Controls;
using HydroFlow.Core;
using HydroFlow.Devices;
using HydroFlow.Controller;

namespace HydroFlow.UI;

public partial class ControlPanel : UserControl
{
    private ActuatorControlService? _controlService;

    public ControlPanel()
    {
        InitializeComponent();
    }

    public void Initialize(Simulation simulation)
    {
        ArgumentNullException.ThrowIfNull(simulation);

        if (_controlService != null)
            return;

        _controlService = new ActuatorControlService(
            simulation.Actuators,
            simulation.Bus);

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
        if (_controlService == null)
            return;

        if (sender is not Button button ||
            button.DataContext is not Actuator actuator)
            return;

        try
        {
            CommandMessage.Text = _controlService.Set(
                actuator.DeviceId, on);

            ActuatorGrid.Items.Refresh();
        }
        catch (ArgumentException ex)
        {
            CommandMessage.Text = ex.Message;
        }
    }
}