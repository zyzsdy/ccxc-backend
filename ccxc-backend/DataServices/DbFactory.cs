using Ccxc.Core.DbOrm;
using System;

namespace ccxc_backend.DataServices
{
    public static class DbFactory
    {
        public static T Get<T>()
        {
            //数据库连接字符串
            var dbConnStr = Config.Config.Options.DbConnStr;

            //创建所需数据库实例对象，将dbConnStr作为参数注入
            var serviceType = typeof(T);
            var serviceInstance = Activator.CreateInstance(serviceType, new object[] { dbConnStr });

            //注入Redis缓存类DataCache
            var redisConnstr = Config.Config.Options.RedisConnStr;
            IDataCache redisCache = new DataCache(redisConnstr, 0);
            var cacheProp = typeof(T).GetProperty("Cache");
            cacheProp?.SetValue(serviceInstance, redisCache);
            return (T)serviceInstance;
        }

        public static DataCache GetCache()
        {
            var redisConnStr = Config.Config.Options.RedisConnStr;
            return new DataCache(redisConnStr, 0);
        }
    }
}
