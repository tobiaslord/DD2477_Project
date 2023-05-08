# Book Search Engine
A Seach engine that uses your feedback to find better books for you to read.

# Dependencies

* .NET 6.0
* ElasticSearch 8.7.1

# Installation

The program uses ElasticSearch for its basic indexing, therefore you will need an empty instance of Elastic Search to install the system.  

To install the program, start by cloning the repository

```
git clone 

```
Then go into the .env file to enter the information about your ElasticSearch instance (username, password, fingerprint and ipaddress)

```
cd DD2477_Project
.env
```

Now, you can run the backend project to index all the books, this step can take up to 15 minutes.

```
dotnet run --project ./Backend/Backend.csproj
```

Once the indexing is done, you can start the program

```
dotnet run --project ./"Graphical Interface"/"Graphical Interface".csproj
```
