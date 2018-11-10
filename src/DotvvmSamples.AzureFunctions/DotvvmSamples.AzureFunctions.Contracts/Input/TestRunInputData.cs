using System;
using System.Collections.Generic;
using System.Linq;

namespace DotvvmSamples.AzureFunctions.Contracts.Input
{
    public class TestRunInputData
    {

        public string ProjectName { get; set; }

        public string TestSuiteName { get; set; }

        public string BuildNumber { get; set; }

        public string TestFullName { get; set; }

        public int TestResult { get; set; }     // enum serialization doesn't work in latest SDK - 0 = Failed, 1 = Success

        public string TestOutput { get; set; }

        public List<TestRunAttachmentInputData> Attachments { get; set; }

    }
}
