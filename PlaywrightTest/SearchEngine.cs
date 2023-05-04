using Crawler.Crawlers;
using ElasticSearchNamespace;
using Microsoft.Azure.Cosmos;
using Models;
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
            List<SearchResponse> books = ElasticIndex.BetterSearch(query);

            if (user.ratings.Count() == 0)
                return books.Select(a => a.Book).ToList();

            if (books.Count() == 0)
                return new List<Book>();

            Dictionary<string, double> user_vec = ElasticIndex.GetUserVector(user);

            if (user.ratings is not null && user.ratings.Count() > 0)
                user_vec = ExtendUserVector(user_vec);

            // Only for debugging 
            var x = user_vec.OrderBy(x => x.Value).ToList();

            double norm = books.First().Score ?? 0;
            foreach (SearchResponse sbook in books)
            {
                Book book = sbook.Book;
                Dictionary<string, double> book_vec = ElasticIndex.GetBookVector(book.genres, 0.7);
                double sim = Utils.CosineSimilarityEuclidian(book_vec, user_vec);
                sbook.Score = (sim + sbook.Score / norm) / 2;
            }

            List<Book> s = books
                .OrderBy(a => a.Score)
                .Select(a => a.Book)
                .Reverse()
                .ToList();

            return s;
        }

        public Dictionary<string, double> ExtendUserVector(Dictionary<string, double> mainUserVector)
        {
            
            var topSimUsers = users
                .ToList()
                .OrderByDescending(x => Utils.CosineSimilarityEuclidian(mainUserVector, userVectors[x.Key]))
                .Select(x => x.Value)
                .ToList()
                .GetRange(0, 5)
                .ToList();

            double[] similarUserWeights = { 0.4, 0.3, 0.2, 0.1, 0.1 };



            for (int i = 0; i < 5; i++)
            {
                foreach (var genreRating in userVectors[topSimUsers[i].id])
                {
                    if (mainUserVector.ContainsKey(genreRating.Key) == false)
                        mainUserVector.Add(genreRating.Key, 0);
                    mainUserVector[genreRating.Key] += genreRating.Value * similarUserWeights[i];
                }
            }
            

            return mainUserVector;
        }


    }
}
