using Models;
namespace Cosmos;

public static class CosmosDBFactory
{
    private static string GetCollection(CosmosCollection collection)
    {
        switch (collection)
        {
            case CosmosCollection.Books:
                return "books-collection";
            case CosmosCollection.Users:
                return "user-collection";
            default:
                throw new NotImplementedException();
        }
    }
    public static CosmosDB<T> GetDB<T>(CosmosCollection collection)
    {
        return new CosmosDB<T>(GetCollection(collection));
    }
    public static CosmosDB<T> GetDB<T>(string collection)
    {
        return new CosmosDB<T>(collection);
    }
    public static CosmosDB<SimpleUser> GetUserDB()
    {
        return GetDB<SimpleUser>(CosmosCollection.Users);
    }
    public static CosmosDB<SimpleBook> GetBookDB()
    {
        return GetDB<SimpleBook>(CosmosCollection.Books);
    }

}