using Microsoft.Azure.Cosmos;
using System.Configuration;

namespace Cosmos;
public class CosmosDB<T> : IDisposable
{
    private string connectionString;
    const string db = "books-database";
    private string collection;
    private CosmosClient? _client;
    private Container? _container;

    public CosmosDB(string collection)
    {
        this.collection = collection;
        this.connectionString = ConfigurationManager.AppSettings.Get("ConnectionString") ?? "";
        this.InitializeCosmosClient();
    }
    private void InitializeCosmosClient()
    {
        var client = new CosmosClient(connectionString);
        var database = client.GetDatabase(db);
        this._container = database.GetContainer(collection);

        if (this._container == null)
        {
            throw new Exception("Could not connect to CosmosDB");
        }
    }
    public async Task PostDocument(T doc)
    {
        if (doc == null)
        {
            Console.WriteLine("Doc was null");
        }
        if (_container == null)
        {
            throw new Exception("Container not initialized");
        }
        try
        {
            var result = await _container.UpsertItemAsync<T>(doc);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Could not save document", doc.ToString(), ex.ToString());
        }
    }

    public async Task<T?> GetDocument(string id, bool usePartitionKey = true)
    {
        if (string.IsNullOrEmpty(id))
        {
            Console.WriteLine("Id was null");
        }
        if (_container == null)
        {
            throw new Exception("Container not initialized");
        }

        var partitionKey = new PartitionKey(id);
        if (!usePartitionKey)
            partitionKey = PartitionKey.None;

        try
        {
            var item = await _container.ReadItemAsync<T>(id, partitionKey);
            return item.Resource;
        }
        catch (Exception ex)
        {
            // Console.WriteLine(ex.Message);
            return default(T);
        }
    }

    public async Task DeleteDocument(string id, bool usePartitionKey = true)
    {
        if (string.IsNullOrEmpty(id))
        {
            Console.WriteLine("Id was null");
        }
        if (_container == null)
        {
            throw new Exception("Container not initialized");
        }

        var partitionKey = new PartitionKey(id);
        if (!usePartitionKey)
            partitionKey = PartitionKey.None;

        try
        {
            var item = await _container.DeleteItemAsync<T>(id, partitionKey);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public IOrderedQueryable<T> GetQueryable()
    {
        return _container.GetItemLinqQueryable<T>();
    }

    public FeedIterator<U> ExecuteSQL<U>(string query)
    {
        using FeedIterator<U> feed = _container.GetItemQueryIterator<U>(
            queryText: query
        );
        return feed;
    }

    public FeedIterator<T> GetEnumerator()
    {
        if (_container == null)
        {
            throw new Exception("Container not initialized");
        }
        return _container.GetItemQueryIterator<T>();
    }

    public void Dispose()
    {
        if (this._client != null)
        {
            this._client.Dispose();
        }
    }
}