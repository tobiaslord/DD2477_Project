using System.Collections.Generic;

namespace Models;

public class SimpleUser {
    public string id { get; set; }
    public List<Rating> ratings { get; set; } = new List<Rating>();
}

public class Rating {
    public string bookId { get; set; }
    public int rating { get; set; }
    public int bookRatingCount { get; set; }
}