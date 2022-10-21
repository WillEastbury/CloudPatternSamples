using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
public class ThrottledRequestQueue<T> : BackgroundService, IThrottledRequestQueue<T>
{
    private readonly ConcurrentQueue<ConcurrentRequest<T>> _workItems = new ConcurrentQueue<ConcurrentRequest<T>>();
    public int _CountThisPeriod = 0;
    public int _MaxTPS; 
    private PeriodicTimer tim;
    public TimeSpan ThrottlePeriod { get; private set; }

    public ThrottledRequestQueue(int maxTPS = 40, TimeSpan ThrottlePeriod = default)
    {
        _MaxTPS = maxTPS;
        this.ThrottlePeriod = ThrottlePeriod == default ? TimeSpan.FromSeconds(1) : ThrottlePeriod;
        tim = new PeriodicTimer(ThrottlePeriod);
    }

    public virtual Task<T> EnqueueWorkItem(Func<Task<T>> WorkItem) // Enqueue the workitem and async yield until it's completed
    {
        ConcurrentRequest<T> req = new ConcurrentRequest<T>() { ExecutedFunction = WorkItem, CompletionSource = new TaskCompletionSource<T>() };
        Task<T> tsk = req.CompletionSource.Task;    // Create a task that will be completed when the work item is processed    
        _workItems.Enqueue(req);                    // Pop the request on the queue for processing
        return tsk;                                 // Return the uncompleted task back to the caller to await whilst the background task completes it and sets the result
    }

    private async Task ResetThrottleTimerCount(CancellationToken _cts)
    {
        while (!_cts.IsCancellationRequested)
        {
            await tim.WaitForNextTickAsync();
            Interlocked.Exchange(ref _CountThisPeriod, 0);
        }
    }
    protected virtual Task ExecuteTimerAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => ResetThrottleTimerCount(stoppingToken));
    }
    protected virtual Task ExecuteProcessingLoopAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => ProcessingLoop(stoppingToken));
    }

    protected virtual async Task ProcessingLoop(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            ConcurrentRequest<T> QueuedRequestTask = null;
            if (_workItems.TryPeek(out QueuedRequestTask)) // Peek at the next request so we have something to process
            {
                try
                {
                    if (_CountThisPeriod <= _MaxTPS)
                    {
                        // OK we have something and are not being throttled.
                        _workItems.TryDequeue(out QueuedRequestTask);
                        Interlocked.Increment(ref _CountThisPeriod);
                        QueuedRequestTask.CompletionSource.SetResult(await QueuedRequestTask.ExecutedFunction());
                    }
                    else
                    {   
                        // Throttling in place, pause processing
                        await Task.Delay(5);
                    }
                }
                catch (Exception ex)
                {
                    // Propagate the exception back to the original task via the TaskCompletionSource
                    QueuedRequestTask.CompletionSource.SetException(ex);
                }
            }
            else
            {
                // Nothing to process, let's wait a bit
                await Task.Delay(5);
            }
        }
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) // Start the background processing loops
    {
        Task timer = Task.Run(() => ExecuteTimerAsync(stoppingToken));
        Task Processing = Task.Run(() => ExecuteProcessingLoopAsync(stoppingToken));
        return Task.WhenAny(timer, Processing);
    }
    public virtual Task StartProcessing(CancellationToken stoppingToken)
    {
        return ExecuteAsync(stoppingToken);
    }
}