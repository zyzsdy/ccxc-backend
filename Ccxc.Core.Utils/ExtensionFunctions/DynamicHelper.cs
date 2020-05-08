using System.Collections;
using System.Collections.Generic;
using System.Dynamic;

namespace Ccxc.Core.Utils.ExtensionFunctions
{
    public static class DynamicHelper
    {
        /// <summary>
        /// Dict转为dynamic对象
        /// </summary>
        /// <param name="dict"></param>
        /// <returns></returns>
        public static dynamic ToDynamic(this IDictionary dict)
        {
            IDictionary<string, object> dynamicObj = new ExpandoObject();
            foreach (var key in dict.Keys)
            {
                var keyName = key.ToString();
                if (!dynamicObj.ContainsKey(keyName))
                {
                    dynamicObj.Add(keyName, dict[key]);
                }
            }

            return dynamicObj;
        }
    }
}
