using Crawler.Crawlers;
using ElasticSearchNamespace;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Book = Models.SimpleBook;
using User = Models.SimpleUser;
using Utils = Vectors.Vectors;

namespace PlaywrightTest
{
    public class SearchEngine
    {
        ElasticIndex ElasticIndex;
        Dictionary<string, Dictionary<string, double>> userVectors;
        Dictionary<string, User> users;

        public SearchEngine() 
        {
            var root = Directory.GetCurrentDirectory();
            var dotenv = System.IO.Path.Combine(root, ".env");
            DotEnv.Load(dotenv);

            ElasticIndex = new ElasticIndex();
            users = ElasticIndex.GetAllUsers();
            userVectors = ElasticIndex.GetUserVectors(users);
        }

        public List<Book> GraphicSearch(string query, User user)
        {
            return ElasticIndex.GraphicSearch(query, user);
        }

        public User ExtendUser(User user)
        {
            var mainUserVector = ElasticIndex.GetUserVector(user);

            var topUserId = userVectors
                .ToList()
                .OrderBy(s => Utils.CosineSimilarityEuclidian(s.Value, mainUserVector))
                .Reverse()
                .First()
                .Key;

            users.TryGetValue(topUserId, out var userVector);

            return userVector;
        }


    }
}
