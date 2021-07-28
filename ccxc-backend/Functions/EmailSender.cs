using Aliyun.Acs.Core;
using Aliyun.Acs.Core.Profile;
using Aliyun.Acs.Dm.Model.V20170622;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ccxc_backend.Functions
{
    public static class EmailSender
    {
        public static Task<bool> SendVerify(string email, string token)
        {
            return Task.Run(() =>
            {
                var regionId = "ap-southeast-1";
                var regionHost = "dm.ap-southeast-1.aliyuncs.com";

                IClientProfile profile = DefaultProfile.GetProfile(regionId, Config.Config.Options.AliyunDmAccessKey, Config.Config.Options.AliyunDmAccessSecret);
                profile.AddEndpoint(regionHost, regionId, "Dm", regionHost);

                IAcsClient client = new DefaultAcsClient(profile);
                var request = new SingleSendMailRequest();

                try
                {
                    request.AccountName = "noreply@notice.cipherpuzzles.com";
                    request.FromAlias = "密码菌（请勿回复本地址）";
                    request.AddressType = 1;
                    request.TagName = "restorePass";
                    request.ReplyToAddress = false;
                    request.ToAddress = email;
                    request.Subject = "CCBC 11 重置密码验证";
                    request.HtmlBody = $@"
<p>尊敬的用户：</p>
<p>&nbsp;</p>
<p>您收到此邮件是因为您在CCBC 11网站中尝试进行密码重置。本邮件将会引导您完成之后的步骤。</p>
<p>&nbsp;</p>
<p>如果不是您申请的密码重置，可能是其他人错误的填写了您的邮件地址，请忽略本邮件。</p>
<p>要继续密码重置，请<a href=""https://ccbc11.cipherpuzzles.com/resetpass?token={token}"" target=""_blank"">点击此链接</a>进入密码重置页，然后在密码重置页上输入您的新密码。</p>
<p>&nbsp;</p>
<p>如果您点击以上链接无效，请尝试将以下链接复制到您的浏览器并打开：</p>
<p>https://ccbc11.cipherpuzzles.com/resetpass?token={token}</p>
<p>&nbsp;</p>
<p>祝参赛愉快。</p>
<p>CCBC 11组委会</p>
<hr>
<p>请勿回复本邮件，如有问题可发送邮件至info@cipherpuzzles.com咨询。</p>
<p>请关注微信公众号【密码菌】持续获取资讯</p>";
                    var response = client.GetAcsResponse(request);

                    return true;
                }
                catch (Exception e)
                {
                    Ccxc.Core.Utils.Logger.Error(e.ToString());
                    return false;
                }
            });
        }
    }
}
