using ElasticSearchNamespace;

class Program
{
    static async Task Main(string[] args)
    {

        // Set up an instance of Elastic Index
        ElasticIndex es = new ElasticIndex();

        // Index all books from the json file
        es.IndexAllBooks();

        // Remove duplicates from the index
        es.CleanDatabase();

        // Remove books in another language than english
        es.CleanDatabaseEnglish();

        //es.CleanDatabase();
        //es.CleanDatabaseEnglish();
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
