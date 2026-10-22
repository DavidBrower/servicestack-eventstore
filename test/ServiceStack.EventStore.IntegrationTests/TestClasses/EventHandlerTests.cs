// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using ServiceStack.EventStore.IntegrationTests.TestDomain;

namespace ServiceStack.EventStore.IntegrationTests.TestClasses
{
    using Repository;
    using Xunit;
    using Xunit.Abstractions;
    using System.Threading.Tasks;

    [Collection(EventStoreCollection.Name)]
    [Trait("Category", "Integration")]
    public class EventHandlerTests 
    {
        private readonly IEventStoreRepository eventStore;
        private readonly ITestOutputHelper testOutput;
        private bool eventFired;

        public EventHandlerTests(ITestOutputHelper testOutput, EventStoreFixture fixture)
        {
            this.testOutput = testOutput;
            eventStore = fixture.EventStore;
        }

        // todo: how best to test handling events and streams?
        public async Task ConsumesEvent()
        {
            var weeGreenMenLanded = new WeeGreenMenLanded(14);

            await eventStore.PublishAsync(weeGreenMenLanded, "");
            await Task.Delay(10000);

            Assert.True(eventFired);
        }

        public void Handle(WeeGreenMenLanded @event)
        {
            eventFired = true;
        }
    }
}
