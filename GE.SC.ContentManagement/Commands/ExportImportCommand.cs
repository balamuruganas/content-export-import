using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;
using System;
using System.Web;

namespace GE.SC.ContentAuthoring.Commands
{
    public class ExportImportCommand : Command
    {
        public override void Execute(CommandContext context)
        {
            if (context.Parameters.Count > 0)
            {
                string apiUrl = context.Parameters.Get("url");
                string targetId = context.Parameters.Get("id");
                Assert.IsNotNull((object)apiUrl, "Service Url parameter cannot be empty");
                Assert.IsNotNull((object)targetId, "Target Id parameter cannot be empty");
                context.Parameters.Add("apiUrl", $"{apiUrl}?id={targetId}");
                Context.ClientPage.Start((object)this, "Run", context.Parameters);
            }
        }
        protected void Run(ClientPipelineArgs args)
        {
            string text = args.Parameters["apiUrl"];
            Assert.IsNotNull((object)text, "apiUrl parameter cannot be empty");
            UrlString val = new UrlString(HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority) + text);
            Assert.IsNotNull((object)val, "Url string cannot be null");
            var options = new ModalDialogOptions(val.ToString())
            {
                Header = "Content Management " + args.Parameters["title"],
                Height = "450px",
                Width = "800px"
            };
            Context.ClientPage.ClientResponse.ShowModalDialog(options);
        }

    }
}