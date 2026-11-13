using HydroFlow.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hydroflow.Core
{
    public class WaterSolenoidValve
    {
        private List<double> valveStates = new();
        private int currentIndex = 0;
        private readonly IMessageBus bus;

        public string ValveId { get; }

        public WaterSolenoidValve(
            string valveId,
            string filePath,
            IMessageBus messageBus)
        {
            if (string.IsNullOrWhiteSpace(valveId))
                throw new ArgumentException("Valve ID is required.");

            ArgumentNullException.ThrowIfNull(messageBus);

            ValveId = valveId;
            bus = messageBus;

            foreach (string line in File.ReadAllLines(filePath))
            {
                string state = line.Trim().ToUpperInvariant();

                if (state == "OPEN")
                    valveStates.Add(1);

                if (state == "CLOSED")
                    valveStates.Add(0);
            }

            if (valveStates.Count == 0)
                throw new InvalidDataException(
                    "No valid valve states found.");
        }

        public double ReadState()
        {
            double state = valveStates[currentIndex];

            currentIndex++;

            if (currentIndex >= valveStates.Count)
                currentIndex = 0;

            return state;
        }


        public void PublishState()
        {
            double state = ReadState();

            TelemetryPacket packet = new TelemetryPacket(
                ValveId,
                "Actuator",
                state,
                "State",
                DateTime.Now,
                DeviceStatus.Normal
            );

            bus.Publish(packet);
        }


    }
}
