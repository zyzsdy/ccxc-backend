using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ccxc.Core.DbOrm
{
    public interface IDataCache
    {
        Task Put(string key, object value, long timeoutMilliseconds = -1);

        Task PutString(string key, string value, long timeoutMilliseconds = -1);

        Task<T> Get<T>(string key);

        Task<string> GetString(string key);

        Task PutAll(string key, IDictionary<string, object> values, long timeoutMilliseconds = -1);

        Task<T> GetFromPk<T>(string key, string pk);

        Task<List<(string key, T value)>> GetHashKeys<T>(string key);

        Task<List<T>> GetAll<T>(string key);

        Task Delete(string key);

        Task<long> GetHashLength(string key);

        Task Delete(string key, string hkey);
    }
}
