using System;
using System.Collections.Generic;
using System.Text;
using Ccxc.Core.HttpServer;
using Ccxc.Core.Utils.ExtensionFunctions;
using ccxc_backend.DataModels;
using Newtonsoft.Json;

namespace ccxc_backend.Controllers.Admin
{
    public class GetAllUserResponse : BasicResponse
    {
        public List<UserView> users { get; set; }
    }

    public class UserView : user
    {
        [JsonConverter(typeof(UnixTimestampConverter))]
        public DateTime last_action_time { get; set; }

        /// <summary>
        /// 是否为测试用户（1-是 0-不是） 
        /// </summary>
        public int is_beta_user { get; set; }

        public UserView(user u)
        {
            uid = u.uid;
            username = u.username;
            email = u.email;
            phone = u.phone;
            roleid = u.roleid;
            update_time = u.update_time;
            create_time = u.create_time;
            profile = u.profile;
            info_key = u.info_key;
        }
    }

    public class AdminUidRequest
    {
        [Required(Message = "UID不能为空")]
        public int uid { get; set; }
    }
}
