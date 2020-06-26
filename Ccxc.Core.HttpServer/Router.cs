using Ccxc.Core.Utils.ExtensionFunctions;
using System.Collections.Generic;
using System.Dynamic;

namespace Ccxc.Core.HttpServer
{
    internal class Router
    {
        public static bool Match(string requestPath, string handlerPath, out dynamic pathParams)
        {
            pathParams = new ExpandoObject();

            //完全匹配
            if (requestPath == handlerPath) return true;

            var reqPathNodes = requestPath.TrimEnd('/').Split('/');
            var handlerPathNodes = handlerPath.TrimEnd('/').Split('/');

            //如果路径节点个数和待匹配模式节点个数不相等，则不匹配
            if (reqPathNodes.Length != handlerPathNodes.Length) return false;

            var pathParamsDict = new Dictionary<string, string>();
            for (var i = 0; i < reqPathNodes.Length; i++)
            {
                var req = reqPathNodes[i];
                var pattern = handlerPathNodes[i];

                if (req == pattern) continue;

                if (!pattern.StartsWith(":")) return false;
                var paramKey = pattern.Substring(1);
                if (!pathParamsDict.ContainsKey(paramKey))
                {
                    pathParamsDict.Add(paramKey, req);
                }
            }

            pathParams = pathParamsDict.ToDynamic();
            return true;
        }
    }
}