using Ccxc.Core.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace ccxc_backend.Functions
{
    public static class CryptoUtils
    {
        public static string GetLoginHash(string md5pass)
        {
            var passContent = $"=={md5pass}.{Config.Config.Options.PassHashKey1}";
            var hashedPass = HashTools.HmacSha1Base64(passContent, Config.Config.Options.PassHashKey2);
            return hashedPass;
        }

        public static string GetRandomKey()
        {
            var random = new Random();
            var randomSeed = random.Next(0, int.MaxValue);
            var rk = HashTools.Md5Base64(randomSeed.ToString());
            return rk;
        }
    }
}
