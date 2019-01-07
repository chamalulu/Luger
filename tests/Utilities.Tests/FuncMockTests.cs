using System.Linq;
using Xunit;

namespace Luger.Utilities.Tests
{
    public class FuncMockTests
    {
        [Fact]
        public void NullaryFuncMockTest()
        {
            // Given
            var expectedReturnValue = true;
            var funcMock = new FuncMock<bool>(() => expectedReturnValue);

            // When
            var actualReturnValue = funcMock.Invoke();

            // Then
            Assert.Equal(expectedReturnValue, actualReturnValue);
            var call = funcMock.Calls.Single();
            Assert.Equal(expectedReturnValue, call.ReturnValue);
        }

        [Fact]
        public void UnaryFuncMockTest()
        {
            // Given
            bool argument1 = false,
                 argument2 = true,
                 expectedReturnValue1 = true,
                 expectedReturnValue2 = false;

            var funcMock = new FuncMock<bool, bool>(x => !x);

            // When
            var actualReturnValue1 = funcMock.Invoke(argument1);
            var actualReturnValue2 = funcMock.Invoke(argument2);

            // Then
            Assert.Equal(expectedReturnValue1, actualReturnValue1);
            Assert.Equal(expectedReturnValue2, actualReturnValue2);
            var calls = funcMock.Calls.ToArray();
            Assert.Equal(2, calls.Length);
            Assert.Equal(argument1, calls[0].Arguments);
            Assert.Equal(argument2, calls[1].Arguments);
            Assert.Equal(expectedReturnValue1, calls[0].ReturnValue);
            Assert.Equal(expectedReturnValue2, calls[1].ReturnValue);
            Assert.True(calls[0].Time <= calls[1].Time);
        }

        [Fact]
        public void BinaryFuncMockTest()
        {
            // Given
            var arguments = (true, true);
            var expectedReturnValue = false;
            var funcMock = new FuncMock<bool, bool, bool>((x, y) => x ^ y);

            // When
            var actual = funcMock.Invoke(arguments.Item1, arguments.Item2);

            // Then
            Assert.Equal(expectedReturnValue, actual);
            var call = funcMock.Calls.Single();
            Assert.Equal(arguments, call.Arguments);
            Assert.Equal(expectedReturnValue, call.ReturnValue);
        }
    }
}