
using Microsoft.Playwright;
using System.Collections.Concurrent;

namespace Crawler;
public interface ICrawler
{
    Task Crawl(ConcurrentQueue<string> ids, IBrowserContext context);
}
