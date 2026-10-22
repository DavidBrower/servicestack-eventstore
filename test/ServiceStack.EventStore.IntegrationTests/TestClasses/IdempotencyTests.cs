// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace ServiceStack.EventStore.IntegrationTests.TestClasses
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using global::EventStore.ClientAPI;
    using Repository;
    using TestDomain;
    using Xunit;
    using Xunit.Abstractions;
    using ReadDirection = Repository.ReadDirection;

    [Collection(EventStoreCollection.Name)]
    [Trait("Category", "Integration")]
    public class IdempotencyTests
    {
        private readonly IEventStoreRepository eventStore;

        private readonly ITestOutputHelper testOutput;

        public IdempotencyTests(EventStoreFixture fixture, ITestOutputHelper output)
        {
            eventStore = fixture.EventStore;
            testOutput = output;
        }

        [EventStoreFact]
        public async Task SameEventIsIdempotent()
        {
            var streamName = $"IdempotentTestStream-{Guid.NewGuid()}";
            var timeStamp = DateTime.UtcNow;
            var evt = new ServiceHasReachedWarningState(timeStamp);

            await eventStore.PublishAsync(evt, streamName).ConfigureAwait(false);
            await eventStore.PublishAsync(evt, streamName).ConfigureAwait(false);

            var streamEvents = await eventStore.ReadSliceFromStreamAsync(streamName, ReadDirection.Forwards, StreamPosition.Start, 200);

            Assert.Single(streamEvents);

            testOutput.WriteLine($"1 event idempotently written to {streamName}");
        }

        [EventStoreFact]
        public async Task SimilarEventWithDifferentTimeStampIsNotIdempotent()
        {
            var product = new Product {ProductCode = "MN2400" };
            var customer = new Customer {CustomerCode = "0000010367" };
            var firstOccurrence = DateTime.UtcNow.Subtract(new TimeSpan(3, 0, 0));
            var secondOccurrence = DateTime.UtcNow.Subtract(new TimeSpan(2, 0, 0));
            var evt1 = new PriceChanged {Product = product, Customer = customer, TimeStamp = firstOccurrence};
            var evt2 = new PriceChanged {Product = product, Customer = customer, TimeStamp = secondOccurrence};
            var streamName = $"IdempotentTestStream-{Guid.NewGuid()}";

            await eventStore.PublishAsync(evt1, streamName).ConfigureAwait(false);
            await eventStore.PublishAsync(evt2, streamName).ConfigureAwait(false);

            var streamEvents = await eventStore.ReadSliceFromStreamAsync(streamName, ReadDirection.Forwards, StreamPosition.Start, 200).ConfigureAwait(false);

            Assert.Equal(2, streamEvents.Count());

            testOutput.WriteLine($"2 events succesfully written to {streamName}");
        }
    }
}
