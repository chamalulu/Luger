using System;
using System.Threading;

namespace Luger.Utilities
{
    /// <summary>
    /// Wrapper around <see cref="ReaderWriterLockSlim"/> providing <see cref="IDisposable"/> context management for releasing
    /// locks.
    /// </summary>
    /// <remarks>
    /// Wrapped <see cref="ReaderWriterLockSlim"/> is private and does not support recursion.
    /// </remarks>
    public sealed class ContextualReaderWriterLock : IDisposable
    {
        private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);

        /// <summary>
        /// Read lock context.
        /// </summary>
        /// <remarks>
        /// Only implements <see cref="IDisposable"/> for now. Disposing this will release the read lock.
        /// </remarks>
        public interface IReadLock : IDisposable { }

        /// <summary>
        /// Upgradeable read lock context.
        /// </summary>
        /// <remarks>
        /// Enables multiple sequential upgrades to write lock and downgrade to read lock.<br/>
        /// Disposing this will release the upgreadable read lock or the read lock if previously downgraded.
        /// </remarks>
        public interface IUpgradeableReadLock : IReadLock
        {
            /// <summary>
            /// Aquire read lock and release upgradeable read lock.
            /// </summary>
            void Downgrade();

            /// <summary>
            /// Aquire write lock.
            /// </summary>
            /// <returns>Aquired write lock context.</returns>
            IWriteLock Upgrade();
        }

        /// <summary>
        /// Write lock context.
        /// </summary>
        /// <remarks>
        /// Only implements <see cref="IDisposable"/> for now. Disposing this will release the write lock.
        /// </remarks>
        public interface IWriteLock : IDisposable { }

        private class ReadLock : IReadLock
        {
            private readonly ReaderWriterLockSlim _lock;

            public ReadLock(ReaderWriterLockSlim @lock) => _lock = @lock;

            public void Dispose()
            {
                if (_lock.IsReadLockHeld)
                {
                    _lock.ExitReadLock();
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        private class UpgradeableReadLock : IUpgradeableReadLock
        {
            private readonly ReaderWriterLockSlim _lock;

            public UpgradeableReadLock(ReaderWriterLockSlim @lock) => _lock = @lock;

            public void Dispose()
            {
                if (_lock.IsUpgradeableReadLockHeld)
                {
                    _lock.ExitUpgradeableReadLock();
                }
                else if (_lock.IsReadLockHeld)
                {
                    _lock.ExitReadLock();
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            public void Downgrade()
            {
                if (_lock.IsUpgradeableReadLockHeld)
                {
                    _lock.EnterReadLock();
                    _lock.ExitUpgradeableReadLock();
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            public IWriteLock Upgrade()
            {
                if (_lock.IsUpgradeableReadLockHeld)
                {
                    _lock.EnterWriteLock();
                    return new WriteLock(_lock);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        private class WriteLock : IWriteLock
        {
            private readonly ReaderWriterLockSlim _lock;

            public WriteLock(ReaderWriterLockSlim @lock) => _lock = @lock;

            public void Dispose()
            {
                if (_lock.IsWriteLockHeld)
                {
                    _lock.ExitWriteLock();
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        /// <inheritdoc cref="ReaderWriterLockSlim.CurrentReadCount"/>
        public int CurrentReadCount => _lock.CurrentReadCount;

        /// <inheritdoc cref="ReaderWriterLockSlim.IsReadLockHeld"/>
        public bool IsReadLockHeld => _lock.IsReadLockHeld;

        /// <inheritdoc cref="ReaderWriterLockSlim.IsUpgradeableReadLockHeld"/>
        public bool IsUpgradeableReadLockHeld => _lock.IsUpgradeableReadLockHeld;

        /// <inheritdoc cref="ReaderWriterLockSlim.IsWriteLockHeld"/>
        public bool IsWriteLockHeld => _lock.IsWriteLockHeld;

        /// <returns><see cref="LockRecursionPolicy.NoRecursion"/></returns>
        /// <inheritdoc cref="ReaderWriterLockSlim.RecursionPolicy"/>
        public LockRecursionPolicy RecursionPolicy => _lock.RecursionPolicy;

        /// <inheritdoc cref="ReaderWriterLockSlim.WaitingReadCount"/>
        public int WaitingReadCount => _lock.WaitingReadCount;

        /// <inheritdoc cref="ReaderWriterLockSlim.WaitingUpgradeCount"/>
        public int WaitingUpgradeCount => _lock.WaitingUpgradeCount;

        /// <inheritdoc cref="ReaderWriterLockSlim.WaitingWriteCount"/>
        public int WaitingWriteCount => _lock.WaitingWriteCount;

        /// <returns>Read lock context.</returns>
        /// <inheritdoc cref="ReaderWriterLockSlim.EnterReadLock"/>
        public IReadLock EnterReadLock()
        {
            _lock.EnterReadLock();

            return new ReadLock(_lock);
        }

        /// <returns>Upgradeable read lock context.</returns>
        /// <inheritdoc cref="ReaderWriterLockSlim.EnterUpgradeableReadLock"/>
        public IUpgradeableReadLock EnterUpgradeableReadLock()
        {
            _lock.EnterUpgradeableReadLock();

            return new UpgradeableReadLock(_lock);
        }

        /// <returns>Write lock context.</returns>
        /// <inheritdoc cref="ReaderWriterLockSlim.EnterWriteLock"/>
        public IWriteLock EnterWriteLock()
        {
            _lock.EnterWriteLock();

            return new WriteLock(_lock);
        }

        /// <inheritdoc cref="ReaderWriterLockSlim.Dispose"/>
        public void Dispose() => _lock.Dispose();
    }
}
