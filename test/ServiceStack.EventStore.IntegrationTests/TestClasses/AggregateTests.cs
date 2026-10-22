// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace ServiceStack.EventStore.IntegrationTests.TestClasses
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Exceptions;
    using Extensions;
    using global::EventStore.ClientAPI;
    using Repository;
    using TestDomain;
    using Types;
    using Xunit;
    using Xunit.Abstractions;

    // NOTE: These integration tests require EventStore to be running locally!
    [Collection(EventStoreCollection.Name)]
    [Trait("Category", "Integration")]
    public class AggregateTests
    {
        private readonly IEventStoreRepository eventStore;

        private readonly ITestOutputHelper testOutput;

        public AggregateTests(EventStoreFixture fixture, ITestOutputHelper output)
        {
            eventStore = fixture.EventStore;
            testOutput = output;
        }

        [EventStoreFact]
        public async Task AnAggregateCanBeSavedAndRehydrated()
        {
            // Act
            var flightToBePersisted = 
                    await new Flight()
                                .UpdateFlightNumber("CQH8821")
                                .SetEstimatedDepartureTime(DateTime.UtcNow.AddHours(1))
                                .SaveAsync(eventStore)
                                .ConfigureAwait(false);

            var flightToBeRehydrated = await eventStore.GetByIdAsync<Flight>(flightToBePersisted.Id);

            // Assert
            Assert.Equal(flightToBePersisted.Id, flightToBeRehydrated.Id);
        }

        [EventStoreFact]
        public async Task GetsEventsFromCorrectStreams()
        {
            var firstFlight = new Flight();
            var secondFlight = new Flight();

            const string firstFlightNumber = "BA7651";
            const string secondlightNumber = "BA7652";

            firstFlight.UpdateFlightNumber(firstFlightNumber);
            secondFlight.UpdateFlightNumber(secondlightNumber);

            testOutput.WriteLine($"First Flight Id: {firstFlight.Id}");
            testOutput.WriteLine($"Second Flight Id: {secondFlight.Id}");

            await eventStore.SaveAsync(firstFlight);
            await eventStore.SaveAsync(secondFlight);

            var firstSaved = await eventStore.GetByIdAsync<Flight>(firstFlight.Id);
            var secondSaved = await eventStore.GetByIdAsync<Flight>(secondFlight.Id);

            Assert.Equal(firstFlightNumber, firstSaved.State.FlightNumber);
            Assert.Equal(secondlightNumber, secondSaved.State.FlightNumber);
        }

        [EventStoreFact]
        public async Task ThrowsOnRequestingSpecificVersionHigherThanExists()
        {
            var flightToBePersisted = 
                await new Flight()
                        .UpdateDestination("Grimthorpe")
                        .UpdateFlightNumber("XY6876")
                        .SetEstimatedDepartureTime(DateTime.UtcNow.AddMinutes(134))
                        .SaveAsync(eventStore)
                        .ConfigureAwait(false);

            var correctVersion = flightToBePersisted.State.Version;
            var incorrectVersion = correctVersion.Add(1);

            await
                Assert.ThrowsAsync<AggregateVersionException>(
                    () => eventStore.GetByIdAsync<Flight>(flightToBePersisted.Id, incorrectVersion));
        }

        [EventStoreFact]
        public async Task CanAddHeadersToAggregateEvents()
        {
            var flight = 
                await new Flight()
                            .UpdateDestination("La Palma")
                            .UpdateFlightNumber("LT7987")
                            .SetEstimatedDepartureTime(DateTime.UtcNow.AddMinutes(67))
                            .SaveAsync(eventStore, headers =>
                            {
                                headers.Add("User", "Lil'Joey Brown");
                                headers.Add("Other", "SomeImportantInfo");
                            })
                            .ConfigureAwait(false);

            await eventStore.GetByIdAsync<Flight>(flight.Id);
        }

        [EventStoreFact]
        public async Task CanSaveAndRehydrateLargeNumberOfEvents()
        {
            const int noOfEvents = 5000;
            
            var newFlight = new Flight();

            do
            {
                newFlight.SetEstimatedDepartureTime(DateTime.UtcNow.AddMinutes(1));
            }
            while (newFlight.State.Version < noOfEvents);

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            await eventStore.SaveAsync(newFlight);

            var timeToSave = stopWatch.Elapsed;
            testOutput.WriteLine($"{noOfEvents} events written in {timeToSave}");

            stopWatch.Start();
            var rehydratedFlight = await eventStore.GetByIdAsync<Flight>(newFlight.Id);

            var timeToRehydrate = stopWatch.Elapsed;
            testOutput.WriteLine($"{noOfEvents} events loaded and applied in {timeToRehydrate}");

            const int expectedVersion = noOfEvents;

            Assert.Equal(expectedVersion, newFlight.State.Version);
            Assert.Equal(expectedVersion, rehydratedFlight.State.Version);
        }

        [EventStoreFact]
        public async Task AggregateStateIsCorrectAfterRehydration()
        {
            const string originalDestination = "Hong Kong";
            const string originalFlightNumber = "BA1987";
            const string newDestination = "Barra";
            const string newFlightNumber = "BA1986";

            var newFlight = new Flight();

            // Act
            await newFlight
                    .UpdateDestination(originalDestination)
                    .UpdateFlightNumber(originalFlightNumber)
                    .SaveAsync(eventStore)
                    .ConfigureAwait(false);

            var firstRehydration = await eventStore.GetByIdAsync<Flight>(newFlight.Id).ConfigureAwait(false);

            await firstRehydration
                    .UpdateDestination(newDestination)
                    .UpdateFlightNumber(newFlightNumber)
                    .SaveAsync(eventStore)
                    .ConfigureAwait(false);

            var secondRehydration = await eventStore.GetByIdAsync<Flight>(newFlight.Id).ConfigureAwait(false);

            // Assert
            Assert.Equal(newDestination, secondRehydration.State.Destination);
            Assert.Equal(newFlightNumber, secondRehydration.State.FlightNumber);
        }

        [EventStoreFact]
        public Task ThrowsCorrectExceptionWhenLoadingNonPersistedAggregate()
        {
            return
                Assert.ThrowsAsync<AggregateNotFoundException>(
                    () => eventStore.GetByIdAsync<Flight>(Guid.NewGuid()));
        }

        [EventStoreFact]
        public async Task ThrowsCorrectExceptionWhenLoadingSoftDeletedAggregate()
        {
            var flight = new Flight();
            var streamName = StreamName(flight);

            // Act
            await flight
                    .SetEstimatedDepartureTime(DateTime.UtcNow.AddMinutes(23))
                    .SaveAsync(eventStore)
                    .ConfigureAwait(false);

            // use the eventstore connection directly
            await eventStore.DeleteStreamAsync(streamName, ExpectedVersion.Any);

            await
                Assert.ThrowsAsync<AggregateNotFoundException>(
                    () => eventStore.GetByIdAsync<Flight>(flight.Id));
        }

        [EventStoreFact]
        public async Task ThrowsCorrectExceptionWhenLoadingHardDeletedAggregate()
        {
            const bool hardDelete = true;
            var flight = new Flight();
            var streamName = StreamName(flight);

            // Act
            await flight.SetEstimatedDepartureTime(DateTime.UtcNow.AddMinutes(23))
                        .SaveAsync(eventStore)
                        .ConfigureAwait(false);

            await eventStore.DeleteStreamAsync(streamName, ExpectedVersion.Any, hardDelete)
                            .ConfigureAwait(false);

            await
                Assert.ThrowsAsync<AggregateDeletedException>(
                    () => eventStore.GetByIdAsync<Flight>(flight.Id));
        }

        [EventStoreFact]
        public async Task CorrectlyRetrievesSpecificVersion()
        {
            var flightToBePersisted = new Flight();

            // Act
            for (var i = 1; i <= 10; i++)
            {
                flightToBePersisted.UpdateDestination($"Destination No: {i}");
                flightToBePersisted.SetEstimatedDepartureTime(DateTime.UtcNow.AddHours(i));
            }

            await eventStore.SaveAsync(flightToBePersisted);

            var rehydratedFlight = await eventStore.GetByIdAsync<Flight>(flightToBePersisted.Id, 5);

            // Assert
            Assert.Equal(5, rehydratedFlight.State.Version);
        }

        [EventStoreFact]
        public async Task CanRepeatedlyPersistAggregate()
        {
            var flightToBePersisted = new Flight();

            // Act
            for (var i = 1; i <= 10; i++)
            {
                flightToBePersisted.UpdateDestination($"Destination No: {i}");
                flightToBePersisted.SetEstimatedDepartureTime(DateTime.UtcNow.AddHours(i));
            }

            await eventStore.SaveAsync(flightToBePersisted);

            for (var i = 11; i <= 20; i++)
            {
                flightToBePersisted.UpdateDestination($"Destination No: {i}");
                flightToBePersisted.SetEstimatedDepartureTime(DateTime.UtcNow.AddHours(i));
            }

            await eventStore.SaveAsync(flightToBePersisted);
        }

        private static string StreamName(Aggregate flight)
        {
            var streamName = $"{typeof(Flight).Name}-{flight.Id}";
            return streamName;
        }
    }
}
