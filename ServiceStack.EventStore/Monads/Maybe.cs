﻿using System.Collections;
using System.Collections.Generic;

namespace ServiceStack.EventStore.Monads
{
    public class Maybe<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T> values;

        public Maybe()
        {
            values = new T[0];
        }

        public Maybe(T value)
        {
            values = new[] { value };
        }

        public IEnumerator<T> GetEnumerator()
        {
            return values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public static class Maybe
    {
        public static Maybe<T> ToMaybe<T>(this T value)
        {
            return new Maybe<T>(value);
        }

        public static Maybe<T> Empty<T>()
        {
            return new Maybe<T>();
        }
    }
}
