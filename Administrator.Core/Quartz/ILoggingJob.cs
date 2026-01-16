using Microsoft.Extensions.Logging;
using Quartz;

namespace Administrator.Core;

public interface ILoggingJob : IJob
{
    ILogger Logger { get; }
}