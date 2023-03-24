

using Microsoft.Playwright;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");




using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync();
var page = await browser.NewPageAsync();

var hungerGames = new BookPage(page, "2767052");
await hungerGames.SetPageData();
Console.WriteLine(hungerGames.title);