namespace Models;
public class SimpleBook
{
    public string id { get; set; }
    public string bookId { get; set; } //Same as id. Just used in cosmos db...
    // public string contextId { get; set; } //All version of Harry Potter 1 should have the same contextId etc.
    public string author { get; set; }
    public string title { get; set; }
    public string description { get; set; }
    public string imageUrl { get; set; }
    public string authorUrl { get; set; }
    public float rating { get; set; }
    public int ratingCount { get; set; }
    public int reviewCount { get; set; }
    public List<string> genres { get; set; }
    public List<string> authors { get; set; }
    public List<string> authorUrls { get; set; }
}

public class BookReview
{
    public string userId { get; set; }
    public int reviewCount { get; set; }
}