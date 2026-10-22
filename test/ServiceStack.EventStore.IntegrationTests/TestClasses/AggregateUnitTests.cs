// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace ServiceStack.EventStore.IntegrationTests.TestClasses
{
    using System;
    using TestDomain;
    using Xunit;

    public class AggregateUnitTests
    {
        [Fact]
        public void AggregateVersionIsCorrectAfterAddingEvents()
        {
            var flight = new Flight();

            flight.UpdateDestination("Malaga");
            Assert.Equal(2, flight.State.Version);

            flight.UpdateFlightNumber("BA1987");
            Assert.Equal(3, flight.State.Version);
        }

        [Fact]
        public void AggregateStateIsCorrectAfterApplyingEvents()
        {
            const string testDestination = "Malaga";
            const string testFlightNumber = "BA1987";

            var flight = new Flight()
                .UpdateDestination(testDestination)
                .UpdateFlightNumber(testFlightNumber);

            Assert.Equal(testDestination, flight.State.Destination);
            Assert.Equal(testFlightNumber, flight.State.FlightNumber);
        }

        [Fact]
        public void RaisingAnAggregateEventThatIsNotHandledThrowsCorrectException()
        {
            var flight = new Flight();

            Assert.Throws<NotImplementedException>(() => flight.AddBaggageToHold(3));
        }
    }
}
