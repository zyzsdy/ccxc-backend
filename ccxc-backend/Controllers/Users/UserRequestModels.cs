using System;
using System.Collections.Generic;
using System.Text;

namespace ccxc_backend.Controllers.Users
{
    public class UserRegRequest
    {
        public string username { get; set; }
        public string email { get; set; }
        public string pass { get; set; }
    }
}
