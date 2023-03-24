using Microsoft.Playwright;
using System.Linq;

class BookPage {
    private IPage page;
    private string bookId = string.Empty;
    public string title = string.Empty;
    public string description = string.Empty;
    public string imageUrl = string.Empty;
    public string rating = string.Empty;
    public string ratingCount = string.Empty;
    public string reviewCount = string.Empty;
    public List<string> genres = new List<string>();
    public BookPage(IPage page, string id) {
        this.page = page;
        this.bookId = id;
    }
    public async Task SetPageData() {
        await this.LoadPage();
        if (await this.TrySetTitle()) {
            Task.WaitAll(new Task[] {
                this.SetDescription(),
                this.SetGenres(),
                this.SetImageUrl(),
                this.SetRating(),
                this.SetRatingCount(),
                this.SetReviewCount(),
            });
        }
    }
    private async Task LoadPage() {
        await page.GotoAsync($"https://www.goodreads.com/book/show/{this.bookId}");
    }

    private async Task<bool> CheckIfErrorPage() {
        var error = page.GetByTestId("errorText");
        return await error.IsVisibleAsync();
    }

    private async Task<bool> TrySetTitle() {
        var stop = DateTime.Now.AddSeconds(2);
        var title = page.GetByTestId("bookTitle");
        while (DateTime.Now < stop && !await title.IsVisibleAsync()) {
            var isError = await CheckIfErrorPage();
            if (isError)
                return false;
            await Task.Delay(250);
        }

        if (await title.IsVisibleAsync()) {
            this.title = await title.InnerTextAsync();
            return true;
        }
        this.title = "Error for id: " + this.bookId;
        return false;
    }

    private async Task SetDescription() {
        var description = page.GetByTestId("description").Locator(".Formatted");
        this.description = await description.InnerTextAsync();
    }

    private async Task SetImageUrl() {
        var image = page.Locator(".BookPage__bookCover .BookCover__image .ResponsiveImage");
        this.imageUrl = await image.GetAttributeAsync("src") ?? String.Empty;
    }

    private async Task SetRating() {
        var rating = page.Locator(".BookPageMetadataSection__ratingStats .RatingStatistics__rating");
        this.rating = await rating.InnerTextAsync();
    }

    private async Task SetRatingCount() {
        var ratingCount = page.Locator(".BookPageMetadataSection__ratingStats").GetByTestId("ratingsCount");
        this.ratingCount = await ratingCount.InnerTextAsync();
    }

    private async Task SetReviewCount() {
        var reviewCount = page.Locator(".BookPageMetadataSection__ratingStats").GetByTestId("reviewsCount");
        this.reviewCount = await reviewCount.InnerTextAsync();
    }

    private async Task SetGenres() {
        var showMoreButton = page.Locator(".BookPageMetadataSection__genres .Button__container .Button__labelItem");
        if (await showMoreButton.IsVisibleAsync())
            await showMoreButton.ClickAsync();

        var genres = page.Locator(".BookPageMetadataSection__genreButton .Button__labelItem");
        var els = await genres.ElementHandlesAsync();
        foreach (var item in els)
        {
            string genre = await item.InnerTextAsync();
            this.genres.Add(genre);
        }
    }

    public override string ToString() {
        string val = string.Empty;
        val += this.title + "\n";
        val += this.description + "\n";
        val += this.imageUrl + "\n";
        val += string.Join( ",", this.genres) + "\n";
        val += this.rating + "\n";
        val += this.ratingCount + "\n";
        val += this.reviewCount + "\n";
        return val;
    }
}