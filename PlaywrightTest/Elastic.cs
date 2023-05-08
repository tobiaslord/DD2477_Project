using Newtonsoft.Json;
using Models;
using Nest;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Utils = Vectors.Vectors;
using User = Models.SimpleUser;
using BookRating = Models.Rating;
using System.Diagnostics;
using Elastic.Clients.Elasticsearch.Core.MGet;
using NTextCat;

namespace ElasticSearchNamespace
{
    public class ElasticIndex
    {
        string base_path;
        ElasticsearchClient _client;
        public ElasticIndex()
        {
            var fingerprint = Environment.GetEnvironmentVariable("FINGERPRINT") ?? "";
            var un = Environment.GetEnvironmentVariable("USERNAME") ?? "";
            var pw = Environment.GetEnvironmentVariable("PASSWORD") ?? "";
            var address = Environment.GetEnvironmentVariable("ADDRESS") ?? "";
            base_path = "D:\\programming\\DD2477_Project\\PlaywrightTest\\";

            _client = new ElasticsearchClient(new ElasticsearchClientSettings(new Uri(address))
                                                               .CertificateFingerprint(fingerprint)
                                                               .Authentication(new BasicAuthentication(un, pw)));
    }


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
            if (!getResponse.IsValidResponse || getResponse.Source == null)
            {
                throw new Exception($"Failed to retrieve document with id '{id}'");
            }
            return getResponse.Source;
        }

        public void IndexAllBooks()
        {
            string json = File.ReadAllText(base_path + "books2.json");
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
            string json = File.ReadAllText(base_path + "users.json");
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

        public void CleanDatabase()
        {
            string json = File.ReadAllText(base_path + "books2.json");
            List<SimpleBook> books = JsonConvert.DeserializeObject<List<SimpleBook>>(json);
            Dictionary<string, string> unique = new Dictionary<string, string>();
            int count = 0;
            foreach (SimpleBook book in books)
            {
                var documentExistsResponse = _client.Exists<SimpleBook>(book.id, d => d.Index("books"));
                if (documentExistsResponse.Exists)
                {
                    // Document exists in the index
                    if (unique.ContainsKey(book.title))
                    {
                        if (unique[book.title] == book.author)
                        {
                            var deleteResponse = _client.Delete<SimpleBook>(book.id, d => d.Index("books"));
                            count++;
                        }
                    }
                    else
                    {
                        unique.Add(book.title, book.author);
                    }

                }
            }
            Console.WriteLine("COUNT: {0}", count);
        }

        public void CleanDatabaseEnglish()
        {
            string json = File.ReadAllText(base_path + "books2.json");
            List<SimpleBook> books = JsonConvert.DeserializeObject<List<SimpleBook>>(json);
            Dictionary<string, string> unique = new Dictionary<string, string>();
            int count = 0;
            int count2 = 0;
            var factory = new RankedLanguageIdentifierFactory();
            var identifier = factory.Load(base_path + "Core14.profile.xml");
            
            foreach (SimpleBook book in books)
            {
                var documentExistsResponse = _client.Exists<SimpleBook>(book.id, d => d.Index("books"));
                if (book.description is null || book.description.Length < 30)
                {
                    continue;
                }
                if (documentExistsResponse.Exists)
                {
                    // Document exists in the index
                    var languages = identifier.Identify(book.description);
                    var mostCertainLanguage = languages.FirstOrDefault();
                    if (mostCertainLanguage != null)
                    {
                        if (mostCertainLanguage.Item1.Iso639_3 != "eng")
                        {
                            var res = _client.Delete<SimpleBook>(book.id, d => d.Index("books"));
                            count++;
                        }
                    }
                    else
                    {
                        count2++;
                    }

                }
            }
            Console.WriteLine("Error Count: {0}, Non-english Count: {1}", count2, count);
        }


        // Calculates the vector representation of users
        public Dictionary<string, double> GetUserVector(User user)
        {
            Dictionary<string, double> user_rep = new Dictionary<string, double>();

            foreach (BookRating br in user.ratings)
            {
                if (br.bookId is null || br.bookId == "")
                    continue;
                
                try
                {
                    List<string> genres = GetDocument<SimpleBook>(br.bookId, "books").genres;
                    Dictionary<string, double> book_rep = GetBookVector(genres, 0.8);
                    
                    foreach (string genre in book_rep.Keys)
                    {
                        if (!user_rep.ContainsKey(genre))
                        {
                            user_rep[genre] = 0;
                        }
                        user_rep[genre] += book_rep[genre] * (br.rating - 2.5f);
                    }
                } 
                catch (Exception ex)
                {
                    Debug.WriteLine("Rated book not found: " + br.bookId);
                    continue;
                }
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
                    .Size(200)
                    .Query(q => q
                        .Bool(b => b
                            .Should(sh => sh
                                .Match(m => m
                                    .Field(f => f.title)
                                    .Query(query)
                                    .Boost(1)
                                ),
                                sh => sh
                                .Match(m => m
                                    .Field(f => f.author)
                                    .Query(query)
                                    .Boost(2)
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
                                    .Boost(1)
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



        public Dictionary<string, User> GetAllUsers()
        {
            return _client.Search<User>(u => u.Index("users").Size(200))
                .Hits
                .Select(u => new User { id = u.Source.id, ratings = u.Source.ratings })
                .ToDictionary(a => a.id);
        }

        public Dictionary<string, User> GetAllUsers2()
        {
            string json = File.ReadAllText(base_path + "users.json");
            List<User> users = JsonConvert.DeserializeObject<List<User>>(json);
            return users.ToDictionary(a => a.id);

        }

        public Dictionary<string, Dictionary<string, double>> GetUserVectors(Dictionary<string, User> users)
        {
            return users
                .Select(u => (u.Key, GetUserVector(u.Value)))
                .ToDictionary(a => a.Key, b => b.Item2);
        }
    }

    

    public class SearchResponse
    {
        public SimpleBook Book { get; set; }
        public double? Score { get; set; }
    }

}
