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

        public T GetDocument<T>(string id, string indexName) where T : class
        {
            var getResponse = _client.Get<T>(id, g => g.Index(indexName));
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
        // Calculates the vector representation of users
        public Dictionary<string, double> GetUserVector(User user)
        {
            Dictionary<string, double> user_rep = new Dictionary<string, double>();
            double avg = user.ratings.Average(b => b.rating);
            foreach (BookRating br in user.ratings)
            {
                double scalar = avg - br.rating;
                
                if (scalar == 0)
                {
                    scalar = 0.1;
                }
                if (scalar < 0)
                {
                    //Maybe dont weigh negative feedback as much
                }
                List<string> genres = GetDocument<SimpleBook>(br.bookId, "books").genres;
                Dictionary<string, double> book_rep = GetBookVector(genres, 0.8);
                foreach (string genre in book_rep.Keys)
                {
                    if (!user_rep.ContainsKey(genre))
                    {
                        user_rep[genre] = 0;
                    }
                    user_rep[genre] += scalar*book_rep[genre];
                }
            }
            double length = Math.Sqrt(user_rep.Values.Sum(x => x * x));
 
            foreach (string genre in user_rep.Keys)
            {
                user_rep[genre] /= length;
            }
            return user_rep;
        }

        public Dictionary<string, double> GetBookVector(List<string> genres, double decayFactor)
        {
            Dictionary<string, double> vector_rep = new Dictionary<string, double>();
            double length = 0;
            for (int i=0; i<genres.Count; i++)
            {
                double weight = Math.Pow(decayFactor, i);
                vector_rep.Add(genres[i], weight);
                length += weight * weight;
            }
            foreach (string genre in vector_rep.Keys)
            {
                vector_rep[genre] /= length;
            }
            return vector_rep;
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
                    .Index("books")
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
                                    .Boost(4)
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
       
        
        public double GetSimilarity(Dictionary<String, double> first, Dictionary<String, double> second)
        {
            var similarity = 0.0;
            foreach (string key in first.Keys)
            {
                if (second.ContainsKey(key))
                {
                    similarity += first[key] * second[key];
                }
            }
            return similarity;
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
