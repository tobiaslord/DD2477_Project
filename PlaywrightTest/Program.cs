using Microsoft.Playwright;
using System.Runtime.InteropServices;
using SimpleBookNamespace;
using Newtonsoft.Json;
using ElasticSearchNamespace;

class Program
{
    static async Task Main(string[] args)
    {
        var book = new SimpleBook
        {
            bookId = 12445,
            title = "The Catcher in the Bye",
            author = "J.D. Salinger",
            description = "A about a teenage boy who is expelled from his prep school and wanders around New York City.",
            imageUrl = "https://www.example.com/images/catcher-in-the-rye.jpg",
            rating = 4.5f,
            ratingCount = 100,
            reviewCount = 50,
            genres = new List<string> { "Fiction", "Coming-of-age" }
        };
        Elastic es = new Elastic();
        es.IndexDocument(book, "test");
        es.Search("novel");
        PlaywrightTest.Crawler crawler = new PlaywrightTest.Crawler();
        await crawler.CrawlTest(120, 15);
    }
}
