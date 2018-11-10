using System;
using System.Collections.Generic;
using System.Linq;

namespace DotvvmSamples.AzureFunctions.Contracts.Output
{
    public class TestRunOutputData
    {
        public DateTime CreatedDate { get; set; }

        public string TestFullName { get; set; }

        public int TestResult { get; set; }     

        public string TestOutput { get; set; }

        public bool IsFullTestOutput { get; set; }

        public List<TestRunAttachmentOutputData> Attachments { get; set; }
    }
}