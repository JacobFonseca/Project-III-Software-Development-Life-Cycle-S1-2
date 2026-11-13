using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HydroFlow.Core;
namespace HydroFlow.Devices;

public class ExhaustFanController : Actuator
{
    public ExhaustFanController(string deviceId, string name, IDataSource feedback, IMessageBus bus)
        : base(deviceId, name, "rpm", feedback, bus) { }
}