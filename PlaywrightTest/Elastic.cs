using Newtonsoft.Json;
using Models;
using Nest;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Utils = Vectors.Vectors;
using User = Models.SimpleUser;
using BookRating = Models.Rating;


namespace ElasticSearchNamespace
{
    public class ElasticIndex
    {

        ElasticsearchClient _client;
        public ElasticIndex()
        {
            var fingerprint = Environment.GetEnvironmentVariable("FINGERPRINT");
            var un = Environment.GetEnvironmentVariable("USERNAME");
            var pw = Environment.GetEnvironmentVariable("PASSWORD");


            _client = new ElasticsearchClient(new ElasticsearchClientSettings(new Uri("https://localhost:9200"))
                                                                .CertificateFingerprint(fingerprint)
                                                                .Authentication(new BasicAuthentication(un, pw)));

        }



        //public ElasticClient _client = new ElasticClient(new ConnectionSettings(new Uri("http://localhost:9200"))
        //        .BasicAuthentication("elastic", "HrdVMU0GgTzrZPi*8Vo1")); // .DefaultIndex("users"));

        public void IndexDocument<T>(T document, string id, string indexName) where T : class
        {
            var indexResponse = _client.Index(document, i => i.Index(indexName));
            if (!indexResponse.IsValidResponse)
            {
                throw new Exception($"Failed to index document with id '{id}'");
            }
        }

        public T GetDocument<T>(string id, string indexName) where T : class
        {
            var getResponse = _client.Get<T>(id, g => g.Index(indexName));
            //var getResponse = _client.Get<T>(id);
            if (!getResponse.IsValidResponse || getResponse.Source == null)
            {
                throw new Exception($"Failed to retrieve document with id '{id}'");
            }
            return getResponse.Source;
        }

        public void IndexAllBooks()
        {
            // string json = File.ReadAllText("D:\\programming\\DD2477_Project\\PlaywrightTest\\books.json");
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
            string json = File.ReadAllText("D:\\programming\\DD2477_Project\\PlaywrightTest\\users.json");
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
                // user_rep[genre] = Math.Max(user_rep[genre], 0);
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
                .Size(1000)
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

            if (!searchResponse.IsValidResponse)
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

        public List<SearchResponse> BetterSearch(string query)
        {
            var searchResponse = _client.Search<SimpleBook>(s => s
                    .Index("books")
                    .Size(50)
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
                                    .Boost(0.1f)
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

            if (!searchResponse.IsValidResponse)
            {
                throw new Exception("Error searching for documents: " + searchResponse.DebugInformation);
            }

            var documentsWithScores = searchResponse.Hits.Select(hit => new SearchResponse{ Score = hit.Score, Book = hit.Source }).ToList();
       

            return documentsWithScores;
        }

        public List<SimpleBook> GraphicSearch(string query, User user)
        {
            List<SearchResponse> books = BetterSearch(query);

            if (user.ratings.Count() == 0)
                return books.Select(a => a.Book).ToList();

            if (books.Count() == 0)
                return new List<SimpleBook>();

            Dictionary<string, double> user_vec = GetUserVector(user);
            double norm = books.First().Score ?? 0;
            foreach (SearchResponse sbook in books)
            {
                SimpleBook book = sbook.Book;
                Dictionary<string, double> book_vec = GetBookVector(book.genres, 0.7);
                double sim = Utils.CosineSimilarityEuclidian(book_vec, user_vec);
                sbook.Score = (sim + sbook.Score / norm) / 2;
            }
            List<SimpleBook> s = books.OrderBy(a => a.Score).Select(a=>a.Book).Reverse().ToList();
            return s;
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


    public class SearchResponse
    {
        public SimpleBook Book { get; set; }
        public double? Score { get; set; }
    }

}
