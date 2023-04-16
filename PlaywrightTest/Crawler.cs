using Microsoft.Playwright;
using SimpleBookNamespace;
using System.Collections.Concurrent;

namespace PlaywrightTest
{
    public interface ICrawler {
        Task Crawl(ConcurrentQueue<string> ids, IBrowserContext context);
    }
    internal class Crawler : ICrawler
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
            using (var cosmos = new CosmosDB<SimpleBook>()) {
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

            using (var db = new CosmosDB<SimpleBook>()) {
                DateTime nextCookieClear = nextCokieClear();
                while (queue.TryDequeue(out string? id)) {
                    if (queue.Count % 50 == 0) {
                        Console.WriteLine(queue.Count + " nodes left");
                        this.tracker.PrintRollingAverage(30);
                    }

                    if (DateTime.UtcNow > nextCookieClear) {
                        await context.ClearCookiesAsync();
                        nextCookieClear = nextCokieClear();
                    }

                    try {

                        if (string.IsNullOrEmpty(id)) {
                            throw new Exception("Invalid id: " + id);
                        }
                        this.tracker.OnRequest();

                        var bookPage = new BookPage(page, id);
                        await bookPage.SetPageData();
                        var simpleBook = bookPage.ToSimpleBook();

                        if (simpleBook.author == null && await bookPage.IsErrorPage()) {
                            throw new Exception("Rate limited, shutting down worker");
                        } else {
                            await db.PostDocument(simpleBook);
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
}
