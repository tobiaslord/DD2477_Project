using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;
using SimpleBookNamespace;
using Nest;


namespace ElasticSearchNamespace
{
    public class Elastic
    {
        public ElasticClient _client = new ElasticClient(new ConnectionSettings(new Uri("http://localhost:9200")).DefaultIndex("books50k"));

        public void IndexDocument<T>(T document, string id) where T : class
        {
            var indexResponse = _client.IndexDocument(document);
            if (!indexResponse.IsValid)
            {
                throw new Exception($"Failed to index document with id '{id}'");
            }
            else
            {
                Console.WriteLine($"Succeded to index document with id '{id}'");
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

        public void IndexAll()
        {
            string json = File.ReadAllText("books.json");

            List<SimpleBook> books = JsonConvert.DeserializeObject<List<SimpleBook>>(json);
            Console.WriteLine("Number of books: " + books.Count);
            int i = 0;
            foreach (SimpleBook book in books)
            {
                IndexDocument(book, book.id);
                i++;
                if (i % 1000)
                {
                    Console.WriteLine("Number of books: " + books.Count);
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

    }
}
