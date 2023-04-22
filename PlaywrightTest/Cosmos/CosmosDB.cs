using Microsoft.Azure.Cosmos;
using System.Configuration;

namespace Cosmos;
public class CosmosDB<T> : IDisposable {
    private string connectionString;
    const string db = "books-database";
    private string collection;
    private CosmosClient? _client;
    private Container? _container;

    public CosmosDB(string collection) {
        this.collection = collection;
        this.connectionString = ConfigurationManager.AppSettings.Get("ConnectionString") ?? "";
        this.InitializeCosmosClient();
    }
    private void InitializeCosmosClient() {
        var client = new CosmosClient(connectionString);
        var database = client.GetDatabase(db);
        this._container = database.GetContainer(collection);

        if (this._container == null) {
            throw new Exception("Could not connect to CosmosDB");
        }
    }
    public async Task PostDocument(T doc) {
        if (doc == null) {
            Console.WriteLine("Doc was null");
        }
        if (_container == null) {
            throw new Exception("Container not initialized");
        }
        try {
            var result = await _container.UpsertItemAsync<T>(doc);
        }
        catch (Exception ex) {
            Console.WriteLine("Could not save document", doc.ToString(), ex.ToString());
        }
    }

    public void Dispose()
    {
        if (this._client != null) {
            this._client.Dispose();
        }
    }
}