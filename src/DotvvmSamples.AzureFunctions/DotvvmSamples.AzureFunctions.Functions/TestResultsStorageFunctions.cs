using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using DotvvmSamples.AzureFunctions.Contracts.Input;
using DotvvmSamples.AzureFunctions.Contracts.Output;
using DotvvmSamples.AzureFunctions.Functions.Model;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;

namespace DotvvmSamples.AzureFunctions.Functions
{
    public static class TestResultsStorageFunctions
    {
        private const int MaxFieldLength = 65000;
        public const string IndexTableName = "index";
        public const string AzureWebJobsContainerPrefixes = "AzureWebJobsHost";

        private static readonly CloudBlobClient blobClient;
        private static readonly CloudTableClient tableClient;
        private static readonly IConfigurationRoot config;

        static TestResultsStorageFunctions()
        {
            config = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("local.settings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var storageConnectionString = config["TestResultsStorageConnectionString"];
            var account = CloudStorageAccount.Parse(storageConnectionString);
            blobClient = account.CreateCloudBlobClient();
            tableClient = account.CreateCloudTableClient();
        }

        private static async Task<CloudBlobContainer> GetBlobContainerForProject(string projectName, bool createIfNotExists = false)
        {
            var container = blobClient.GetContainerReference(projectName);
            if (createIfNotExists)
            {
                await container.CreateIfNotExistsAsync();
            }
            return container;
        }

        private static async Task<CloudTable> GetIndexTable(bool createIfNotExists = false)
        {
            var table = tableClient.GetTableReference(IndexTableName);
            if (createIfNotExists)
            {
                await table.CreateIfNotExistsAsync();
            }

            return table;
        }

        private static async Task<CloudTable> GetTableForProject(string projectName, bool createIfNotExists = false)
        {
            var table = tableClient.GetTableReference(projectName);
            if (createIfNotExists)
            {
                await table.CreateIfNotExistsAsync();
            }
            return table;
        }

        private static string GetTestRunFolderUrl(string testSuiteName, string buildNumber, string testFullName)
        {
            return $"{testSuiteName}/{buildNumber}/{testFullName}";
        }

        private static string GetTestOutputBlobUrl(string testSuiteName, string buildNumber, string testFullName)
        {
            var testRunFolder = GetTestRunFolderUrl(testSuiteName, buildNumber, testFullName);
            return $"{testRunFolder}/__output.txt";
        }

        private static string GetTestAttachmentBlobUrl(string testSuiteName, string buildNumber, string testFullName, string attachmentFileName)
        {
            var testRunFolder = GetTestRunFolderUrl(testSuiteName, buildNumber, testFullName);
            return $"{testRunFolder}/{Helpers.RemoveUnsafeChars(attachmentFileName, allowUrlChars: true)}";
        }

        private static string AppBaseUrl => config["AppBaseUrl"].TrimEnd('/');

        private static string BlobBaseUrl => config["BlobBaseUrl"].TrimEnd('/');


        [FunctionName("PublishResult")]
        public static async Task<IActionResult> PublishResult([HttpTrigger(AuthorizationLevel.Function, "post", Route = "publish")]TestRunInputData input, TraceWriter log)
        {
            try
            {
                // remove special characters
                input.ProjectName = Helpers.RemoveUnsafeChars(input.ProjectName);
                input.TestSuiteName = Helpers.RemoveUnsafeChars(input.TestSuiteName);
                input.BuildNumber = Helpers.RemoveUnsafeChars(input.BuildNumber);
                input.TestFullName = Helpers.RemoveUnsafeChars(input.TestFullName, allowUrlChars: true);

                // save test run results
                var entity = new TestRun()
                {
                    PartitionKey = TestRun.CreatePartitionKey(input.TestSuiteName, input.BuildNumber),
                    RowKey = TestRun.CreateRowKey(input.TestFullName),
                    Attachments = new List<TestRunAttachmentLink>(),
                    Timestamp = DateTimeOffset.UtcNow,
                    TestResult = input.TestResult
                };

                // store test output - if it is short, put it directly in the table; if it is too long, put it in blob storage
                if (input.TestOutput.Length <= MaxFieldLength)
                {
                    entity.TestOutput = input.TestOutput;
                }
                else
                {
                    // prepare URL
                    var testOutputUrl = GetTestOutputBlobUrl(input.TestSuiteName, input.BuildNumber, input.TestFullName);

                    // upload test output
                    var container = await GetBlobContainerForProject(input.ProjectName);
                    var testOutputBlob = container.GetBlockBlobReference(testOutputUrl);
                    await testOutputBlob.UploadTextAsync(input.TestOutput);

                    entity.TestOutput = input.TestOutput.Substring(0, MaxFieldLength);
                    entity.TestOutputUrl = testOutputUrl;
                }

                // save attachments to blob storage
                if (input.Attachments != null)
                {
                    foreach (var attachment in input.Attachments)
                    {
                        // prepare URL
                        var attachmentUrl = GetTestAttachmentBlobUrl(input.TestSuiteName, input.BuildNumber, input.TestFullName, attachment.FileName);

                        // upload blob
                        var container = await GetBlobContainerForProject(input.ProjectName);
                        var attachmentBlob = container.GetBlockBlobReference(attachmentUrl);
                        var attachmentData = Convert.FromBase64String(attachment.ContentBase64);
                        await attachmentBlob.UploadFromByteArrayAsync(attachmentData, 0, attachmentData.Length);

                        entity.Attachments.Add(new TestRunAttachmentLink()
                        {
                            FileName = attachment.FileName,
                            BlobUrl = attachmentUrl
                        });
                    }
                }

                // store entity
                var table = await GetTableForProject(input.ProjectName, createIfNotExists: true);
                await table.ExecuteAsync(TableOperation.Insert(entity));

                // store index entity
                var indexTable = await GetIndexTable(createIfNotExists: true);
                await indexTable.ExecuteAsync(TableOperation.InsertOrReplace(new TestSuiteInfo()
                {
                    PartitionKey = TestSuiteInfo.CreatePartitionKey(input.ProjectName),
                    RowKey = TestSuiteInfo.CreateRowKey(input.TestSuiteName, input.BuildNumber),
                    Timestamp = DateTimeOffset.UtcNow
                }));

                return new OkObjectResult(new TestRunInputResult()
                {
                    TestSuiteUrl = $"{AppBaseUrl}/results/{input.ProjectName}/{input.TestSuiteName}/{input.BuildNumber}",
                    TestResultUrl = $"{AppBaseUrl}/results/{input.ProjectName}/{input.TestSuiteName}/{input.BuildNumber}#{input.TestFullName}"
                });
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
                return new BadRequestErrorMessageResult(ex.Message);
            }
        }

        [FunctionName("GetProjects")]
        public static async Task<List<string>> GetProjects([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects")]HttpRequest req, TraceWriter log)
        {
            var projects = new List<string>();

            TableContinuationToken continuationToken = null;
            do
            {
                var page = await tableClient.ListTablesSegmentedAsync(continuationToken);
                continuationToken = page.ContinuationToken;

                projects.AddRange(page.Results
                    .Select(r => r.Name)
                    .Where(r => r != IndexTableName && !r.StartsWith(AzureWebJobsContainerPrefixes))
                );
            }
            while (continuationToken != null);

            return projects;
        }

        [FunctionName("GetProjectTestSuites")]
        public static async Task<List<TestSuiteListData>> GetProjectTestSuites([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "testSuites/{projectName}")]HttpRequest req, string projectName, TraceWriter log)
        {
            var table = await GetIndexTable();
            var query = new TableQuery<TestSuiteInfo>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, projectName));

            var testSuites = new List<TestSuiteListData>();
            TableContinuationToken continuationToken = null;
            do
            {
                var page = await table.ExecuteQuerySegmentedAsync(query, continuationToken);
                continuationToken = page.ContinuationToken;

                testSuites.AddRange(page.Results
                    .Select(s => new TestSuiteListData()
                    {
                        TestSuiteName = s.TestSuiteName,
                        BuildNumber = s.BuildNumber,
                        CreatedDate = s.Timestamp.UtcDateTime
                    }));
            }
            while (continuationToken != null);

            return testSuites.OrderByDescending(s => s.CreatedDate).ToList();
        }

