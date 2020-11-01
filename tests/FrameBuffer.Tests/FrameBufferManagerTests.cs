using FakeItEasy;

using System;
using System.Collections.Generic;
using System.Linq;

using Xunit;

namespace Luger.FrameBuffer.Tests
{
    public class FrameBufferManagerTests
    {
        [Fact]
        public void CreateFrameBufferBySequenceTest()
        {
            // Arrange
            var disposables = A.Fake<ISet<IDisposable>>();
            A.CallTo(() => disposables.Add(A<IDisposable>._)).Returns(true);

            using var target = new FrameBufferManager(disposables);

            // Act
            _ = target.CreateFrameBuffer(0, 0, Enumerable.Empty<IEnumerable<int>>());

            // Assert
            A.CallTo(() => disposables.Add(A<IDisposable>._)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void DisposeTest()
        {
            // Arrange
            var realDisposables = new HashSet<IDisposable>();
            var fakeDisposables = A.Fake<ISet<IDisposable>>(opts => opts.Wrapping(realDisposables));

            IDisposable frameBufferMemoryOwner;

            using (var target = new FrameBufferManager(fakeDisposables))
            {
                // Act
                _ = target.CreateFrameBuffer(0, 0, Enumerable.Empty<IEnumerable<int>>());
                frameBufferMemoryOwner = realDisposables.Single();
            }

            // Assert
            A.CallTo(() => fakeDisposables.Remove(frameBufferMemoryOwner)).MustHaveHappenedOnceExactly();
        }
    }
}
