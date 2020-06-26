using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Ccxc.Core.DbOrm
{
    public abstract class MysqlClient<T> where T : class, new()
    {
        public SqlBaseClient Db => new SqlBaseClient(ConnStr, DbType.MySql, IfInitKeyFromAttribute);

        public IDataCache Cache { get; set; } = null;

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
                var tempName = (attr as DbTableAttribute)?.TableName;
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
                var dbattr = prop.GetCustomAttributes().FirstOrDefault(attr => attr is DbColumnAttribute);
                return dbattr != null && ((DbColumnAttribute) dbattr).IsPrimaryKey;
            }).Select(prop =>
            {
                var name = prop.Name;
                var dbattr = prop.GetCustomAttributes().FirstOrDefault(attr => attr is DbColumnAttribute);
                if (dbattr != null && !string.IsNullOrEmpty((dbattr as DbColumnAttribute)?.ColumnName))
                {
                    name = (dbattr as DbColumnAttribute).ColumnName;
                }
                return $"{name}_{prop.GetValue(obj)}";
            }));
        }

        protected virtual string GetPkeyWhereClause(T obj)
        {
            return string.Join(" AND ", typeof(T).GetProperties().Where(prop =>
            {
                var dbattr = prop.GetCustomAttributes().FirstOrDefault(attr => attr is DbColumnAttribute);
                return dbattr != null && (dbattr as DbColumnAttribute).IsPrimaryKey;
            }).Select(prop =>
            {
                var name = prop.Name;
                var dbattr = prop.GetCustomAttributes().FirstOrDefault(attr => attr is DbColumnAttribute);
                if (dbattr != null && !string.IsNullOrEmpty((dbattr as DbColumnAttribute)?.ColumnName))
                {
                    name = (dbattr as DbColumnAttribute).ColumnName;
                }
                return $"`{name}` = '{prop.GetValue(obj)}'";
            }));
        }

        public virtual async Task InvalidateCache()
        {
            if (Cache != null)
            {
                var tableName = GetTableName();
                var key = $"/ccxc-backend/datacache/{tableName}";
                await Cache.Delete(key);
            }
        }

        public virtual async Task<List<T>> SelectAllFromCache(bool writeCache = true)
        {
            string key = null;
            if (Cache != null)
            {
                var tableName = GetTableName();
                key = $"/ccxc-backend/datacache/{tableName}";

                var res = await Cache.GetAll<T>(key);
                if (res != null && res.Count > 0) return res;
            }

            var result = SimpleDb.GetList();
            if (writeCache)
            {
                var hashvalues = new Dictionary<string, object>();

                foreach (var i in result)
                {
                    var pk = GetPkey(i);
                    hashvalues.Add(pk, i);
                }

                if (Cache != null) await Cache.PutAll(key, hashvalues, 86400000);
            }

            return result;
        }
    }
}
