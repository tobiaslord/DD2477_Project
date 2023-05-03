using Crawler.Crawlers;
using Crawler;
using ElasticSearchNamespace;
using Cosmos;
using Models;

class Program
{
    static async Task Main(string[] args)
    {
        Elastic es = new Elastic();
        User user = es.GetDocument<User>("4482859", "users");
        Dictionary<string, double> user_vec = es.GetUserVector(user);
        List<SearchResponse> books = es.BetterSearch("space");
        foreach (SearchResponse sbook in books)
        {
            SimpleBook book = sbook.Book;
            Dictionary<string, double> book_vec = es.GetBookVector(book.genres, 0.7);
            double sim = es.GetSimilarity(book_vec, user_vec);
            Console.WriteLine("Title: {0} ------ Similarity: {1} -------- OGScore: {2}", book.title, sim, sbook.Score);
        }

        //var cosmos = new CosmosScripts();
        //await cosmos.PerformRemoveDuplicates();
        //List<string> ids = await cosmos.LoadBookIdsFromUsers();

        //var crawler = new BookCrawler();
        //var manager = new CrawlerManager(crawler, 10);
        //await manager.Setup(ids.ToList());
        //await manager.StartWorkers();
    }
}
