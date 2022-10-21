
// Create a class that queues requests for an external service and executes them asynchronously in the background using TaskCompletionSource to mock synchronous calls
public class ConcurrentRequest<T>
{
    public Func<Task<T>> ExecutedFunction { get; set; }
    public TaskCompletionSource<T> CompletionSource { get; set; }
}
