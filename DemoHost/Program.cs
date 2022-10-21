using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.ContentModerator;
using Microsoft.Azure.CognitiveServices.ContentModerator.Models;
internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        List<Tuple<string, string>> accounts = new List<Tuple<string, string>>();
        // Usage -> accounts.Add(new Tuple<string, string>("key", "https://username.cognitiveservices.azure.com/"));
   
        List<ContentModeratorClient> clients = new List<ContentModeratorClient>();
        foreach (var account in accounts)
        {
            ContentModeratorClient clientint = new ContentModeratorClient(new ApiKeyServiceClientCredentials(account.Item1));
            clientint.Endpoint = account.Item2;
            clients.Add(clientint);
        }

        builder.Services.AddSingleton<List<ContentModeratorClient>>(clients);
        builder.Services.AddSingleton<IThrottledRequestQueue<Screen>>(e =>
        {
            var AsTRQ = new ThrottledRequestQueue<Screen>(13, TimeSpan.FromSeconds(1));
            Task tsk = AsTRQ.StartProcessing(new CancellationToken(false));
            return AsTRQ;
        });

        builder.Services.AddSingleton<ISoftwareClientLoadBalancer<ContentModeratorClient>, SoftwareClientLoadBalancer<ContentModeratorClient>>();
        var app = builder.Build();
        app.UseHttpsRedirection();

        app.MapGet("/ProfanityFilter/{CheckWord}", async (string CheckWord, [FromServices()] IThrottledRequestQueue<Screen> ATRQ, ISoftwareClientLoadBalancer<ContentModeratorClient> SCLB) =>
        {
            // Just wrap the code you want to throttle inside EnqueueWorkItem and it will be throttled and dispatched downstream via the ThrottledRequestQueue
            
            // Note that if you await this task, it will block until the work item is processed and the result is returned 
            // and you will basically restrict asp.net core to the throttle level but smart clients should be able to handle this
            // The difference is, you can make the call and return the task to the client and the client can continue to do other work, 
            // So this pattern of throttling is much more useful inside a client application than a web application but it does apply backpressure
            // to your app and stop all of the 429s from happening early, even if used in a web app - you will see the response time of 
            // the app increase as the queue backs up

            return await ATRQ.EnqueueWorkItem(async () => await SCLB.GetNextClientRoundRobin().TextModeration.ScreenTextAsync("text/plain", new MemoryStream(Encoding.UTF8.GetBytes(CheckWord))));
        
            // note that theoretically you could reverse the order of these and have multiple throttled queues and load balance across them
            // but that would be a bit more complicated to inject the typed client into the throttled queue
            
        }); 
        app.Run();
    }
}
