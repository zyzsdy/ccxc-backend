using Ccxc.Core.HttpServer;
using System;
using System.Collections.Generic;
using System.Text;

namespace ccxc_backend.Controllers.Admin
{
    public class PurgeCacheRequest
    {
        [Required(Message = "操作Key不能为空")]
        public string op_key { get; set; }
    }
}
