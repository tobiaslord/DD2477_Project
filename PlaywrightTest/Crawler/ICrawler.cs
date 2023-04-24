
using System.Collections.Concurrent;
using Microsoft.Playwright;

namespace Crawler;
public interface ICrawler {
    Task Crawl(ConcurrentQueue<string> ids, IBrowserContext context);
}
