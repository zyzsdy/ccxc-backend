using Ccxc.Core.HttpServer;
using System;
using System.Collections.Generic;
using System.Text;

namespace ccxc_backend.Controllers.Users
{
    public class UserRegRequest
    {
        [Required(Message = "用户名不能为空")]
        public string username { get; set; }

        [Required(Message = "E-mail不能为空")]
        public string email { get; set; }

        [Required(Message = "密码不能为空")]
        public string pass { get; set; }
    }

    public class UserLoginRequest
    {
        [Required(Message = "用户名不能为空")]
        public string username { get; set; }

        [Required(Message = "密码不能为空")]
        public string pass { get; set; }
    }

    public class UserLoginResponse : BasicResponse
    {
        public UserLoginInfo user_login_info { get; set; }
        public class UserLoginInfo
        {
            public int uid { get; set; }
            public string username { get; set; }
            public int roleid { get; set; }
            public string token { get; set; }
            public string sk { get; set; }
        }
    }

    public class ModifyPasswordRequest
    {
        [Required(Message = "原密码不能为空")]
        public string old_pass { get; set; }

        [Required(Message = "新密码不能为空")]
        public string pass { get; set; }
    }
}
