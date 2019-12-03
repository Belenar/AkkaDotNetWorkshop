using System;
using System.Data.SqlClient;
using System.Linq;
using Akka.Actor;
using AkkaDotNet.SensorData.Shared.Database;
using AkkaDotNet.SensorData.Shared.Messages;
using Dapper;

namespace AkkaDotNet.SensorData.Shared.Actors.Device
{
    public class AlertsDbReaderActor : ReceiveActor
    {
        private readonly Guid _deviceId;

        public AlertsDbReaderActor(Guid deviceId)
        {
            _deviceId = deviceId;
            Receive<ReadAlertConfigurations>(HandleReadAlertConfigurations);
        }

        private void HandleReadAlertConfigurations(ReadAlertConfigurations obj)
        {
            // TODO: read alerts from DB and notify Parent
        }

        public static Props CreateProps(Guid deviceId)
        {
            return Props.Create<AlertsDbReaderActor>(deviceId);
        }
    }
}