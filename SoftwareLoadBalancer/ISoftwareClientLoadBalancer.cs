public interface ISoftwareClientLoadBalancer<T>
{
    int Count { get; }
    T GetClientByIndex(int index);
    T GetNextClientRandom();
    T GetNextClientRoundRobin();
}
