using SqlSugar;
using System;
using System.Linq;
using System.Reflection;

namespace Ccxc.Core.DbOrm
{
    public abstract class MysqlClient<T> where T : class, new()
    {
        public SqlBaseClient Db
        {
            get
            {
                return new SqlBaseClient(ConnStr, DbType.MySql, IfInitKeyFromAttribute);
            }
        }

        protected string ConnStr;
        protected bool IfInitKeyFromAttribute;

        public MysqlClient(string connStr)
        {
            ConnStr = connStr;
            IfInitKeyFromAttribute = true;
        }

        public MysqlClient(string connStr, bool initKeyFromAttribute)
        {
            ConnStr = connStr;
            IfInitKeyFromAttribute = initKeyFromAttribute;
        }

        public SimpleClient<T> SimpleDb => new SimpleClient<T>(Db);
        public virtual void InitTable() => Db.CodeFirst.InitTables<T>();

        public virtual string GetTableName()
        {
            var tableName = typeof(T).Name;
            var dbTableAttrs = typeof(T).GetCustomAttributes().Where(attr => attr is DbTableAttribute);
            foreach (var attr in dbTableAttrs)
            {
                var tempName = (attr as DbTableAttribute).TableName;
                if (!string.IsNullOrEmpty(tempName))
                {
                    tableName = tempName;
                }
                break;
            }

            return tableName;
        }

        public virtual string GetPkey(T obj)
        {
            return string.Join("_", typeof(T).GetProperties().Where(prop =>
            {
                var dbattr = prop.GetCustomAttributes().Where(attr => attr is DbColumnAttribute).FirstOrDefault();
                if (dbattr != null)
                {
                    return (dbattr as DbColumnAttribute).IsPrimaryKey;
                }
                else
                {
                    return false;
                }
            }).Select(prop =>
            {
                var name = prop.Name;
                var dbattr = prop.GetCustomAttributes().Where(attr => attr is DbColumnAttribute).FirstOrDefault();
                if (dbattr != null && !string.IsNullOrEmpty((dbattr as DbColumnAttribute).ColumnName))
                {
                    name = (dbattr as DbColumnAttribute).ColumnName;
                }
                return $"{name}_{prop.GetValue(obj).ToString()}";
            }));
        }

        protected virtual string GetPkeyWhereClause(T obj)
        {
            return string.Join(" AND ", typeof(T).GetProperties().Where(prop =>
            {
                var dbattr = prop.GetCustomAttributes().Where(attr => attr is DbColumnAttribute).FirstOrDefault();
                if (dbattr != null)
                {
                    return (dbattr as DbColumnAttribute).IsPrimaryKey;
                }
                else
                {
                    return false;
                }
            }).Select(prop =>
            {
                var name = prop.Name;
                var dbattr = prop.GetCustomAttributes().Where(attr => attr is DbColumnAttribute).FirstOrDefault();
                if (dbattr != null && !string.IsNullOrEmpty((dbattr as DbColumnAttribute).ColumnName))
                {
                    name = (dbattr as DbColumnAttribute).ColumnName;
                }
                return $"`{name}` = '{prop.GetValue(obj).ToString()}'";
            }));
        }
    }
}
