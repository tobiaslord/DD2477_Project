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
using System.Diagnostics;
using System.Text.Json;

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
            users = ElasticIndex.GetAllUsers2();
            // userVectors = ElasticIndex.GetUserVectors(users);
            string json = File.ReadAllText("C:\\Users\\chickenthug\\Desktop\\test\\PlaywrightTest\\user_vectors.json");
            userVectors = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, double>>>(json);
        }

        public List<Book> GraphicSearch(string query, User user)
        {
            List<SearchResponse> books = ElasticIndex.BetterSearch(query);
            if (books.Count() == 0)
                return new List<Book>();

            double maxRatingCount = Math.Log(books.Max(sr => sr.Book.ratingCount));
            double norm = books.First().Score ?? 0;
            if (user.ratings.Count() == 0)
            {
                foreach (SearchResponse sbook in books)
                {
                    Book book = sbook.Book;
                    sbook.Score = (sbook.Score / norm + Math.Log(book.ratingCount) / maxRatingCount) / 2;
                }
                return books
                .OrderBy(a => a.Score)
                .Select(a => a.Book)
                .Reverse()
                .ToList();
            }
                
            Dictionary<string, double> user_vec = ElasticIndex.GetUserVector(user);

            if (user.ratings is not null && user.ratings.Count() > 0)
                user_vec = ExtendUserVector(user_vec);

            // Only for debugging 
            var x = user_vec.OrderBy(x => x.Value).ToList();
            
            foreach (SearchResponse sbook in books)
            {
                Book book = sbook.Book;
                Dictionary<string, double> book_vec = ElasticIndex.GetBookVector(book.genres, 0.7);
                double sim = Utils.CosineSimilarityEuclidian(book_vec, user_vec);
                sbook.Score = (3*sim + sbook.Score / norm + Math.Log(book.ratingCount)/ maxRatingCount) / 5;
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
