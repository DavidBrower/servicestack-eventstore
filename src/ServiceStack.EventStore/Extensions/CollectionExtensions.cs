namespace ServiceStack.EventStore.Extensions
{
    using System.Collections.Specialized;

    internal static class CollectionExtensions
    {
        public static void AddAll(this NameValueCollection @this, NameValueCollection collection)
        {
            foreach (var key in collection.AllKeys)
            {
                @this.Add(key, collection[key]);
            }
        }
    }
}
