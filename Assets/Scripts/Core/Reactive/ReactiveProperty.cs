using System;
using System.Collections.Generic;

namespace KlondikeSolitaire.Core
{
    public sealed class ReactiveProperty<T> : IDisposable
    {
        private T _value;
        private readonly List<Action<T>> _subscribers;

        public T Value
        {
            get => _value;
            set
            {
                if (EqualityComparer<T>.Default.Equals(_value, value))
                {
                    return;
                }

                _value = value;
                NotifySubscribers();
            }
        }

        public ReactiveProperty(T initialValue = default)
        {
            _value = initialValue;
            _subscribers = new List<Action<T>>(capacity: 4);
        }

        public IDisposable Subscribe(Action<T> callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            _subscribers.Add(callback);
            return new Subscription(this, callback);
        }

        private void NotifySubscribers()
        {
            for (int subscriberIndex = 0; subscriberIndex < _subscribers.Count; subscriberIndex++)
            {
                _subscribers[subscriberIndex].Invoke(_value);
            }
        }

        private void Unsubscribe(Action<T> callback)
        {
            _subscribers.Remove(callback);
        }

        public void Dispose()
        {
            _subscribers.Clear();
        }

        private sealed class Subscription : IDisposable
        {
            private readonly ReactiveProperty<T> _owner;
            private readonly Action<T> _callback;
            private bool _disposed;

            public Subscription(ReactiveProperty<T> owner, Action<T> callback)
            {
                _owner = owner;
                _callback = callback;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _owner.Unsubscribe(_callback);
            }
        }
    }
}
