using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace HydroFlow.Core;

public class CommandInvoker
{
    private readonly IMessageBus _bus;
    private readonly List<ICommand> _history = new();

    public CommandInvoker(IMessageBus bus)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
    }

    public IReadOnlyList<ICommand> History => _history;

    public void Run(ICommand command)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        command.Execute();              // if the actuator rejects it, nothing below runs
        _history.Add(command);

        _bus.Publish(new TelemetryPacket(
            command.DeviceId, "Command", 0, "",
            DateTime.UtcNow, DeviceStatus.Normal, command.Description));
    }
}
