using Microsoft.Playwright;
using Models;

namespace Crawler.Pages;
public class UserReviewsPage
{
    private IPage page;
    private string userId;
    private List<Tuple<string, int, int>> reviews = new List<Tuple<string, int, int>>();
    public UserReviewsPage(IPage page, string userId)
    {
        this.page = page;
        this.userId = userId;
    }
    public async Task SetPageData()
    {
        await this.LoadPage();
        if (this.ValidPage())
        {
            await this.NavigatePages();
        }
    }
    private async Task LoadPage(int? listPage = null)
    {
        string url = $"https://www.goodreads.com/review/list/{this.userId}?shelf=read&print=true";

        if (listPage != null)
            url += $"&page={listPage}";

        await page.GotoAsync(url);
    }
    private bool ValidPage()
    {
        if (this.page.Url.Contains("/review/list/"))
            return true;

        return false;
    }
    private async Task ScrollToBottom()
    {
        await this.page.EvaluateAsync(
            @"var intervalID = setInterval(function () {
                var scrollingElement = (document.scrollingElement || document.body);
                scrollingElement.scrollTop = scrollingElement.scrollHeight;
            }, 200);"
        );
        float prev_height = 0;
        while (true)
        {
            var curr_height = await this.page.EvaluateAsync<float>(@"(window.innerHeight + window.scrollY)");
            if (prev_height == 0)
            {
                prev_height = curr_height;
                await Task.Delay(2000);
            }
            else if (prev_height == curr_height)
            {
                await this.page.EvaluateAsync("clearInterval(intervalID)");
                break;
            }
            else
            {
                prev_height = curr_height;
                await Task.Delay(2000);
            }
        }
    }
    private async Task NavigatePages()
    {
        int currentPage = 0;
        while (true)
        {
            currentPage++;
            await this.LoadPage(currentPage);

            var count = await this.page.Locator(".greyText.nocontent.stacked").CountAsync();
            if (count > 0)
                return;

            await this.SetReviews();
        }
    }
    private async Task SetReviews()
    {
        // await this.ScrollToBottom();

        var reviews = this.page.Locator(".bookalike.review");
        var els = await reviews.ElementHandlesAsync();

        foreach (var item in els)
        {
            // var review = await item.QuerySelectorAsync(".field.review .greyText");
            // if (review != null) continue; //User has not reviewed book.

            var stars = await item.QuerySelectorAllAsync(".field.rating .staticStar.p10");
            int rating = stars.Count;

            var href = await item.QuerySelectorAsync(".field.title .value > a");
            if (href == null) continue;

            var bookLink = await href.GetAttributeAsync("href") ?? string.Empty;
            var bookId = Utility.GetIdFromUrl(bookLink);

            var totalRatingEl = await item.QuerySelectorAsync(".field.num_ratings .value");
            if (totalRatingEl == null) continue;
            var totalRatings = Utility.ParseRatingCount(await totalRatingEl.InnerTextAsync());

            this.reviews.Add(new Tuple<string, int, int>(bookId, rating, totalRatings));
        }
    }
    public SimpleUser ToSimpleUser()
    {
        return new SimpleUser()
        {
            id = this.userId,
            ratings = this.reviews
                        .Select(r => new Rating
                        {
                            bookId = r.Item1,
                            rating = r.Item2,
                            bookRatingCount = r.Item3,
                        })
                        .ToList(),
        };
    }
}