        [FunctionName("GetResults")]
        public static async Task<TestSuiteOutputData> GetResults([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "results/{projectName}/{testSuiteName}/{buildNumber}")]HttpRequest req, string projectName, string testSuiteName, string buildNumber, TraceWriter log)
        {
            var table = await GetTableForProject(projectName);
            var query = new TableQuery<TestRun>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, TestRun.CreatePartitionKey(testSuiteName, buildNumber)));

            // prepare the result
            var result = new TestSuiteOutputData()
            {
                ProjectName = projectName,
                TestSuiteName = testSuiteName,
                BuildNumber = buildNumber
            };

            // get all test results
            var testRuns = new List<TestRunOutputData>();
            TableContinuationToken continuationToken = null;
            do
            {
                var queryResults = await table.ExecuteQuerySegmentedAsync(query, continuationToken);
                continuationToken = queryResults.ContinuationToken;

                var data = queryResults.Results
                    .Select(r => new TestRunOutputData()
                    {
                        CreatedDate = r.Timestamp.UtcDateTime,
                        TestResult = r.TestResult,
                        TestFullName = r.TestFullName,
                        TestOutput = r.TestOutput,
                        IsFullTestOutput = r.TestOutputUrl != null,
                        Attachments = r.Attachments
                            .Select(a => new TestRunAttachmentOutputData()
                            {
                                FileName = a.FileName,
                                Url = $"{BlobBaseUrl}/{projectName}/{a.BlobUrl}"
                            })
                            .ToList()
                    });
                testRuns.AddRange(data);
            } while (continuationToken != null);

            // sort by creation date
            result.TestRuns = testRuns.OrderBy(r => r.CreatedDate).ToList();

            return result;
        }

        [FunctionName("GetTestOutput")]
        public static async Task<string> GetTestOutput([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "testOutput/{projectName}/{testSuiteName}/{buildNumber}/{testFullName}")]HttpRequest req, string projectName, string testSuiteName, string buildNumber, string testFullName, TraceWriter log)
        {
            var container = await GetBlobContainerForProject(projectName);

            // get full test output
            var testOutputUrl = GetTestOutputBlobUrl(testSuiteName, buildNumber, testFullName);
            var testOutputBlob = container.GetBlockBlobReference(testOutputUrl);

            return await testOutputBlob.DownloadTextAsync();
        }
    }
}
