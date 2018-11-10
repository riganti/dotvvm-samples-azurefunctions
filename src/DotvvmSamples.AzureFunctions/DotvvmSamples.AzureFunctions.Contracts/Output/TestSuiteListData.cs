using System;
using System.Collections.Generic;
using System.Text;

namespace DotvvmSamples.AzureFunctions.Contracts.Output
{
    public class TestSuiteListData
    {

        public string TestSuiteName { get; set; }

        public string BuildNumber { get; set; }

        public DateTime CreatedDate { get; set; }

    }
}
