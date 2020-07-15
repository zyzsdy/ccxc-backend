using System;
using System.Collections.Generic;
using System.Text;
using Ccxc.Core.HttpServer;
using ccxc_backend.DataModels;

namespace ccxc_backend.Controllers.Mail
{
    public class SendMailRequest
    {
        [Required(Message = "内容不能为空")]
        public string content { get; set; }
    }

    public class GetMailRequest
    {
        public int page { get; set; } = 1;
    }

    public class GetMailResponse : BasicResponse
    {
        public int page { get; set; }
        public int page_size { get; set; }
        public int total_count { get; set; }
        public List<MessageView> messages { get; set; }
    }

    public class MessageView : message
    {
        public MessageView(message m)
        {
            mid = m.mid;
            create_time = m.create_time;
            gid = m.gid;
            uid = m.uid;
            content = m.content;
            is_read = m.is_read;
            direction = m.direction;
        }

        public string user_name { get; set; }
        public int roleid { get; set; }
    }
}
