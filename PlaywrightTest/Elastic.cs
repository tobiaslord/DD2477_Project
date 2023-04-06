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
        public ElasticClient _client = new ElasticClient(new ConnectionSettings(new Uri("http://localhost:9200")).DefaultIndex("books"));

        public void IndexDocument<T>(T document, string id) where T : class
        {
            var indexResponse = _client.IndexDocument(document);
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



    }
    

    
}


