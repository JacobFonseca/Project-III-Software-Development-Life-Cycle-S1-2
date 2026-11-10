using Hydroflow.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hydroflow.Tests
{
    internal class SensorTests
    {

        [TestClass]
        public class SensoringTests
        {
            class TestSensor : Sensor
            {
                public TestSensor(string id, string value, string unit)
                    : base(id, value, unit)
                {
                }
            }

            [TestMethod]
            public void Sensor_StoresId()
            {
                TestSensor sensor = new TestSensor("S1", "25", "C");

                Assert.AreEqual("S1", sensor.SensorId);
            }

            [TestMethod]
            public void Sensor_StoresValue()
            {
                TestSensor sensor = new TestSensor("S1", "25", "C");

                Assert.AreEqual("25", sensor.SensorValue);
            }

            [TestMethod]
            public void Sensor_StoresUnit()
            {
                TestSensor sensor = new TestSensor("S1", "25", "C");

                Assert.AreEqual("C", sensor.SensorUnit);
            }

        }

    }

}