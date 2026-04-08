using System;
using System.Collections.Generic;

namespace KlondikeSolitaire.Core
{
    public sealed class CompositeDisposable : IDisposable
    {
        private readonly List<IDisposable> _disposables;
        private bool _disposed;

        public CompositeDisposable()
        {
            _disposables = new List<IDisposable>(capacity: 8);
        }

        public void Add(IDisposable disposable)
        {
            if (disposable == null)
            {
                throw new ArgumentNullException(nameof(disposable));
            }

            if (_disposed)
            {
                disposable.Dispose();
                return;
            }

            _disposables.Add(disposable);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            for (int disposableIndex = 0; disposableIndex < _disposables.Count; disposableIndex++)
            {
                _disposables[disposableIndex].Dispose();
            }

            _disposables.Clear();
        }
    }

    public static class DisposableExtensions
    {
        public static T AddTo<T>(this T disposable, CompositeDisposable composite) where T : IDisposable
        {
            if (composite == null)
            {
                throw new ArgumentNullException(nameof(composite));
            }

            composite.Add(disposable);
            return disposable;
        }
    }
}
