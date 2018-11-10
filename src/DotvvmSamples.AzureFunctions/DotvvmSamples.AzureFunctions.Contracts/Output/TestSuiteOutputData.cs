using System;
using System.Collections.Generic;
using System.Text;

namespace DotvvmSamples.AzureFunctions.Contracts.Output
{

    public class TestSuiteOutputData
    {
        public string ProjectName { get; set; }

        public string TestSuiteName { get; set; }

        public string BuildNumber { get; set; }

        public List<TestRunOutputData> TestRuns { get; set; }
    }
}
