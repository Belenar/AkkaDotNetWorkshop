using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.Persistence;
using AkkaDotNet.SensorData.Shared.Messages;

namespace AkkaDotNet.SensorData.Shared.Actors.Device
{
    // READ THE COMMENTS IN THIS ACTOR IN ORDER TO MAKE SENSE OF IT
    public class ReadingPersistenceActor : ReceivePersistentActor
    {
        private readonly Guid _deviceId;
        private ReadingPersistenceState _state = new ReadingPersistenceState();
        private readonly IActorRef _dbWriterActor;

        // 1. The persistence ID is what links the saved messages and snapshots to this unique actor.
        //    THIS NEEDS TO BE UNIQUE!
        public override string PersistenceId => $"value-persistence-{_deviceId}";

        public ReadingPersistenceActor(Guid deviceId)
        {
            _deviceId = deviceId;

            // 2. Commands are message tha com in from the normal mailbox
            Command<NormalizedMeterReading>(HandleNormalizeMeterReadingCommand);
            Command<RequestLastNormalizedReadings>(HandleRequestLastNormalizedReading);
            Command<TakeHourlySnapshotMessage>(_ => TakeHourlySnapshot());
            Command<WrittenReadingsToDatabase>(HandleWrittenReadingsToDatabaseCommand);

            // 3. Recover messages are replayed first when the Actor is restored (after a restart)
            Recover<SnapshotOffer>(HandleSnapshotOffer);
            Recover<NormalizedMeterReading>(HandleNormalizeMeterReading);
            Recover<WrittenReadingsToDatabase>(HandleWrittenReadingsToDatabase);

            // 4. This child Actor will save older data to another database for future reference
            _dbWriterActor = Context.ActorOf(ReadingDbWriterActor.CreateProps(_deviceId));

            // 5. Schedule an hourly message at a random time point during the hour.
            ScheduleSnapshots();
        }


        #region Snapshots

        /// <summary>
        /// To spread all the snapshot activity over the hour, we schedule these messages at a random
        /// time in the first hour, and every hour after that. It will trigger the following:
        /// - save of a snapshot
        /// - trigger the save of historic values
        /// - truncate the current state back to 12 hours
        /// </summary>
        private void ScheduleSnapshots()
        {
            // 6. This schedules a TakeHourlySnapshotMessage every hour.
            var seconds = new Random().Next(3600);
            var initialDelay = new TimeSpan(0, 0, 0, seconds);
            var interval = new TimeSpan(0, 1, 0, 0);
            Context.System.Scheduler.ScheduleTellRepeatedly(initialDelay, interval, Context.Self, new TakeHourlySnapshotMessage(), Context.Self);
        }

        /// <summary>
        /// Restores the last snapshot
        /// </summary>
        private void HandleSnapshotOffer(SnapshotOffer offer)
        {
            if (offer.Snapshot is ReadingPersistenceState state)
                _state = state;
        }

        private void TakeHourlySnapshot()
        { 
            // 7. Saves the entire state as a snapshot to the Akka.NET Persistence
            SaveSnapshot(_state);

            // 8. Tells the child Actor to write values to the DB too.
            _dbWriterActor.Tell(new WriteReadingsToDatabase(_state.GetUnsavedItems()));
        }

        #endregion

        #region NormalizedMeterReading

        private void HandleNormalizeMeterReadingCommand(NormalizedMeterReading message)
        {
            // 9. A command comes in --> Persist it to Akka.NET Persistence and call business logic
            Persist(message, msg => HandleNormalizeMeterReading(message));
        }

        private void HandleNormalizeMeterReading(NormalizedMeterReading message)
        {
            // 10. Business logic for the above function. Recover calls this DIRECTLY.
            _state.Add(message);
        }

        #endregion

        #region WrittenReadingsToDatabase

        private void HandleWrittenReadingsToDatabaseCommand(WrittenReadingsToDatabase message)
        {
            // 11. A command comes in --> Persist it to Akka.NET Persistence and call business logic
            Persist(message, msg => HandleWrittenReadingsToDatabase(message));
        }

        private void HandleWrittenReadingsToDatabase(WrittenReadingsToDatabase message)
        {
            // 12. Business logic for the above function. Recover calls this DIRECTLY.
            _state.SetSavedUntil(message.WrittenToDate);

            // 13. state has been saved to the database, so we can throw away older items.
            _state.Truncate();
        }

        #endregion

        #region RequestLastNormalizedReadings

        private void HandleRequestLastNormalizedReading(RequestLastNormalizedReadings message)
        {
            // 14. When another actor request data, we return it from our state.
            //     Since this does not change the state, this command doesn't need persisting.
            var lastReadings = _state.GetLastReadings(message.NumberOfReadings);
            var response = new ReturnLastNormalizedReadings(lastReadings);
            Sender.Tell(response);
        }

        #endregion

        public static Props CreateProps(Guid deviceId)
        {
            // 15. Mandatory CreateProps
            return Props.Create<ReadingPersistenceActor>(deviceId);
        }
    }

    // 16. A state class is always a good idea when dealing with persistence.
    //     It makes it harder to forget stuff in your snapshot.
    //     It also makes snapshot restores harder
    class ReadingPersistenceState
    {
        // 17. All necessary data goes in read only properties
        public List<ReadingPersistenceStateItem> Items { get; } = new List<ReadingPersistenceStateItem>();

        // 18. For every state manipulation we make a function
        public void Add(NormalizedMeterReading reading)
        {
            Items.Add(new ReadingPersistenceStateItem { Reading = reading, Saved = false });
        }

        public NormalizedMeterReading[] GetUnsavedItems()
        {
            return Items.Where(i => !i.Saved).Select(i => i.Reading).ToArray();
        }

        public void SetSavedUntil(DateTime until)
        {
            foreach (var item in Items.Where(i => i.Reading.Timestamp <= until))
            {
                item.Saved = true;
            }
        }

        public NormalizedMeterReading[] GetLastReadings(int numberOfReadings)
        {
            var numberOfReturnedReadings = Math.Min(numberOfReadings, Items.Count);

            if (numberOfReturnedReadings == 0)
                return Array.Empty<NormalizedMeterReading>();

            return Items
                .Select(i => i.Reading)
                .OrderByDescending(r => r.Timestamp)
                .Take(numberOfReturnedReadings)
                .OrderBy(r => r.Timestamp)
                .ToArray();
        }

        public void Truncate()
        {
            if (Items.Any())
            {
                var bottomDate = Items.Last().Reading.Timestamp.AddHours(-12);
                Items.RemoveAll(i => i.Reading.Timestamp < bottomDate && i.Saved);
            }
        }
    }

    class ReadingPersistenceStateItem
    {
        public NormalizedMeterReading Reading { get; set; }
        // 19. Tells is if this one has made it to the second database yet
        public bool Saved { get; set; }
    }

    // 20. Empty message to trigger an hourly action on the Actor
    class TakeHourlySnapshotMessage
    {
    }
}