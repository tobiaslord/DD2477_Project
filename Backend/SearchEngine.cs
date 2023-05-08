using ElasticSearchNamespace;
using Book = Models.SimpleBook;
using User = Models.SimpleUser;
using Utils = Vectors.Vectors;

namespace Backend
{
    public class SearchEngine
    {
        ElasticIndex ElasticIndex;
        Dictionary<string, Dictionary<string, double>> userVectors;
        Dictionary<string, User> users;

        public SearchEngine()
        {
            ElasticIndex = new ElasticIndex();
            users = ElasticIndex.GetAllUsers();
            userVectors = ElasticIndex.GetAllUserVectors();
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
                .OrderByDescending(a => a.Score)
                .Select(a => a.Book)
                .Take(52)
                .ToList();

            }

            Dictionary<string, double> user_vec = ElasticIndex.GetUserVector(user);

            if (user.ratings is not null && user.ratings.Count() > 0)
                user_vec = ExtendUserVector(user_vec);

            foreach (SearchResponse sbook in books)
            {
                Book book = sbook.Book;
                Dictionary<string, double> book_vec = ElasticIndex.GetBookVector(book.genres, 0.7);
                double sim = Utils.CosineSimilarityEuclidian(book_vec, user_vec);
                sbook.Score = (3 * sim + sbook.Score / norm + Math.Log(book.ratingCount) / maxRatingCount) / 5;
            }

            List<Book> s = books
                .OrderByDescending(a => a.Score)
                .Select(a => a.Book)
                .Take(52)
                .ToList();

            return s;
        }

        public Dictionary<string, double> ExtendUserVector(Dictionary<string, double> mainUserVector)
        {

            var topSimUsers = users
                .ToList()
                .OrderByDescending(x => Utils.CosineSimilarityEuclidian(mainUserVector, userVectors[x.Key]))
                .Select(x => x.Value)
                .Take(3)
                .ToList();

            double[] similarUserWeights = { 0.4, 0.2, 0.1 };



            for (int i = 0; i < 3; i++)
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
