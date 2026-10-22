// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace ServiceStack.EventStore.IntegrationTests.TestClasses
{
    using System;
    using Xunit;

    public sealed class EventStoreFactAttribute : FactAttribute
    {
        public const string ConnectionStringVariable = "EVENTSTORE_CONNECTION_STRING";

        public EventStoreFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ConnectionStringVariable)))
            {
                Skip = $"Set {ConnectionStringVariable} to run this test.";
            }
        }
    }
}
