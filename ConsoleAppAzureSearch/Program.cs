using System;
using System.Configuration;
using System.Linq;
using System.Threading;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;

namespace ConsoleAppAzureSearch
{
    internal class Program
    {
        private static SearchServiceClient _searchClient;
        private static SearchIndexClient _indexClient;
        private static readonly string indexName = "contents";


        private static void Main(string[] args)
        {
            var searchServiceName = ConfigurationManager.AppSettings["SearchServiceName"];
            var apiKey = ConfigurationManager.AppSettings["SearchServiceApiKey"];

            _searchClient = new SearchServiceClient(searchServiceName, new SearchCredentials(apiKey));
            _indexClient = _searchClient.Indexes.GetClient(indexName);

            Console.WriteLine("{0}", "Deleting index, data source, and indexer...\n");
            if (DeleteIndexIfExists())
            {
                Console.WriteLine("{0}", "Creating index...\n");
                CreateIndex();
                Console.WriteLine("{0}", "Adding documents...\n");
                AddData();
            }

            SearchDocuments("Khan");
            SearchDocuments("JK");
            Console.WriteLine("{0}", "Complete.  Press any key to end application...\n");
            Console.ReadKey();
        }
        private static bool DeleteIndexIfExists()
        {
            if (_searchClient.Indexes.Exists(indexName))
            {
                _searchClient.Indexes.Delete(indexName);
                return true;
            }
            return false;
        }

        private static void CreateIndex()
        {
            // Create the Azure Search index based on the included schema
            try
            {
                var definition = new Index
                {
                    Name = indexName,
                    Fields = new[]
                    {
                        new Field(nameof(SearchUpdateDto.Id), DataType.String)
                        {
                            IsKey = true
                        },
                        new Field(nameof(SearchUpdateDto.Title), DataType.String)
                        {
                            IsSearchable = true,
                            IsSortable = true
                        },
                        new Field(nameof(SearchUpdateDto.Body), DataType.String)
                        {
                            IsSearchable = true,
                            IsFilterable = true
                        }
                    }
                };

                _searchClient.Indexes.Create(definition);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating index: {0}\r\n", ex.Message);
            }
        }

        private static void AddData()
        {
            var documents = new[]
            {
                new SearchUpdateDto
                {
                    Id = "1",
                    Title = "Mohsin JK",
                    Body = "bla bla"
                },
                new SearchUpdateDto
                {
                    Id = "2",
                    Title = "Mohsin Javed Khan",
                    Body = "bla bla"
                },
                new SearchUpdateDto
                {
                    Id = "3",
                    Title = "Mohsin Khan",
                    Body = "bla bla"
                }
            };

            try
            {
                var batch = IndexBatch.Upload(documents);
                _indexClient.Documents.Index(batch);
            }
            catch (IndexBatchException e)
            {
                Console.WriteLine(
                    "Failed to index some of the documents: {0}",
                    string.Join(", ", e.IndexingResults.Where(r => !r.Succeeded).Select(r => r.Key)));
            }

            // Wait a while for indexing to complete.
            Thread.Sleep(2000);
        }

        private static void SearchDocuments(string searchText, string filter = null)
        {
            // Execute search based on search text and optional filter
            var sp = new SearchParameters();

            if (!string.IsNullOrEmpty(filter))
            {
                sp.Filter = filter;
            }

            var documentSearchResult = _indexClient.Documents.Search<SearchUpdateDto>(searchText, sp);
            foreach (var result in documentSearchResult.Results)
            {
                Console.WriteLine(result.Document.Title);
            }
        }




    }
}