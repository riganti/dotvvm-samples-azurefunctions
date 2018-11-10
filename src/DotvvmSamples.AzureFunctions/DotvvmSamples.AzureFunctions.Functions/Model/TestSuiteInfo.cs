using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.WindowsAzure.Storage.Table;

namespace DotvvmSamples.AzureFunctions.Functions.Model
{
    public class TestSuiteInfo : TableEntity
    {

        public string ProjectName => PartitionKey;

        public string TestSuiteName => RowKey.Split('!')[0];

        public string BuildNumber => RowKey.Split('!')[1];


        public static string CreatePartitionKey(string projectName)
        {
            return projectName;
        }

        public static string CreateRowKey(string testSuiteName, string buildNumber)
        {
            return $"{testSuiteName}!{buildNumber}";
        }

    }
}
