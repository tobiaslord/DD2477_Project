using Microsoft.Playwright;
using System.Runtime.InteropServices;

class Program
{
    static async Task Main(string[] args)
    {
        PlaywrightTest.Crawler crawler = new PlaywrightTest.Crawler();
        var x = crawler.Crawl();
        await x;
    }
}

