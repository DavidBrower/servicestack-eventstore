// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace ServiceStack.EventStore.IntegrationTests.TestClasses
{
    using System;
    using global::EventStore.ClientAPI;
    using Repository;

    public sealed class EventStoreFixture : IDisposable
    {
        private IEventStoreConnection connection;
        private IEventStoreRepository eventStore;

        public IEventStoreRepository EventStore
        {
            get
            {
                if (eventStore != null)
                {
                    return eventStore;
                }

                var connectionString = Environment.GetEnvironmentVariable(EventStoreFactAttribute.ConnectionStringVariable);
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new InvalidOperationException(
                        $"Set {EventStoreFactAttribute.ConnectionStringVariable} to run the EventStore integration tests.");
                }

                connection = EventStoreConnection.Create(connectionString);
                connection.ConnectAsync().GetAwaiter().GetResult();
                eventStore = new EventStoreRepository(connection);

                return eventStore;
            }
        }

        public void Dispose()
        {
            connection?.Close();
        }
    }
}
