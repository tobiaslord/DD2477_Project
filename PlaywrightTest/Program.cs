using Microsoft.Playwright;
using System.Runtime.InteropServices;
using SimpleBookNamespace;
using Newtonsoft.Json;
using ElasticSearchNamespace;

class Program
{
    static async Task Main(string[] args)
    {
        var book = new SimpleBook { Title = "The Catcher in the Ryes", Genre = "Fiction", Author = "J.D. Salinger"};
        Elastic es = new Elastic();
        es.IndexDocument(book, "test");
        PlaywrightTest.Crawler crawler = new PlaywrightTest.Crawler();
        await crawler.CrawlTest(120, 15);
    }
}
