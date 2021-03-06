﻿// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace ServiceStack.EventStore.Consumers
{
    using System;
    using System.Threading.Tasks;
    using global::EventStore.ClientAPI;
    using Dispatcher;
    using Repository;
    using Subscriptions;
    using Types;

    /// <summary>
    /// Represents the consumer of a catch-up subscription to all streams for the purpose of populating a read model
    /// </summary>
    internal class ReadModelConsumer : StreamConsumer
    {
        public ReadModelConsumer(IEventStoreConnection connection, IEventDispatcher dispatcher, IEventStoreRepository eventStoreRepository) 
                : base(connection, dispatcher, eventStoreRepository) { }

        public override async Task ConnectToSubscription(Subscription subscription)
        {
            this.subscription = subscription;

            try
            {
                await Task.Run(() => connection.SubscribeToAllFrom(Position.Start, true, EventAppeared, LiveProcessingStarted, SubscriptionDropped));
            }
            catch (Exception exception)
            {
                log.Error(exception);
            }
        }

        private async void SubscriptionDropped(EventStoreCatchUpSubscription eventStoreCatchUpSubscription, SubscriptionDropReason subscriptionDropReason, Exception exception) => 
            await HandleDroppedSubscription(new DroppedSubscription(subscription, exception.Message, subscriptionDropReason));

        private void LiveProcessingStarted(EventStoreCatchUpSubscription eventStoreCatchUpSubscription) => 
            log.Info("Read model now caught-up");

        private async void EventAppeared(EventStoreCatchUpSubscription eventStoreCatchUpSubscription, ResolvedEvent resolvedEvent) => 
            await Dispatch(resolvedEvent);
    }
}
