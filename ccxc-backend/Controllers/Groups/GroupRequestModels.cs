using Ccxc.Core.HttpServer;
using System;
using System.Collections.Generic;
using System.Text;

namespace ccxc_backend.Controllers.Groups
{
    public class CreateGroupRequest
    {
        [Required(Message = "队名不能为空")]
        public string groupname { get; set; }

        public string profile { get; set; }
    }
}
