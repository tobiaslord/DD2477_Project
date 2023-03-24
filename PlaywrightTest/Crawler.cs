using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            Console.WriteLine(hungerGames.title);
        }
    }
}
