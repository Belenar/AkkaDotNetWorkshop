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
            // TODO: finish the implementation
        }

        public static Props CreateProps(Guid deviceId)
        {
            return Props.Create<DeviceActor>(deviceId);
        }
    }
}