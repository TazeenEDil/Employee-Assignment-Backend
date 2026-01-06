using Microsoft.Extensions.Logging;
using Moq;

namespace Employee_Assignment.UnitTests.Helpers
{
    public static class MockLogger
    {
        public static Mock<ILogger<T>> Create<T>()
        {
            return new Mock<ILogger<T>>();
        }

        public static ILogger<T> CreateLogger<T>()
        {
            return new Mock<ILogger<T>>().Object;
        }
    }
}