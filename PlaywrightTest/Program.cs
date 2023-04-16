using PlaywrightTest;

class Program
{
    static async Task Main(string[] args)
    {

        // var book = new SimpleBook
        // {
        //     bookId = 12445,
        //     title = "The Catcher in the Bye",
        //     author = "J.D. Salinger",
        //     description = "A about a teenage boy who is expelled from his prep school and wanders around New York City.",
        //     imageUrl = "https://www.example.com/images/catcher-in-the-rye.jpg",
        //     rating = 4.5f,
        //     ratingCount = 100,
        //     reviewCount = 50,
        //     genres = new List<string> { "Fiction", "Coming-of-age" }
        // };
        // Elastic es = new Elastic();
        // es.IndexDocument(book, "test");
        // es.Search("novel");

        List<string> idsToVisit = new List<string>();
        for (int node = 50000; node < 50001; node++)
        {
            idsToVisit.Add(node.ToString());
        }
        var crawler = new Crawler();
        var manager = new CrawlerManager(crawler, 1);
        await manager.Setup(idsToVisit);
        await manager.StartWorkers();
    }
}
