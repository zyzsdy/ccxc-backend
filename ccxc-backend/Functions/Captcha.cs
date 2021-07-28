using Hei.Captcha;
using System;
using System.Collections.Generic;
using System.Text;

namespace ccxc_backend.Functions
{
    public static class Captcha
    {
        private static SecurityCodeHelper CaptchaCodeHelper { get; set; } = new SecurityCodeHelper();

        public static byte[] GetCode(out string code)
        {
            code = CaptchaCodeHelper.GetRandomEnDigitalText(4);
            var imgByte = CaptchaCodeHelper.GetGifEnDigitalCodeByte(code);

            return imgByte;
        }
    }
}
