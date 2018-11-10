using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotVVM.Framework.ViewModel;

namespace DotvvmSamples.AzureFunctions.Web.ViewModels
{
    public class ResultsViewModel : MasterPageViewModel
    {
        [FromRoute(nameof(ProjectName))]
        public string ProjectName { get; set; }

        [FromRoute(nameof(TestSuiteName))]
        public string TestSuiteName { get; set; }

        [FromRoute(nameof(BuildNumber))]
        public string BuildNumber { get; set; }
    }
}

