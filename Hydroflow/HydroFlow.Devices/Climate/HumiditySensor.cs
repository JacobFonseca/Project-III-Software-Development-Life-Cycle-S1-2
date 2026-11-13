using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HydroFlow.Core;
namespace HydroFlow.Devices;

public class HumiditySensor : Sensor
{
    public HumiditySensor(string deviceId, string name, IDataSource source, IMessageBus bus)
        : base(deviceId, name, "%", source, bus) { }
}