using System.Collections.Generic;
using MessagePipe;

namespace KlondikeSolitaire.Tests
{
    public sealed class TestPublisher<T> : IPublisher<T>
    {
        private readonly List<T> _messages = new();

        public IReadOnlyList<T> Messages => _messages;
        public T LastMessage => _messages[_messages.Count - 1];
        public int MessageCount => _messages.Count;

        public void Publish(T message)
        {
            _messages.Add(message);
        }

        public void Clear()
        {
            _messages.Clear();
        }
    }
}
