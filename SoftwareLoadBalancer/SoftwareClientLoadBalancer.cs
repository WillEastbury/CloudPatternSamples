public class SoftwareClientLoadBalancer<T> : ISoftwareClientLoadBalancer<T>
{
    private Random rand = new Random();
    private readonly List<T> _clients = new List<T>();
    private int _currentClientIndex = 0;
    public SoftwareClientLoadBalancer(List<T> clients)
    {
        _clients = clients;
    }

    public int Count => _clients.Count;

    public T GetNextClientRoundRobin()
    {   int myIndex = _currentClientIndex; 
        Interlocked.Add(ref _currentClientIndex, 1);
        if (_currentClientIndex >= _clients.Count)
        {
            Interlocked.Exchange(ref _currentClientIndex, 0);
        }
        return _clients[myIndex];
    }

    public T GetNextClientRandom()
    {
        return _clients[rand.Next(_clients.Count - 1)];
    }

    public T GetClientByIndex(int index)
    {
        return _clients[index];
    }
}
