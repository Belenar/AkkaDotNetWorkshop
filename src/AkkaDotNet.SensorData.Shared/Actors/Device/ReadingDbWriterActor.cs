using System;
using System.Collections.Immutable;
using System.Data.SqlClient;
using System.Linq;
using Akka.Actor;
using AkkaDotNet.SensorData.Shared.Database;
using AkkaDotNet.SensorData.Shared.Messages;
using Dapper;

namespace AkkaDotNet.SensorData.Shared.Actors.Device
{
    public class ReadingDbWriterActor : ReceiveActor
    {
        private readonly Guid _deviceId;

        public ReadingDbWriterActor(Guid deviceId)
        {
            _deviceId = deviceId;
            Receive<WriteReadingsToDatabase>(HandleWriteReadingsToDatabase);
        }

        private void HandleWriteReadingsToDatabase(WriteReadingsToDatabase message)
        {
            using (var connection = new SqlConnection(DbSettings.HistoryConnectionString))
            { 
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    WriteReadings(message.Readings, connection, transaction);
                    transaction.Commit();
                }
            }

            ReplyToSender(message);
        }

        private void WriteReadings(ImmutableList<NormalizedMeterReading> messageReadings, SqlConnection connection, SqlTransaction transaction)
        {
            // TODO Write the values to the DB
        }

        private void ReplyToSender(WriteReadingsToDatabase message)
        {
            // TODO Notify the sender of completion
        }


        public static Props CreateProps(Guid deviceId)
        {
            return Props.Create<ReadingDbWriterActor>(deviceId);
        }
    }
}
