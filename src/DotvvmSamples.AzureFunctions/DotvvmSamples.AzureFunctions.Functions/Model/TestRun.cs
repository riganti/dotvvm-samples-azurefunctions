using System;
using System.Collections.Generic;
using System.Text;
using DotvvmSamples.AzureFunctions.Contracts;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace DotvvmSamples.AzureFunctions.Functions.Model
{
    public class TestRun : TableEntity
    {
        public int TestResult { get; set; }
        
        public string TestOutput { get; set; }

        public string TestOutputUrl { get; set; }


        [IgnoreProperty]
        public string TestSuiteName => PartitionKey.Split('!')[0];

        [IgnoreProperty]
        public string BuildNumber => PartitionKey.Split('!')[1];

        [IgnoreProperty]
        public string TestFullName => RowKey;

        [IgnoreProperty]
        public List<TestRunAttachmentLink> Attachments { get; set; }

        public override void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            base.ReadEntity(properties, operationContext);
            Attachments = JsonConvert.DeserializeObject<List<TestRunAttachmentLink>>(properties["attachments"].StringValue);
        }

        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            var properties = base.WriteEntity(operationContext);
            properties["attachments"] = new EntityProperty(JsonConvert.SerializeObject(Attachments));
            return properties;
        }

        public static string CreatePartitionKey(string testSuiteName, string buildNumber)
        {
            return $"{testSuiteName}!{buildNumber}";
        }

        public static string CreateRowKey(string testFullName)
        {
            return testFullName;
        }
    }
}
