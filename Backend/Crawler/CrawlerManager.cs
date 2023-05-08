using System.Collections.Concurrent;
using System.Configuration;
using Microsoft.Playwright;

namespace Crawler;
public class CrawlerManager : IDisposable
{
    private ConcurrentQueue<string> _ipQueue = new ConcurrentQueue<string>();
    private ConcurrentQueue<string> _idQueue = new ConcurrentQueue<string>();
    private string ipEndpoint;
    private int numberOfCrawlers;
    private int idCount;
    private ICrawler crawler;
    private IPlaywright? playwright;

    public CrawlerManager(ICrawler crawler, int numberOfCrawlers) {
        this.ipEndpoint = ConfigurationManager.AppSettings.Get("IPEndpoint") ?? "";
        this.crawler = crawler;
        this.numberOfCrawlers = numberOfCrawlers;
    }

    public async Task Setup(List<string> ids) {
        this.AddIdsToQueue(ids);
        await this.GetIPs();
        this.playwright = await Playwright.CreateAsync();
    }

    private void AddIdsToQueue(List<string> ids) {
        this.idCount = ids.Count;
        foreach (var id in ids)
        {
            this._idQueue.Enqueue(id);
        }
    }

    private async Task GetIPs() {
        Console.WriteLine("# Getting IPs:");
        using (var client = new HttpClient()) {
            using (var s = client.GetStreamAsync(this.ipEndpoint)) {
                using (var sr = new StreamReader(await s)) {
                    var line = await sr.ReadLineAsync();
                    while (line != null) {
                        var ip = line.Trim();
                        Console.WriteLine("- " + ip);
                        this._ipQueue.Enqueue(ip);
                        line = await sr.ReadLineAsync();
                    }
                }
            }
        }
        for (int i = 0; i < 50; i++)
        {
            this._ipQueue.TryDequeue(out string ip);
        }
        Console.WriteLine("# Done");
    }

    public async Task StartWorkers() {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var tasks = new Task[this.numberOfCrawlers];
        for (int i = 0; i < this.numberOfCrawlers; i++) {
            tasks[i] = DoWork();
        }

        Task.WaitAll(tasks);
        Console.WriteLine("Job took: " + watch.ElapsedMilliseconds);
        Console.WriteLine("Average processing speed: " + watch.ElapsedMilliseconds / this.idCount);
    }
    public async Task DoWork() {
        while (!this._idQueue.IsEmpty) {
            await using (var browser = await this.playwright.Chromium.LaunchAsync(await GetBrowserOptions()))
            {
                var context = await browser.NewContextAsync();
                await this.crawler.Crawl(this._idQueue, context);
                await context.DisposeAsync();
                await browser.DisposeAsync();
            }
        }
    }

    private async Task<BrowserTypeLaunchOptions> GetBrowserOptions() {
        if (this._ipQueue.TryDequeue(out string? ip)) {
            return new BrowserTypeLaunchOptions() {
                Proxy = new Proxy() {
                    Server = ip,
                }
            };
        }
        //If no proxy IP is available, use own IP
        await this.GetIPs();
        return new BrowserTypeLaunchOptions() {};
    }
    public void Dispose() {
        if (this.playwright != null)
            this.playwright.Dispose();
    }
}