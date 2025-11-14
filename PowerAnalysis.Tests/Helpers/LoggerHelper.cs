using Microsoft.Extensions.Logging;
using Moq;

namespace PowerAnalysis.Tests.Helpers;

/// <summary>
/// Helper class for creating mock loggers
/// </summary>
public static class LoggerHelper
{
    /// <summary>
    /// Create a mock logger for testing
    /// </summary>
    public static Mock<ILogger<T>> CreateMockLogger<T>()
    {
        return new Mock<ILogger<T>>();
    }

    /// <summary>
    /// Verify that a log message was written at a specific level
    /// </summary>
    public static void VerifyLog<T>(
        Mock<ILogger<T>> mockLogger,
        LogLevel level,
        Times times)
    {
        mockLogger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            times);
    }

    /// <summary>
    /// Verify that an error log was written
    /// </summary>
    public static void VerifyErrorLog<T>(
        Mock<ILogger<T>> mockLogger,
        Times? times = null)
    {
        VerifyLog(mockLogger, LogLevel.Error, times ?? Times.AtLeastOnce());
    }

    /// <summary>
    /// Verify that an information log was written
    /// </summary>
    public static void VerifyInformationLog<T>(
        Mock<ILogger<T>> mockLogger,
        Times? times = null)
    {
        VerifyLog(mockLogger, LogLevel.Information, times ?? Times.AtLeastOnce());
    }

    /// <summary>
    /// Verify that a warning log was written
    /// </summary>
    public static void VerifyWarningLog<T>(
        Mock<ILogger<T>> mockLogger,
        Times? times = null)
    {
        VerifyLog(mockLogger, LogLevel.Warning, times ?? Times.AtLeastOnce());
    }
}
