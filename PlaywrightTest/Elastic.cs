using Newtonsoft.Json;
using Models;
using Nest;


namespace ElasticSearchNamespace
{
    public class Elastic
    {
        public ElasticClient _client = new ElasticClient(new ConnectionSettings(new Uri("http://localhost:9200"))); // .DefaultIndex("users"));

        public void IndexDocument<T>(T document, string id, string indexName) where T : class
        {
            var indexResponse = _client.Index(document, i => i.Index(indexName));
            if (!indexResponse.IsValid)
            {
                throw new Exception($"Failed to index document with id '{id}'");
            }
        }

        public T GetDocument<T>(string id) where T : class
        {
            var getResponse = _client.Get<T>(id);
            if (!getResponse.IsValid || getResponse.Source == null)
            {
                throw new Exception($"Failed to retrieve document with id '{id}'");
            }
            return getResponse.Source;
        }

        public void IndexAllBooks()
        {
            string json = File.ReadAllText("books2.json");

            List<SimpleBook> books = JsonConvert.DeserializeObject<List<SimpleBook>>(json);
            Console.WriteLine("Number of books: " + books.Count);
            int i = 0;
            foreach (SimpleBook book in books)
            {
                if (book.author is null)
                {
                    continue;
                }
                i++;
                IndexDocument(book, book.id, "books");
                if (i % 1000 == 0)
                {
                    Console.WriteLine("Number of books: " + i);
                }
            }

        }

        public void IndexAllUsers()
        {
            string json = File.ReadAllText("users.json");
            List<User> users = JsonConvert.DeserializeObject<List<User>>(json);
            Console.WriteLine("Number of user: " + users.Count);
            int i = 0;
            foreach (User user in users)
            {
                List<BookRating> ratings = user.ratings;
                
                if (ratings is null || ratings.Count == 0) // Skip users without ratings
                {
                    continue;
                }
                i++;
                IndexDocument(user, user.id, "users");
                if (i % 100 == 0)
                {
                    Console.WriteLine("Number of users indexed: " + i);
                }
            }
        }

        public List<SimpleBook> Search(string query)
        {
            var searchResponse = _client.Search<SimpleBook>(s => s
                .Query(q => q
                    .Bool(b => b
                        .Should(sh => sh
                            .Match(m => m
                                .Field(f => f.title)
                                .Query(query)
                            ),
                            sh => sh
                            .Match(m => m
                                .Field(f => f.description)
                                .Query(query)
                            ),
                            sh => sh
                            .Match(m => m
                                .Field(f => f.genres)
                                .Query(query)
                            )
                        )
                    )
                )
            );

            if (!searchResponse.IsValid)
            {
                throw new Exception("Error searching for documents: " + searchResponse.DebugInformation);
            }

            var documents = searchResponse.Documents.ToList();
            foreach (var document in documents)
            {
                Console.WriteLine($"Book ID: {document.id}");
                Console.WriteLine($"Title: {document.title}");
                Console.WriteLine($"Description: {document.description}");
                Console.WriteLine($"Image URL: {document.imageUrl}");
                Console.WriteLine($"Rating: {document.rating}");
                Console.WriteLine($"Rating count: {document.ratingCount}");
                Console.WriteLine($"Review count: {document.reviewCount}");
                Console.WriteLine($"Genres: {string.Join(", ", document.genres)}");
                Console.WriteLine("----------------------");
            }

            return documents;



        }

        public List<SimpleBook> BetterSearch(string query)
        {
            var searchResponse = _client.Search<SimpleBook>(s => s
                    .Query(q => q
                        .Bool(b => b
                            .Should(sh => sh
                                .Match(m => m
                                    .Field(f => f.title)
                                    .Query(query)
                                    .Boost(2)
                                ),
                                sh => sh
                                .Match(m => m
                                    .Field(f => f.author)
                                    .Query(query)
                                    .Boost(1)
                                ),
                                sh => sh
                                .Match(m => m
                                    .Field(f => f.description)
                                    .Query(query)
                                    .Boost(0.1)
                                ),
                                sh => sh
                                .Match(m => m
                                    .Field(f => f.genres)
                                    .Query(query)
                                    .Boost(1)
                                )
                            )
                        )
                    )
                );

            if (!searchResponse.IsValid)
            {
                throw new Exception("Error searching for documents: " + searchResponse.DebugInformation);
            }

            var documents = searchResponse.Documents.ToList();
            foreach (var document in documents)
            {
                Console.WriteLine($"Book ID: {document.id}");
                Console.WriteLine($"Title: {document.title}");
                Console.WriteLine($"Description: {document.description}");
                Console.WriteLine($"Image URL: {document.imageUrl}");
                Console.WriteLine($"Rating: {document.rating}");
                Console.WriteLine($"Rating count: {document.ratingCount}");
                Console.WriteLine($"Review count: {document.reviewCount}");
                Console.WriteLine($"Genres: {string.Join(", ", document.genres)}");
                Console.WriteLine("----------------------");
            }

            return documents;
        }


       }
    public class User
    {
        public string id { get; set; }
        public List<BookRating> ratings { get; set; }
    }

    public class BookRating
    {
        public string bookId { get; set; }
        public int rating { get; set; }
        public int bookRatingCount { get; set; }
    }

}
