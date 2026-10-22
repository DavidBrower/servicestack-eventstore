// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace ServiceStack.EventStore.IntegrationTests.TestClasses
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Exceptions;
    using global::EventStore.ClientAPI;
    using Repository;
    using TestDomain;
    using Xunit;
    using Xunit.Abstractions;
    using ReadDirection = Repository.ReadDirection;

    [Collection(EventStoreCollection.Name)]
    [Trait("Category", "Integration")]
    public class ReaderTests
    {
        private readonly IEventStoreRepository eventStore;
        private readonly ITestOutputHelper testOutput;

        public ReaderTests(EventStoreFixture fixture, ITestOutputHelper output)
        {
            eventStore = fixture.EventStore;
            testOutput = output;
        }

        [EventStoreFact]
        public async Task CanReadSingleEventFromCorrectPositionInStream()
        {
            var streamName = $"ReaderTestStream-{Guid.NewGuid()}";

            await eventStore.PublishAsync(new LastPolled { Time = DateTime.UtcNow, Sequence = "Start" }, streamName).ConfigureAwait(false);
            await eventStore.PublishAsync(new LastPolled { Time = DateTime.UtcNow, Sequence = "Middle" }, streamName).ConfigureAwait(false);
            await eventStore.PublishAsync(new LastPolled { Time = DateTime.UtcNow, Sequence = "End" }, streamName).ConfigureAwait(false);

            var firstEvent = await eventStore.ReadEventAsync<LastPolled>(streamName, WhereInStream.Start).ConfigureAwait(false);
            Assert.Equal("Start", firstEvent.Sequence);

            var lastEvent = await eventStore.ReadEventAsync<LastPolled>(streamName, WhereInStream.End).ConfigureAwait(false);
            Assert.Equal("End", lastEvent.Sequence);

            var middleEvent = await eventStore.ReadEventAsync<LastPolled>(streamName, 1).ConfigureAwait(false);
            Assert.Equal("Middle", middleEvent.Sequence);
        }

        [EventStoreFact]
        public async Task ReadSingleEventThrowsEventNotFoundException()
        {
            var streamName = $"ReaderTestStream-{Guid.NewGuid()}";

            //try to read from the stream when no events have been published to it yet
            await Assert.ThrowsAsync<EventNotFoundException>(async () => 
                    await eventStore.ReadEventAsync<LastPolled>(streamName, WhereInStream.Start).ConfigureAwait(false));
        }

        [EventStoreFact]
        public async Task CanReadStreamForwardsAsStronglyTypedObjects()
        {
            var streamName = $"ReaderTestStream-{Guid.NewGuid()}";

            var firstDate = DateTime.UtcNow.AddDays(-3);
            var secondDate = DateTime.UtcNow.AddDays(-2);
            var thirdDate = DateTime.UtcNow.AddDays(-1);
            var fourthDate = DateTime.UtcNow.AddDays(0);

            await eventStore.PublishAsync(new LastPolled { Time = firstDate}, streamName).ConfigureAwait(false);
            await eventStore.PublishAsync(new LastPolled { Time = secondDate}, streamName).ConfigureAwait(false);
            await eventStore.PublishAsync(new LastPolled { Time = thirdDate }, streamName).ConfigureAwait(false);
            await eventStore.PublishAsync(new LastPolled { Time = fourthDate}, streamName).ConfigureAwait(false);

            var eventSlice = await eventStore.ReadSliceFromStreamAsync<LastPolled>(streamName, ReadDirection.Forwards, StreamPosition.Start, SliceSize.Max).ConfigureAwait(false);
            var events = eventSlice.ToArray();

            Assert.Equal(4, events.Length);
            Assert.Equal(firstDate.Date, events[0].Time.Date);
            Assert.Equal(secondDate.Date, events[1].Time.Date);
            Assert.Equal(thirdDate.Date, events[2].Time.Date);
            Assert.Equal(fourthDate.Date, events[3].Time.Date);
        }

        [EventStoreFact]
        public async Task CanReadEventBackwardFromStreamAsStronglyTypedObject()
        {
            var streamName = $"ReaderTestStream-{Guid.NewGuid()}";

            var firstDate = DateTime.UtcNow.AddDays(-3);
            var secondDate = DateTime.UtcNow.AddDays(-2);
            var thirdDate = DateTime.UtcNow.AddDays(-1);
            var fourthDate = DateTime.UtcNow.AddDays(0);

            await eventStore.PublishAsync(new LastPolled { Time = firstDate }, streamName).ConfigureAwait(false);
            await eventStore.PublishAsync(new LastPolled { Time = secondDate }, streamName).ConfigureAwait(false);
            await eventStore.PublishAsync(new LastPolled { Time = thirdDate }, streamName).ConfigureAwait(false);
            await eventStore.PublishAsync(new LastPolled { Time = fourthDate }, streamName).ConfigureAwait(false);

            var eventSlice = await eventStore.ReadSliceFromStreamAsync<LastPolled>(streamName, ReadDirection.Backwards, StreamPosition.End, SliceSize.Max).ConfigureAwait(false);
            var events = eventSlice.ToArray();

            Assert.Equal(4, events.Length);
            Assert.Equal(fourthDate.Date, events[0].Time.Date);
            Assert.Equal(thirdDate.Date, events[1].Time.Date);
            Assert.Equal(secondDate.Date, events[2].Time.Date);
            Assert.Equal(firstDate.Date, events[3].Time.Date);
        }
    }
}
