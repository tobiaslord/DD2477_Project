using Microsoft.Playwright;
using Models;

namespace Crawler.Pages;
class BookPage
{
    private IPage page;
    private string bookId = string.Empty;
    // private string contextId = string.Empty;
    private string title = string.Empty;
    private string description = string.Empty;
    private string imageUrl = string.Empty;
    private List<string> authorUrls = new List<string>();
    private string rating = string.Empty;
    private string ratingCount = string.Empty;
    private string reviewCount = string.Empty;
    private List<string> genres = new List<string>();
    private List<string> authors = new List<string>();
    private List<Tuple<string, int>> reviews = new List<Tuple<string, int>>();
    public BookPage(IPage page, string id)
    {
        this.page = page;
        this.bookId = id;
    }
    public async Task SetPageData()
    {
        await this.LoadPage();
        if (await this.TrySetTitle())
        {
            Task.WaitAll(new Task[] {
                // this.SetContextId(),
                this.SetDescription(),
                this.SetGenres(),
                this.SetAuthors(),
                this.SetAuthorUrls(),
                this.SetImageUrl(),
                this.SetRating(),
                this.SetRatingCount(),
                this.SetReviewCount(),
            });
        }
    }
    public async Task SetReviewData()
    {
        await this.LoadPage();
        if (await this.TrySetTitle())
        {
            await this.WaitForReviews();
            await this.SetUserReviews();
        }
    }
    public async Task<bool> IsErrorPage()
    {
        var error = page.Locator(".leftContainer.mediumText h1");
        if (await error.IsVisibleAsync())
        {
            var errorText = await error.InnerTextAsync();
            if (errorText == "page unavailable")
            {
                return true;
            }
        }
        return false;
    }
    public bool BookExists()
    {
        return !string.IsNullOrEmpty(this.title);
    }
    private async Task LoadPage()
    {
        await page.GotoAsync($"https://www.goodreads.com/book/show/{this.bookId}");
    }

    private async Task<bool> CheckIfErrorPage()
    {
        var error = page.GetByTestId("errorText");
        return await error.IsVisibleAsync();
    }

    private async Task<bool> TrySetTitle()
    {
        var stop = DateTime.Now.AddSeconds(2);
        var title = page.GetByTestId("bookTitle");
        while (DateTime.Now < stop && !await title.IsVisibleAsync())
        {
            var isError = await CheckIfErrorPage();
            if (isError)
                return false;
            await Task.Delay(250);
        }

        if (await title.IsVisibleAsync())
        {
            this.title = await title.InnerTextAsync();
            return true;
        }
        // this.title = "Error for id: " + this.bookId;
        return false;
    }

    // private async Task SetContextId() {
    //     var div = page.Locator(".BookPage__bookCover");
    //     this.contextId = await div.GetAttributeAsync("data-csa-c-s_id") ?? string.Empty;
    // }

    private async Task SetDescription()
    {
        var description = page.GetByTestId("description").Locator(".Formatted");
        this.description = await description.InnerTextAsync();
    }

    private async Task SetImageUrl()
    {
        var image = page.Locator(".BookPage__bookCover .BookCover__image .ResponsiveImage");
        this.imageUrl = await image.GetAttributeAsync("src") ?? String.Empty;
    }

    private async Task SetRating()
    {
        var rating = page.Locator(".BookPageMetadataSection__ratingStats .RatingStatistics__rating");
        this.rating = await rating.InnerTextAsync();
    }

    private async Task SetRatingCount()
    {
        var ratingCount = page.Locator(".BookPageMetadataSection__ratingStats").GetByTestId("ratingsCount");
        this.ratingCount = await ratingCount.InnerTextAsync();
    }

    private async Task SetReviewCount()
    {
        var reviewCount = page.Locator(".BookPageMetadataSection__ratingStats").GetByTestId("reviewsCount");
        this.reviewCount = await reviewCount.InnerTextAsync();
    }

    private async Task SetAuthors()
    {
        var authors = page.Locator(".ContributorLinksList .ContributorLink__name");
        var els = await authors.ElementHandlesAsync();
        foreach (var item in els)
        {
            string author = await item.InnerTextAsync();
            this.authors.Add(author);
        }
    }

    private async Task SetAuthorUrls()
    {
        var authors = page.Locator(".ContributorLinksList .ContributorLink");
        var els = await authors.ElementHandlesAsync();
        foreach (var item in els)
        {
            string author = await item.GetAttributeAsync("href") ?? string.Empty;
            this.authorUrls.Add(author);
        }
    }

    private async Task WaitForReviews()
    {
        var profiles = page.Locator("#ReviewsSection div.ReviewsList__listContext.ReviewsList__listContext--centered");
        await profiles.InnerTextAsync();
    }
    private async Task SetUserReviews()
    {
        var profiles = page.Locator(".BookPage__reviewsSection .ReviewsSection .ReviewCard__profile");
        var els = await profiles.ElementHandlesAsync();

        foreach (var item in els)
        {
            var userLink = await item.QuerySelectorAsync(".ReviewerProfile__name a");
            if (userLink == null) continue;
            var href = await userLink.GetAttributeAsync("href") ?? string.Empty;
            var userId = Utility.GetIdFromUrl(href);

            var reviewEl = await item.QuerySelectorAsync(".ReviewerProfile__meta > span");
            if (reviewEl == null) continue;
            var reviewText = await reviewEl.InnerTextAsync();
            var reviews = Utility.ParseReviewCount(reviewText);
            if (reviews == -1) continue;

            this.reviews.Add(new Tuple<string, int>(userId, reviews));
        }
    }

    private async Task SetGenres()
    {
        //Removed this to save time.
        // var showMoreButton = page.Locator(".BookPageMetadataSection__genres .Button__container .Button__labelItem");
        // if (await showMoreButton.IsVisibleAsync())
        //     await showMoreButton.ClickAsync();

        var genres = page.Locator(".BookPageMetadataSection__genreButton .Button__labelItem");
        var els = await genres.ElementHandlesAsync();
        foreach (var item in els)
        {
            string genre = await item.InnerTextAsync();
            this.genres.Add(genre);
        }
    }

    public override string ToString()
    {
        return this.bookId;
    }

    public SimpleBook ToSimpleBook()
    {
        if (string.IsNullOrEmpty(this.title))
        {
            return new SimpleBook()
            {
                id = this.bookId,
            };
        }

        return new SimpleBook()
        {
            id = this.bookId,
            bookId = this.bookId,
            // contextId = this.contextId,
            author = this.authors.FirstOrDefault() ?? string.Empty,
            authorUrl = this.authorUrls.FirstOrDefault() ?? string.Empty,
            title = this.title,
            description = this.description,
            imageUrl = this.imageUrl,
            rating = Utility.ParseDecimalString(this.rating),
            ratingCount = Utility.ParseRatingCount(this.ratingCount),
            reviewCount = Utility.ParseReviewCount(this.reviewCount),
            genres = this.genres,
            authors = this.authors,
            authorUrls = this.authorUrls,
        };
    }

    public List<BookReview> GetReviews()
    {
        return this.reviews
            .Select(r => new BookReview
            {
                userId = r.Item1,
                reviewCount = r.Item2,
            })
            .ToList();
    }
}