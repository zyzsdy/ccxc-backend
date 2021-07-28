using Ccxc.Core.HttpServer;
using ccxc_backend.DataModels;
using ccxc_backend.DataServices;
using Hei.Captcha;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using System.Threading.Tasks;

namespace ccxc_backend.Controllers.Users
{
    [Export(typeof(HttpController))]
    public class EmailVerifyController : HttpController
    {
        [HttpHandler("GET", "/getcaptcha")]
        public async Task GetCaptcha(Request request, Response response)
        {
            var imgByte = Functions.Captcha.GetCode(out string code);

            var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

            var cache = DbFactory.GetCache();
            var cKey = cache.GetCacheKey($"captcha_{token}");
            await cache.Put(cKey, code, 120000);

            response.SetHeader("Access-Control-Expose-Headers", "X-Captcha-Nonce");
            response.SetHeader("X-Captcha-Nonce", token);
            await response.BinaryResponse(200, imgByte, "image/gif");
        }

        [HttpHandler("POST", "/send-reset-pass-email")]
        public async Task SendResetPassEmail(Request request, Response response)
        {
            var requestJson = request.Json<EmailResetPassRequest>();
            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            var cache = DbFactory.GetCache();
            var cKey = cache.GetCacheKey($"captcha_{requestJson.nonce}");
            var code = await cache.Get<string>(cKey);

            if (string.IsNullOrEmpty(code))
            {
                await response.BadRequest("验证码错误");
                return;
            }

            if (code.ToLower() != requestJson.code.ToLower())
            {
                await response.BadRequest("验证码错误");
                return;
            }

            var now = DateTime.Now;

            var emailTokenKey = cache.GetDataKey("tokenbucket/email_total_sender");
            var emailToken = await cache.Client.RateLimiter(emailTokenKey, 20, Ccxc.Core.Utils.UnixTimestamp.GetTimestamp(now), 200, 1);
            if (emailToken < 1)
            {
                await response.BadRequest("Email服务器：邮件发送太多，请稍后再试。");
                return;
            }

            var userTokenKey = cache.GetDataKey($"tokenbucket/user_{requestJson.userid}");
            var userToken = await cache.Client.RateLimiter(userTokenKey, 1, Ccxc.Core.Utils.UnixTimestamp.GetTimestamp(now), 1440, 1);
            if (userToken < 1)
            {
                await response.BadRequest("Email服务器：重试太快，请稍后再试。");
                return;
            }

            var userDb = DbFactory.Get<User>();
            var user = await userDb.SimpleDb.AsQueryable().Where(x => x.email == requestJson.email).FirstAsync();
            if (user == null)
            {
                await response.BadRequest("Email服务器：发送失败，请稍后再试，或检查邮件地址是否填写错误。");
                return;
            }

            var restoreToken = Guid.NewGuid().ToString("n");
            restoreToken += Guid.NewGuid().ToString("n");

            user.info_key = restoreToken;

            await userDb.SimpleDb.AsUpdateable(user).UpdateColumns(x => new { x.info_key }).ExecuteCommandAsync();

            var verifyKey = cache.GetCacheKey($"emailVerify/{restoreToken}");
            await cache.Put(verifyKey, user, 2400000); //40分钟有效期。

            //触发邮件推送

            await response.OK();
        }
    }
}
