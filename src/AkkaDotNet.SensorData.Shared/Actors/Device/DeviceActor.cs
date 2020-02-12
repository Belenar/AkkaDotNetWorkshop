using System;
using Akka.Actor;
using AkkaDotNet.SensorData.Shared.Messages;

namespace AkkaDotNet.SensorData.Shared.Actors.Device
{
    public class DeviceActor : ReceiveActor
    {
        private readonly Guid _deviceId;

        public DeviceActor(Guid deviceId)
        {
            _deviceId = deviceId;
            // TODO  3: Create Child Actor for normalization
            // TODO  4: Forward MeterReadingReceived to this Actor
            // TODO  5: Add PersistenceActor
            // TODO  6: Forward NormalizedMeterReading
            // TODO  9: Add Alerts Actorfinish the implementation
            // TODO 10: Forward NormalizedMeterReading
        }

        public static Props CreateProps(Guid deviceId)
        {
            return Props.Create<DeviceActor>(deviceId);
        }
    }
}