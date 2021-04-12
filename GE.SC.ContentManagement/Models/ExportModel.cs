using System.Collections.Generic;
using System.Web.Mvc;

namespace GE.SC.ContentAuthoring.Models
{
    public class ExportModel
    {
        public string ID { get; set; }
        public List<SelectListItem> AvailableLanguages { get; set; }
        public List<SelectListItem> Templates { get; set; }
        public string SelectedLanguage { get; set; }
        public string SelectedTemplate { get; set; }
        public string RootItem { get; set; }
        public bool IncludeChildren { get; set; }

        public IEnumerable<string> SelectedFields { get; set; }
    }
}