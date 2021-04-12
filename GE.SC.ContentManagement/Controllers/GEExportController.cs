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
using System.Web.Mvc;

namespace GE.SC.ContentAuthoring.Controllers
{
    public class GEExportController : Controller
    {
        private Database _masterDb { get; set; }
        public GEExportController()
        {
            _masterDb = ScHelpers.GetDatabase(Constants.DatabaseNames.Master);
        }

        [HttpGet]
        public ActionResult ContentExport(string id)
        {
            var model = new ExportModel();
            try
            {
                if (!string.IsNullOrEmpty(id))
                {
                    model.ID = id;
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
                    List<SelectListItem> templates = new List<SelectListItem>();
                    foreach (var item in ScHelpers.GetTemplates())
                    {
                        SelectListItem val = new SelectListItem
                        {
                            Text = item.DisplayName.ToString(),
                            Value = item.ID.ToString()
                        };
                        templates.Add(val);
                    }
                    if (templates?.Any() ?? false)
                    {
                        model.Templates = templates;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Export", ex, (object)model);
            }
            return View("~/Views/ContentAuthoring/ContentExport.cshtml", model);
        }
        [HttpPost]
        public JsonResult GetTemplateFields(string id)
        {
            var fields = ScHelpers.GetTemplateFields(id, true).Select(x => new { Name = x.Name });
            return Json(fields, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult ContentExport(ExportModel model)
        {
            string fileName = string.Empty;
            try
            {
                var _sourceLanguage = LanguageManager.GetLanguage(model.SelectedLanguage);
                Item rootItem = ScHelpers.GetItem(model.RootItem, _sourceLanguage, Constants.DatabaseNames.Master);
                if (rootItem != null && rootItem.Versions.Count > 0)
                {
                    var items = new List<Item>();
                    if (rootItem.HasChildren && model.IncludeChildren)
                        items = ScHelpers.GetItemsByTemplate(rootItem, new ID(model.SelectedTemplate));
                    else
                        items.Add(rootItem);

                    if (items != null && items.Any())
                    {
                        var firstItem = items.FirstOrDefault();
                        var dataTable = new DataTable();
                        if (model.SelectedFields.Any())
                        {
                            dataTable = ScHelpers.BuildColumns(model.SelectedFields);
                            ScHelpers.BuildContents(items, ref dataTable, model.SelectedFields);
                        }
                        else
                        {
                            var fields = ScHelpers.GetTemplateFields(firstItem)?.Where(x => !x.Name.StartsWith("_"));
                            dataTable = ScHelpers.BuildColumns(fields);
                            ScHelpers.BuildContents(items, ref dataTable);
                        }

                        fileName = ScHelpers.DownloadExcel(dataTable, firstItem.TemplateName.Replace(' ', '-'));
                    }
                }
                return Json(new { fileName = fileName, message = "Export Completed Successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { fileName = fileName, message = ex.Message });
            }
        }

        [HttpGet]
        [Attributes.DeleteFile]
        public ActionResult Download(string fileName)
        {
            try
            {
                string text = Path.Combine(Path.GetTempPath(), fileName);
                return File(text, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                Log.Error("Download Failed", ex, (object)this);
                return View("~/Views/ContentAuthoring/ContentExport.cshtml", new ExportModel());
            }
        }
    }
}