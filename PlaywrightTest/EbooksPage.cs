using Microsoft.Playwright;

class EbooksPage {
    private IPage page;
    public EbooksPage(IPage page) {
        this.page = page;
    }
}