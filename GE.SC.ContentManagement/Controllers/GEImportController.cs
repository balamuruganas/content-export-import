using GE.SC.ContentAuthoring.Models;
using GE.SC.ContentAuthoring.Utilities;
using Sitecore.Collections;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace GE.SC.ContentAuthoring.Controllers
{
    public class GEImportController : Controller
    {
        private Database _masterDb { get; set; }
        public GEImportController()
        {
            _masterDb = ScHelpers.GetDatabase(Constants.DatabaseNames.Master);
        }

        public ActionResult ContentImport(string id)
        {
            var model = new ImportModel();
            try
            {
                LanguageCollection languages = LanguageManager.GetLanguages(_masterDb);
                List<SelectListItem> list = new List<SelectListItem>();
                foreach (Language item in (Collection<Language>)(object)languages)
                {
                    SelectListItem val = new SelectListItem
                    {
                        Text = item.CultureInfo.DisplayName,
                        Value = item.CultureInfo.Name
                    };
                    list.Add(val);
                }
                if (list?.Any() ?? false)
                {
                    model.AvailableLanguages = list;
                }
            }
            catch (Exception ex)
            {
                Log.Error("ContentImport", ex, this);
            }

            return View("~/Views/ContentAuthoring/ContentImport.cshtml", model);
        }

        [HttpPost]
        public ActionResult ContentImport(string language, string displayName)
        {
            try
            {
                HttpPostedFileBase httpPostedFileBase = Request.Files[0];
                DataTable dtExcel = ScHelpers.GetExcelData(httpPostedFileBase.InputStream);
                if (httpPostedFileBase != null && Path.GetExtension(httpPostedFileBase.FileName).Equals(".xlsx"))
                {
                    ScHelpers.SitecoreContentImport(dtExcel, language, displayName);
                }
            }
            catch (Exception ex)
            {
                Log.Error("ContentImport", ex, this);
                return Json(new
                {
                    message = ex.Message
                });
            }
            return Json((object)new
            {
                message = "Content Imported Successfully"
            });
        }
    }
}