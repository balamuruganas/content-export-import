using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Reflection;

namespace GE.SC.ContentAuthoring.Utilities
{
    public static class DataTableConversionExtensions
    {
        public static void SetColumnsOrder(this DataTable table, params string[] columnNames)
        {
            int ordinal = 0;
            foreach (string columnName in columnNames)
            {
                table.Columns[columnName].SetOrdinal(ordinal);
                ++ordinal;
            }
        }

        public static DataTable ToDataTable<T>(this IList<T> data) where T : class, new()
        {
            DataTable dataTable = new DataTable();
            if (typeof(T).IsValueType || typeof(T).Equals(typeof(string)))
            {
                DataColumn column = new DataColumn("Value");
                dataTable.Columns.Add(column);
                foreach (T obj in (IEnumerable<T>)data)
                {
                    DataRow row = dataTable.NewRow();
                    row[0] = (object)obj;
                    dataTable.Rows.Add(row);
                }
            }
            else
            {
                PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(T));
                foreach (PropertyDescriptor propertyDescriptor in properties)
                {
                    DataColumnCollection columns = dataTable.Columns;
                    string name = propertyDescriptor.Name;
                    Type type = Nullable.GetUnderlyingType(propertyDescriptor.PropertyType);
                    if ((object)type == null)
                        type = propertyDescriptor.PropertyType;
                    columns.Add(name, type);
                }
                foreach (T obj in (IEnumerable<T>)data)
                {
                    DataRow row = dataTable.NewRow();
                    foreach (PropertyDescriptor propertyDescriptor in properties)
                    {
                        try
                        {
                            row[propertyDescriptor.Name] = propertyDescriptor.GetValue((object)obj) ?? (object)DBNull.Value;
                        }
                        catch (Exception ex)
                        {
                            row[propertyDescriptor.Name] = (object)DBNull.Value;
                        }
                    }
                    dataTable.Rows.Add(row);
                }
            }
            return dataTable;
        }

        public static List<T> ToList<T>(this DataTable table) where T : class, new()
        {
            try
            {
                List<T> objList = new List<T>();
                foreach (DataRow dataRow in table.AsEnumerable())
                {
                    T obj = new T();
                    foreach (PropertyInfo property1 in obj.GetType().GetProperties())
                    {
                        try
                        {
                            if (table.Columns.Contains(property1.Name))
                            {
                                PropertyInfo property2 = obj.GetType().GetProperty(property1.Name);
                                property2.SetValue((object)obj, Convert.ChangeType(dataRow[property1.Name], property2.PropertyType), (object[])null);
                            }
                        }
                        catch
                        {
                        }
                    }
                    objList.Add(obj);
                }
                return objList;
            }
            catch
            {
                return (List<T>)null;
            }
        }

        public static T ToObject<T>(this DataRow dataRow) where T : new()
        {
            T obj1 = new T();
            foreach (DataColumn column in (InternalDataCollectionBase)dataRow.Table.Columns)
            {
                PropertyInfo property = obj1.GetType().GetProperty(column.ColumnName);
                if (property != (PropertyInfo)null && dataRow[column] != DBNull.Value)
                {
                    object obj2 = Convert.ChangeType(dataRow[column], property.PropertyType);
                    property.SetValue((object)obj1, obj2, (object[])null);
                }
            }
            return obj1;
        }
    }
}