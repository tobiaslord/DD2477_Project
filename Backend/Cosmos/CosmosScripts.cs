using Cosmos;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Models;

class CosmosScripts {

    public async Task PerformRemoveDuplicates() {
        using (var db = CosmosDBFactory.GetBookDB()) {
            string query = "SELECT d.id FROM (SELECT c.id, COUNT(1) as counts FROM c GROUP BY c.id) as d WHERE d.counts > 1";
            var queryable = db.ExecuteSQL<SimpleBook>(query);;

            while (queryable.HasMoreResults) {
                FeedResponse<SimpleBook> obj = await queryable.ReadNextAsync();
                foreach (SimpleBook book in obj) {
                    await db.DeleteDocument(book.id, false);
                }
            }
        }
    }

    public async Task<List<string>> LoadBookIdsFromUsers() {
        var ids = new HashSet<string>();
        using (var db = CosmosDBFactory.GetUserDB()) {
            var enumerator = db.GetEnumerator();
            while (enumerator.HasMoreResults) {
                var response = await enumerator.ReadNextAsync().ConfigureAwait(false);
                foreach (var item in response.Resource) {
                    foreach (var rating in item.ratings) {
                        ids.Add(rating.bookId);
                    }
                }
            }
        }
        return ids.ToList();
    }

}