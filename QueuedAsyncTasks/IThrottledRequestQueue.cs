using Microsoft.Extensions.Hosting;
public interface IThrottledRequestQueue<T> : IHostedService, IDisposable
{
    Task<T> EnqueueWorkItem(Func<Task<T>> WorkItem);
    Task StartProcessing(CancellationToken stoppingToken);
}
