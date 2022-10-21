using System.Diagnostics;
public class Program
{            
    static int cps = 0;
    static int tcps = 0;
    static int tfps = 0;
    static int fps = 0;
    static DateTime Started = DateTime.Now; 
    static List<Task>  tskList = new List<Task>();
    static PeriodicTimer  timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

    public static void Main(string[] args)
    {
        
        Task tsk = Task.Run(async () =>
        {
            while (true)
            {
                await timer.WaitForNextTickAsync();
                Console.WriteLine($"Total Requests {tcps} Failures {tfps} | Uptime : {DateTime.Now.Subtract(Started).TotalSeconds} | RPS : {tcps / DateTime.Now.Subtract(Started).TotalSeconds} | FPS : {tfps / DateTime.Now.Subtract(Started).TotalSeconds} {DateTime.Now.ToString()} Instant RPS {cps} Instant FPS {fps}");
                cps = 0;
                fps = 0;
            }
        });

        tskList.Add(tsk);

        // Multiple threads with separate http Connections to try and create throttling
        tskList.Add(BlastRequests("scunthorpe")); 
        tskList.Add(BlastRequests("diddly"));
        tskList.Add(BlastRequests("Insert horrible test obscenity that I don't want to check in on github here."));
        
        Task.WaitAny(tskList.ToArray()); 
    }

    private static async Task BlastRequests(string profane)
    {
        using (var httpClientHandler = new HttpClientHandler())
        {
            Console.WriteLine("Beginning Testing");
            httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };
            var htc = new HttpClient(httpClientHandler);
            while (true)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                try
                {
                    var response = await htc.GetAsync("https://LOCALHOST:5001/ProfanityFilter/{profane}");
                    Interlocked.Increment(ref cps);
                    Interlocked.Increment(ref tcps);
                    sw.Stop();
                    response.EnsureSuccessStatusCode();

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception: " + ex.Message);
                    Interlocked.Increment(ref fps);
                    Interlocked.Increment(ref tfps);
                }
            }
        }
    }
}