using System.Net;
using System.Reactive.Linq;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using System.Runtime.Caching;

// Initialize cache
MemoryCache cache = MemoryCache.Default;

// Create a new HttpListener instance
HttpListener listener = new HttpListener();

// Add prefix for listening on port 80
listener.Prefixes.Add("http://*:8080/");

// Start the listener
listener.Start();
printNote("Listening on port 8080...");

// Handle incoming requests
Task server = Task.Factory.StartNew(() =>
{
    while (listener.IsListening)
    {
        try
        {
            // Wait for an incoming request
            HttpListenerContext context = listener.GetContext();
            Task.Factory.StartNew(() => handleRequest(context));
        }
        catch (Exception ex)
        {
            printError(ex.Message);
        }
    }
}, TaskCreationOptions.LongRunning);

// Wait for the user to press a key before quitting
printNote("Press any key to stop the server...");
Console.ReadKey();
printNote("Key pressed. Stopping server...");

// Stop the listener
listener.Stop();

void handleRequest(HttpListenerContext context) {
    const string STARTYEAR = "startYear";
    const string ENDYEAR = "endYear";

    // Check if there are arguments
    var uri = new Uri(context.Request.Url!.ToString());
    var queryParams = HttpUtility.ParseQueryString(uri.Query);
    int startYear = 0;
    int endYear = 0;
    StringBuilder stringResponse = new();
    if(queryParams[STARTYEAR] == null || queryParams[STARTYEAR]?.Length != 4 || !int.TryParse(queryParams[STARTYEAR], out startYear)) {
        returnText("Invalid start date.", context);
    }
    if(queryParams[ENDYEAR] == null || queryParams[ENDYEAR]?.Length != 4 || !int.TryParse(queryParams[ENDYEAR], out endYear)) {
        returnText("Invalid end date.", context);
    }


        var observables = new List<IObservable<NobelPrize>>();
        for (int year = startYear; year <= endYear; year++)
        {
            IObservable<NobelPrize> NobelPrizes = (IObservable<NobelPrize>)cache.Get(year.ToString());
            if(NobelPrizes != null) {
                printNote($"Nobel prizes form year {year} are already cached.");
            }
            else {
                printWarning($"Nobel prizes form year {year} are not cached.");
                NobelPrizes = FetchNobelPrizesForYear(year);
                CacheItemPolicy policy = new CacheItemPolicy
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(1)
                };
                cache.Add(year.ToString(), NobelPrizes, policy);
                printNote($"Nobel prizes form year {year} cached.");
            }
            observables.Add(NobelPrizes);
        }
        var observable = Observable.Concat(observables);

        var subscription = observable
            .ToList()
            .Select(prizes =>
            {
                var totalAmount = prizes.Sum(prize => prize.PrizeAmountAdjusted);
                var averageAmount = prizes.Any() ? totalAmount / prizes.Count : 0;
                return new { Prizes = prizes, AverageAmount = averageAmount };
            })
            .Subscribe(result =>
            {
                    foreach (var prize in result.Prizes)
                    {
                        try
                        {
                            if(prize.Laureates!= null && prize.Laureates.Count > 0) 
                            {
                                printNote($"Category: {prize.Category.Name} - {prize.AwardYear}");
                                stringResponse.AppendLine($"<p>Category: {prize.Category.Name} - {prize.AwardYear}</p>");
                                foreach (var laureate in prize.Laureates)
                                {
                                    if(laureate.KnownName != null) {
                                        printNote($"Laureate name: {laureate.KnownName.Name}");
                                        stringResponse.AppendLine($"<li>Laureate name: {laureate.KnownName.Name}</li>");
                                    }
                                    else {
                                        printNote($"Laureate organization name: {laureate.OrgName.Name}");
                                        stringResponse.AppendLine($"<li>Laureate organization name: {laureate.OrgName.Name}</li>");
                                    }
                                }
                                Console.WriteLine();
                                stringResponse.AppendLine($"<br>");
                            }
                        }
                        catch(Exception ex) {
                            printError(ex.ToString());
                        }
                    }
                stringResponse.AppendLine($"<p>Average Adjusted Prize Amount: {result.AverageAmount}</p>");
                returnText(stringResponse.ToString(), context);
            });
            
}

IObservable<NobelPrize> FetchNobelPrizesForYear(int year)
{
    return Observable.Create<NobelPrize>(async observer =>
    {
        try
        {
            using (var client = new HttpClient())
            {
                var url = $"https://api.nobelprize.org/2.1/nobelPrizes?nobelPrizeYear={year}";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var prizes = JsonConvert.DeserializeObject<NobelPrizeResponse>(content)!.NobelPrizes;

                foreach (var prize in prizes)
                {
                    observer.OnNext(prize);
                }

                observer.OnCompleted();
            }
        }
        catch (Exception ex)
        {
            observer.OnError(ex);
        }
    });
}

void returnText(string text, HttpListenerContext context) {
    string responseString = $"<html><body>{text}</body></html>";
    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
    context.Response.ContentLength64 = buffer.Length;
    context.Response.OutputStream.Write(buffer, 0, buffer.Length);
    context.Response.Close();
}

void printWarning(string text) 
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write("WARN: ");
    Console.ResetColor();
    Console.WriteLine(text);
}

void printNote(string text) 
{
    Console.ForegroundColor = ConsoleColor.Blue;
    Console.Write("NOTE: ");
    Console.ResetColor();
    Console.WriteLine(text);
}

void printError(string text) 
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Write("ERROR: ");
    Console.ResetColor();
    Console.WriteLine(text);
}