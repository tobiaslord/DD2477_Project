using Microsoft.Playwright;
using Models;
using System.Collections.Concurrent;
using Cosmos;
using Crawler.Pages;

namespace Crawler.Crawlers;
internal class BookCrawler : ICrawler
{
    private Random random = new Random();
    private Tracker tracker = new Tracker();
    public async Task Crawl()
    {
        // Installs chromium
        var exitCode = Microsoft.Playwright.Program.Main(new[] { "install" });
        if (exitCode != 0)
        {
            throw new Exception($"Playwright exited with code {exitCode}");
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        var page = await browser.NewPageAsync();


        var hungerGames = new BookPage(page, "2767052");
        await hungerGames.SetPageData();
        var item = hungerGames.ToSimpleBook();
        using (var cosmos = CosmosDBFactory.GetDB<SimpleBook>(CosmosCollection.Books)) {
            await cosmos.PostDocument(item);
        }
        Console.WriteLine(hungerGames);
    }

    public async Task CrawlTest(List<int> ids, int workerCount) {
        var random = new Random();
        var tasks = new Task[workerCount];
        var queue = new ConcurrentQueue<string>();

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();

        var watch = System.Diagnostics.Stopwatch.StartNew();
        foreach (var id in ids)
        {
            queue.Enqueue(id.ToString());
        }


        for (int worker = 0; worker < workerCount; worker++)
        {
            var context = await browser.NewContextAsync();
            tasks[worker] = Crawl(queue, context);
        }

        Task.WaitAll(tasks);
        watch.Stop();

        Console.WriteLine("Job took: " + watch.ElapsedMilliseconds);
        Console.WriteLine("Average processing speed: " + watch.ElapsedMilliseconds / ids.Count);
    }
    public async Task Crawl(ConcurrentQueue<string> queue, IBrowserContext context) {
        Console.WriteLine("Worker started");

        var watch = System.Diagnostics.Stopwatch.StartNew();
        var page = await context.NewPageAsync();

        DateTime nextCokieClear() => DateTime.UtcNow.AddSeconds(30);
        void stop() {
            watch.Stop();
            Console.WriteLine("Worker took: " + watch.ElapsedMilliseconds);
        }

        using (var db = CosmosDBFactory.GetBookDB()) {
            DateTime nextCookieClear = nextCokieClear();
            while (queue.TryDequeue(out string? id)) {
                tracker.PrintStatus(queue, 50);

                // if (await db.GetDocument(id, true) != null) {
                //     Console.WriteLine("Bookid " + id + " already exists");
                //     continue;
                // }

                if (DateTime.UtcNow > nextCookieClear) {
                    await context.ClearCookiesAsync();
                    nextCookieClear = nextCokieClear();
                }

                try {

                    if (string.IsNullOrEmpty(id)) {
                        throw new Exception("Id is null");
                    }
                    this.tracker.OnRequest();

                    var bookPage = new BookPage(page, id);
                    await bookPage.SetPageData();
                    var simpleBook = bookPage.ToSimpleBook();

                    if (simpleBook.author == null && await bookPage.IsErrorPage()) {
                        throw new Exception("Rate limited, shutting down worker");
                    } else if (!string.IsNullOrEmpty(simpleBook.bookId)) {
                        // await db.DeleteDocument(id, false);
                        await db.PostDocument(simpleBook);
                    }
                    else {
                        throw new Exception("Failed id: " + id);
                    }
                }
                catch (Exception ex) {
                    Console.WriteLine("Error Page! Last id: " + id);
                    Console.WriteLine(ex.Message);
                    this.tracker.PrintRollingAverage(60);
                    if (!string.IsNullOrEmpty(id))
                        queue.Enqueue(id);

                    break;
                }
            }
            stop();
        }
    }
}
