using Sitecore.Pipelines;
using System.Web.Mvc;
using System.Web.Routing;

namespace GE.SC.ContentAuthoring.Infrastructure.Pipelines
{
    public class RegisterWebApiRoutes
    {
        public void Process(PipelineArgs args)
        {
            RouteTable.Routes.MapRoute("Export", "api/sc/export",
                new
                {
                    controller = "GEExport",
                    action = "ContentExport"
                }, new[] { "GE.SC.ContentAuthoring" });
            RouteTable.Routes.MapRoute("Download", "api/sc/export/{action}",
               new
               {
                   controller = "GEExport",
               }, new[] { "GE.SC.ContentAuthoring" });
            RouteTable.Routes.MapRoute("TemplateFeilds", "api/sc/{action}",
              new
              {
                  controller = "GeExport",
              }, new[] { "GE.SC.ContentAuthoring" });
            RouteTable.Routes.MapRoute("Import", "api/sc/importcontent",
                new
                {
                    controller = "GEImport",
                    action = "ContentImport"
                }, new[] { "GE.SC.ContentAuthoring" });
        }
    }
}