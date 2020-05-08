using System;

namespace Ccxc.Core.DbOrm
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class DbTableAttribute : SqlSugar.SugarTable
    {
        public DbTableAttribute(string tableName) : base(tableName)
        {
        }

        public DbTableAttribute(string tableName, string tableDescription) : base(tableName, tableDescription)
        {
        }

        public string RecordTableName { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class DbColumnAttribute : SqlSugar.SugarColumn
    {

    }
}
