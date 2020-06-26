using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Ccxc.Core.HttpServer
{
    public static class Validation
    {
        public static bool Valid<T>(T model, out string reason) where T : class, new()
        {
            if (model == null)
            {
                reason = "请求格式不正确";
                return false;
            }

            var props = typeof(T).GetProperties();
            reason = "ok";
            foreach (var prop in props)
            {
                var requireAttr = prop.GetCustomAttribute(typeof(RequiredAttribute));
                var paramName = prop.Name;

                if (requireAttr == null) continue;
                var message = (requireAttr as RequiredAttribute)?.Message;

                var value = prop.GetValue(model);
                if (value == null)
                {
                    reason = string.IsNullOrEmpty(message) ? $"参数 {paramName} 不能为空。" : message;
                    return false;
                }

                if (prop.PropertyType.Name != "String") continue;
                if (!string.IsNullOrEmpty(value as string)) continue;
                reason = string.IsNullOrEmpty(message) ? $"参数 {paramName} 不能为空。" : message;
                return false;
            }

            return true;
        }
    }

    public class RequiredAttribute : Attribute
    {
        public string Message { get; set; }
    }
}
