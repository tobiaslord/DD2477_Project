using System.Collections.Concurrent;
using Crawler.Pages;
using Models;
using Cosmos;
using Microsoft.Playwright;

namespace Crawler.Crawlers;
public class UserReviewCrawler : ICrawler
{
    private Random random = new Random();
    private Tracker tracker = new Tracker();
    public async Task Crawl(ConcurrentQueue<string> queue, IBrowserContext context)
    {
        //1. Get book id
        //2. Load book and get reviews
        //3. Get available reviews, order users by number of reviews
        //4. Load user reviews until ~100 reviews
        //5. For each user, save user id and list of (score, bookId)
        var page = await context.NewPageAsync();
        using (var db = CosmosDBFactory.GetDB<SimpleUser>(CosmosCollection.Users)) {
            await GetBookReviews(queue, db, page);
        }
    }
    private async Task GetBookReviews(ConcurrentQueue<string> queue, CosmosDB<SimpleUser> db, IPage page) {
        while (queue.TryDequeue(out string? bookId)) {
            tracker.PrintStatus(queue, 10);
            this.tracker.OnRequest();

            try {
                var bookPage = new BookPage(page, bookId);
                await bookPage.SetReviewData();
                var reviews = bookPage.GetReviews();
                reviews = reviews
                            .OrderBy(r => r.reviewCount)
                            .ToList();

                await GetUserReviews(db, page, reviews);
                Console.WriteLine("##bookId##" + bookId);
            }
            catch (Exception ex) {
                Console.WriteLine("Error! Last bookId: " + bookId);
                Console.WriteLine(ex.Message);

                queue.Enqueue(bookId);

                break;
            }
        }
    }

    private async Task GetUserReviews(CosmosDB<SimpleUser> db, IPage page, List<BookReview> reviews) {
        int addedBooks = 0;
        int idx = 0;
        while (addedBooks < 100 && reviews.Count > idx) {
            var review = reviews.ElementAt(idx);
            idx++;
            if (addedBooks > 0 && (review.reviewCount + addedBooks) > 200) {
                break;
            }

            try {
                var existingUser = await db.GetDocument(review.userId);
                if (existingUser != null) {
                    continue;
                }

                var reviewPage = new UserReviewsPage(page, review.userId);
                await reviewPage.SetPageData();
                var user = reviewPage.ToSimpleUser();

                if (user.ratings.Count == 0)
                    continue;

                user.ratings = user.ratings.Where(r => r.rating > 0).ToList();

                if (user.ratings.Count != review.reviewCount) {
                    Console.WriteLine("Should be equal!, userId: " + review.userId);
                }

                await db.PostDocument(user);
                Console.WriteLine("Added " + user.ratings.Count + " ratings");
                addedBooks += user.ratings.Count;
            }
            catch (Exception ex) {
                Console.WriteLine("Error GetUserReviews! Last userId: " + review.userId);
                Console.WriteLine(ex.Message);
            }
        }
    }
}