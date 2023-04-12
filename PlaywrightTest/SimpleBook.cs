namespace SimpleBookNamespace
{
    public class SimpleBook
    {
        public int bookId { get; set; }
        public string author { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string imageUrl { get; set; }
        public float rating { get; set; }
        public int ratingCount { get; set; }
        public int reviewCount { get; set; }
        public List<string> genres { get; set; }
    }
}
