using ClosedXML.Excel;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Data.Templates;
using Sitecore.Globalization;
using Sitecore.SecurityModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace GE.SC.ContentAuthoring.Utilities
{
    public static class ScHelpers
    {
        public static IEnumerable<Item> GetTemplates()
        {
            Item templateRootItem = GetItem(Settings.GetSetting("templatesPath"));

            if (templateRootItem != null)
            {
                return templateRootItem.Axes.GetDescendants().Where(x => x.TemplateName.Equals("Template"));
            }
            return new List<Item>();
        }

        public static Language DefaultLanguage => LanguageManager.GetLanguages(ScHelpers.Databases.masterDb).Where<Language>((Func<Language, bool>)(x => x.Name.ToLower() == "en")).FirstOrDefault<Language>();

        public static DataTable BuildColumns(IEnumerable<TemplateFieldItem> templateFields)
        {
            var dataTable = new DataTable();
            dataTable.Columns.Add("ID");
            dataTable.Columns.Add("Name");
            dataTable.Columns.Add("Path");
            foreach (TemplateFieldItem item in templateFields)
            {
                dataTable.Columns.Add(item.Name);
            }
            return dataTable;
        }
        public static DataTable BuildColumns(IEnumerable<string> fields)
        {
            var dataTable = new DataTable();
            dataTable.Columns.Add("ID");
            dataTable.Columns.Add("Name");
            dataTable.Columns.Add("Path");
            foreach (var fieldName in fields)
            {
                dataTable.Columns.Add(fieldName);
            }
            return dataTable;
        }

        public static void BuildContents(IEnumerable<Item> items, ref DataTable dataTable, IEnumerable<string> fields)
        {
            foreach (Item item in items)
            {
                DataRow dataRow = dataTable.NewRow();
                dataRow["ID"] = item.ID.ToString();
                dataRow["Name"] = item.Name;
                dataRow["Path"] = item.Paths.Path;
                foreach (string fieldName in fields)
                {
                    dataRow[fieldName] = item?.Fields[fieldName]?.Value;
                }
                dataTable.Rows.Add(dataRow);
            }
        }

        public static void BuildContents(IEnumerable<Item> items, ref DataTable dataTable)
        {
            foreach (Item item in items)
            {
                DataRow dataRow = dataTable.NewRow();
                dataRow["ID"] = item.ID.ToString();
                dataRow["Name"] = item.Name;
                dataRow["Path"] = item.Paths.Path;
                var fields = GetTemplateFields(item);
                foreach (TemplateFieldItem fieldItem in fields)
                {
                    dataRow[fieldItem.Name] = item?.Fields[fieldItem.Name]?.Value;
                }
                dataTable.Rows.Add(dataRow);
            }
        }

        public static string DownloadExcel(DataTable dt, string fileName)
        {
            using (XLWorkbook xLWorkbook = new XLWorkbook())
            {
                xLWorkbook.Worksheets.Add(dt, fileName);
                IXLWorksheet workSheet = xLWorkbook.Worksheet(1);

                workSheet.TabColor = XLColor.Black;
                workSheet.RowHeight = 12.0;
                workSheet.Protection.Protected = true;
                for (int col = 1; col <= 3; ++col)
                    workSheet.Column(col).Style.Protection.Locked = false;
                string excelFileName = fileName + DateTime.Now.ToString("yyyyMMddHHmm") + ".xlsx";
                string path = Path.Combine(Path.GetTempPath(), excelFileName);
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    xLWorkbook.SaveAs(memoryStream);
                    using (FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.Write))
                    {
                        memoryStream.WriteTo(fileStream);
                        fileStream.Close();
                    }
                }
                return excelFileName;
            }
        }

        public static Database GetDatabase(string databaseName) => Factory.GetDatabase(databaseName);

        public static DataTable GetExcelData(Stream inputStream)
        {
            DataTable dataTable = (DataTable)null;
            using (XLWorkbook xlWorkbook = new XLWorkbook(inputStream))
            {
                IXLWorksheet xlWorksheet = xlWorkbook.Worksheet(1);
                dataTable = new DataTable();
                bool flag = true;
                foreach (IXLRow row in (IEnumerable<IXLRow>)xlWorksheet.Rows())
                {
                    if (flag)
                    {
                        foreach (IXLCell cell in (IEnumerable<IXLCell>)row.Cells())
                            dataTable.Columns.Add(cell.Value.ToString());
                        flag = false;
                    }
                    else
                    {
                        dataTable.Rows.Add();
                        int columnIndex = 0;
                        foreach (IXLCell cell in (IEnumerable<IXLCell>)row.Cells(1, dataTable.Columns.Count))
                        {
                            dataTable.Rows[dataTable.Rows.Count - 1][columnIndex] = (object)cell.Value.ToString();
                            ++columnIndex;
                        }
                    }
                }
            }
            return dataTable;
        }

        public static Item GetItem(ID itemID, Language language = null, string databaseName = "")
        {
            Item obj = (Item)null;
            Database database = string.IsNullOrEmpty(databaseName) ? ScHelpers.GetDatabase(Constants.DatabaseNames.Master) : ScHelpers.GetDatabase(databaseName);
            if (database != null)
            {
                if (language == (Language)null)
                    language = ScHelpers.DefaultLanguage;
                if (language != (Language)null)
                {
                    using (new SecurityDisabler())
                    {
                        using (new LanguageSwitcher(language.Name))
                            obj = database.GetItem(itemID, language);
                    }
                }
            }
            return obj;
        }

        public static Item GetItem(string itemPath, Language language = null, string databaseName = "")
        {
            Item obj = (Item)null;
            Database database = string.IsNullOrEmpty(databaseName) ? ScHelpers.GetDatabase(Constants.DatabaseNames.Master) : ScHelpers.GetDatabase(databaseName);
            if (database != null)
            {
                if (language == (Language)null)
                    language = ScHelpers.DefaultLanguage;
                if (language != (Language)null)
                {
                    using (new SecurityDisabler())
                    {
                        using (new LanguageSwitcher(language.Name))
                            obj = database.GetItem(itemPath, language);
                    }
                }
            }
            return obj;
        }

        public static List<Item> GetItemsByTemplate(Item parentItem, ID templateID, bool checkBaseTemplates = false)
        {
            List<Item> objList = new List<Item>();
            if (parentItem != null && ScHelpers.IsValidID(templateID))
            {
                List<ID> idList = new List<ID>();
                TemplateItem templateItem = (TemplateItem)ScHelpers.Databases.masterDb.GetItem(templateID);
                objList = !checkBaseTemplates ? ((IEnumerable<Item>)parentItem.Axes.GetDescendants()).Where<Item>((Func<Item, bool>)(x => x.TemplateID == templateID)).ToList<Item>() : ((IEnumerable<Item>)parentItem.Axes.GetDescendants()).Where<Item>((Func<Item, bool>)(x =>
                {
                    if (x.TemplateID == templateID)
                        return true;
                    return x.Template != null && ((IEnumerable<TemplateItem>)x.Template.BaseTemplates).Any<TemplateItem>((Func<TemplateItem, bool>)(b => b.ID == templateID));
                })).ToList<Item>();
            }
            return objList;
        }

        public static List<TemplateFieldItem> GetTemplateFields(Item contextItem, bool includeSystemTemplateFields = false)
        {
            List<TemplateFieldItem> templateFieldItemList = (List<TemplateFieldItem>)null;
            if (contextItem != null)
            {
                TemplateFieldItem[] fields = contextItem.Template.Fields;
                if (fields != null && fields.Length != 0)
                    templateFieldItemList = !includeSystemTemplateFields ? ((IEnumerable<TemplateFieldItem>)fields).Where<TemplateFieldItem>((Func<TemplateFieldItem, bool>)(x => !x.InnerItem.Paths.FullPath.StartsWith(Constants.Paths.SystemTemplates, StringComparison.OrdinalIgnoreCase))).ToList<TemplateFieldItem>() : ((IEnumerable<TemplateFieldItem>)fields).ToList<TemplateFieldItem>();
            }
            return templateFieldItemList;
        }
        public static IEnumerable<TemplateField> GetTemplateFields(string id, bool includeSystemTemplateFields = false)
        {
            Template template = TemplateManager.GetTemplate(new ID(id), Sitecore.Context.Database);

            return template.GetFields(includeSystemTemplateFields).Where(x => !x.Name.StartsWith("_"));
        }

        public static bool IsValidFile(string fileName, string[] validFileExtensions)
        {
            bool flag = false;
            if (fileName.Length > 0)
            {
                foreach (string validFileExtension in validFileExtensions)
                {
                    if (fileName.EndsWith(validFileExtension))
                    {
                        flag = true;
                        break;
                    }
                }
            }
            return flag;
        }

        public static bool IsValidID(ID id) => !string.IsNullOrEmpty(System.Convert.ToString((object)id)) && !id.IsNull && !id.IsGlobalNullId;

        public static bool IsValidID(string id)
        {
            bool flag = false;
            try
            {
                ID id1 = new ID(id);
                flag = !id1.IsNull && !id1.IsGlobalNullId;
            }
            catch (Exception ex)
            {
            }
            return flag;
        }

        public static bool IsValidItemName(string itemName) => !string.IsNullOrEmpty(itemName) && new Regex("^[a-zA-Z0-9-_\\s]*$").IsMatch(itemName);

        public static bool IsValidName(string name)
        {
            bool flag = true;
            name = name.ToLower();
            if (!string.IsNullOrEmpty(name))
            {
                foreach (char ch in name.ToCharArray())
                {
                    if (!Constants.ValidCharacters.Contains<char>(ch))
                    {
                        flag = false;
                        break;
                    }
                }
            }
            else
                flag = false;
            return flag;
        }

        public static void SitecoreContentImport(DataTable table, string language, string displayName)
        {
            var _sourceLanguage = LanguageManager.GetLanguage(language);
            var _masterDb = ScHelpers.GetDatabase(Constants.DatabaseNames.Master);
            foreach (DataRow row in table.Rows)
            {
                string text = row["ID"].ToString();
                if (string.IsNullOrEmpty(text))
                {
                    return;
                }
                Item item = _masterDb.GetItem(text, _sourceLanguage);
                if (item == null)
                {
                    return;
                }
                for (int i = 3; i < table.Columns.Count; i++)
                {
                    string fieldName = table.Columns[i].ColumnName;
                    Field val = item.Fields.Where(x => x.Name.Equals(fieldName)).FirstOrDefault();
                    if (val != null)
                    {
                        string value = row[val.Name].ToString();
                        using (SecurityDisabler val2 = new SecurityDisabler())
                        {
                            try
                            {
                                item.Editing.BeginEdit();
                                val.Value = value;
                                item.Editing.EndEdit();
                            }
                            finally
                            {
                                val2?.Dispose();
                            }
                        }
                    }
                    if (!string.IsNullOrEmpty(displayName) && displayName.ToLower().Equals("true"))
                    {
                        using (SecurityDisabler val3 = new SecurityDisabler())
                        {
                            try
                            {
                                item.Editing.BeginEdit();
                                item.Appearance.DisplayName = row["Name"].ToString();
                                item.Editing.EndEdit();
                            }
                            finally
                            {
                                val3?.Dispose();
                            }
                        }
                    }
                }
            }
        }

        public struct Databases
        {
            public static Database masterDb = Factory.GetDatabase(Constants.DatabaseNames.Master);
        }
    }
}