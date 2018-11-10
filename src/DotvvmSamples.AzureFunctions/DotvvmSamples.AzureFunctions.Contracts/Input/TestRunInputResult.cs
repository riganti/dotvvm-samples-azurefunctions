using System;
using System.Collections.Generic;
using System.Linq;

namespace DotvvmSamples.AzureFunctions.Contracts.Input
{
    public class TestRunInputResult
    {
        public string TestSuiteUrl { get; set; }

        public string TestResultUrl { get; set; }
    }
}