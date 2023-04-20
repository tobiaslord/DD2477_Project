using PlaywrightTest;
using ElasticSearchNamespace;

class Program
{
    static async Task Main(string[] args)
    {
        Elastic es = new Elastic();
        es.IndexAll();
        Environment.Exit(0);
        Console.ReadKey();

        // PlaywrightTest.Crawler crawler = new PlaywrightTest.Crawler();
        // await crawler.CrawlTest(120, 15);
    }
}
