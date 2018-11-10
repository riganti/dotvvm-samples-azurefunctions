using System;
using System.Collections.Generic;
using System.Linq;

namespace DotvvmSamples.AzureFunctions.Contracts.Input
{
    public class TestRunAttachmentInputData
    {

        public string FileName { get; set; }

        public string ContentBase64 { get; set; }

    }
}