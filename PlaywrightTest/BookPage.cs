using Microsoft.Playwright;

class BookPage {
    private IPage page;
    private string bookId = string.Empty;
    public string title = string.Empty;
    public BookPage(IPage page, string id) {
        this.page = page;
        this.bookId = id;
    }
    public async Task SetPageData() {
        await this.LoadPage();
        await this.setTitle();
    }
    private async Task LoadPage() {
        await page.GotoAsync($"https://www.goodreads.com/book/show/{this.bookId}");
    }

    private async Task setTitle() {
        var el = page.GetByTestId("bookTitle");
        this.title = await el.InnerTextAsync();
    }
}