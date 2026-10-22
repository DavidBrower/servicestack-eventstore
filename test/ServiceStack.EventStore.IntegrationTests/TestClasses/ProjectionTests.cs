// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ServiceStack.EventStore.IntegrationTests.TestClasses
{
    using System.Threading.Tasks;
    using Repository;
    using Xunit;
    using Xunit.Abstractions;

    [Collection(EventStoreCollection.Name)]
    [Trait("Category", "Integration")]
    public class ProjectionTests
    {
        private readonly IEventStoreRepository eventStore;
        private readonly ITestOutputHelper testOutput;

        public ProjectionTests(EventStoreFixture fixture, ITestOutputHelper output)
        {
            eventStore = fixture.EventStore;
            testOutput = output;
        }
    }
}
