	using DotVVM.Framework.Configuration;
using DotVVM.Framework.ResourceManagement;
using DotVVM.Framework.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DotvvmSamples.AzureFunctions.Web
{
    public class DotvvmStartup : IDotvvmStartup, IDotvvmServiceConfigurator
    {
        // For more information about this class, visit https://dotvvm.com/docs/tutorials/basics-project-structure
        public void Configure(DotvvmConfiguration config, string applicationPath)
        {
            ConfigureRoutes(config, applicationPath);
            ConfigureControls(config, applicationPath);
            ConfigureResources(config, applicationPath);

            var azureFunctionsUrl = Startup.Configuration["AzureFunctionsUrl"].TrimEnd('/');
            config.RegisterApiClient(typeof(Api.Client), azureFunctionsUrl + "/api", "wwwroot/Scripts/ApiClient.js", "_functions");
        }

        private void ConfigureRoutes(DotvvmConfiguration config, string applicationPath)
        {
            config.RouteTable.Add("Default", "", "Views/Default.dothtml");
            config.RouteTable.Add("Project", "project/{ProjectName}", "Views/Project.dothtml");
            config.RouteTable.Add("Results", "results/{ProjectName}/{TestSuiteName}/{BuildNumber}", "Views/Results.dothtml");
        }

        private void ConfigureControls(DotvvmConfiguration config, string applicationPath)
        {
            // register code-only controls and markup controls
        }

        private void ConfigureResources(DotvvmConfiguration config, string applicationPath)
        {
            // register custom resources and adjust paths to the built-in resources
            config.Resources.Register("jquery", new ScriptResource()
            {
                Location = new UrlResourceLocation("~/lib/jquery/jquery.min.js")
            });
            config.Resources.Register("bootstrap-css", new StylesheetResource()
            {
                Location = new UrlResourceLocation("~/lib/twitter-bootstrap/css/bootstrap.min.css")
            });
            config.Resources.Register("bootstrap", new ScriptResource()
            {
                Location = new UrlResourceLocation("~/lib/twitter-bootstrap/js/bootstrap.min.js"),
                Dependencies = new [] { "jquery", "bootstrap-css" }
            });
        }
		public void ConfigureServices(IDotvvmServiceCollection options)
        {
            options.AddDefaultTempStorages("temp");
		}
    }
}
