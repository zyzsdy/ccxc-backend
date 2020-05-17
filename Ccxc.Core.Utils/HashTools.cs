using System;
using System.Security.Cryptography;
using System.Text;

namespace Ccxc.Core.Utils
{
    public static class HashTools
    {
        public static string HmacSha1Base64(string content, string key)
        {
            using (var hmac = new HMACSHA1
            {
                Key = Encoding.UTF8.GetBytes(key)
            })
            {
                var hashedData = hmac.ComputeHash(Encoding.UTF8.GetBytes(content));
                return Convert.ToBase64String(hashedData);
            }
        }

        public static string Md5Base64(string content)
        {
            using (var md5 = MD5.Create())
            {
                var hashedData = md5.ComputeHash(Encoding.UTF8.GetBytes(content));
                return Convert.ToBase64String(hashedData);
            }
        }
    }
}
