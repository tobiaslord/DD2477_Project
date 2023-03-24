using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace PlaywrightTest
{
    internal class Crawler
    {
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
            Console.WriteLine(hungerGames);
        }

        public async Task CrawlTest(int visitCount, int workerCount) {
            var random = new Random();
            var tasks = new Task[workerCount];
            var queue = new ConcurrentQueue<string>();

            var watch = System.Diagnostics.Stopwatch.StartNew();
            for (int node = 0; node < visitCount; node++)
            {
                queue.Enqueue(random.Next(0, 8_500_000).ToString());
            }

            for (int worker = 0; worker < workerCount; worker++)
            {
                var nodeIds = new List<string>();
                tasks[worker] = CrawlNodes(nodeIds, queue);
            }

            Task.WaitAll(tasks);
            watch.Stop();

            Console.WriteLine("Job took: " + watch.ElapsedMilliseconds);
            Console.WriteLine("Average processing speed: " + watch.ElapsedMilliseconds / visitCount);
        }
        public async Task CrawlNodes(List<String> nodeids, ConcurrentQueue<string> queue) {
            Console.WriteLine("Worker started");
            var watch = System.Diagnostics.Stopwatch.StartNew();

            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync();
            var page = await browser.NewPageAsync();

            while (queue.TryDequeue(out string? id)) {
                if (id == null) {
                    Console.WriteLine("null?");
                    break;
                }

                var book = new BookPage(page, id);
                await book.SetPageData();

                if (book.title == string.Empty) {
                    Console.WriteLine("Failed: " + id);
                }
            }

            watch.Stop();
            Console.WriteLine("Worker took: " + watch.ElapsedMilliseconds);
        }
    }
}
