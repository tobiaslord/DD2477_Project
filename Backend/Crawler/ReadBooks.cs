

using Cosmos;
using Models;

namespace Crawler;
public class BookReader
{
    public async Task GetTop(int n)
    {
        using (var db = CosmosDBFactory.GetDB<SimpleUser>(CosmosCollection.Users))
        {
        }
    }
}