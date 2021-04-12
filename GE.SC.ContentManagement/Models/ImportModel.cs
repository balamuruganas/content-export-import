using System.Collections.Generic;
using System.Web.Mvc;

namespace GE.SC.ContentAuthoring.Models
{
    public class ImportModel
    {
        public List<SelectListItem> AvailableLanguages
        {
            get;
            set;
        }

        public bool IsDisplayName
        {
            get;
            set;
        }

        public string SelectedLanguage
        {
            get;
            set;
        }
    }
}