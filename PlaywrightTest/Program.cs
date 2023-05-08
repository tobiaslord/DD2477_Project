using Crawler.Crawlers;
using Crawler;
using ElasticSearchNamespace;
using Cosmos;
using Models;
using Backend;

class Program
{
    static async Task Main(string[] args)
    {
        // Load Environment variables
        var root = Directory.GetCurrentDirectory();
        var dotenv = Path.Combine(root, ".env");
        DotEnv.Load(dotenv);

        ElasticIndex es = new ElasticIndex();
        es.CleanDatabase();
        es.CleanDatabaseEnglish();
        //es.BetterSearch("romance");

        //var cosmos = new CosmosScripts();
        //await cosmos.PerformRemoveDuplicates();
        //List<string> ids = await cosmos.LoadBookIdsFromUsers();

        //var crawler = new BookCrawler();
        //var manager = new CrawlerManager(crawler, 10);
        //await manager.Setup(ids.ToList());
        //await manager.StartWorkers();
    }
}
