using Ccxc.Core.HttpServer;
using ccxc_backend.DataModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace ccxc_backend.Controllers.Invites
{
    public class SendInviteRequest
    {
        [Required(Message = "用户名不能为空")]
        public string username { get; set; }
    }

    public class ListSentResponse : BasicResponse
    {
        public List<InviteView> result { get; set; }

        public class InviteView : invite
        {
            public string to_username { get; set; }

            public InviteView() { }

            public InviteView(invite i)
            {
                iid = i.iid;
                create_time = i.create_time;
                from_gid = i.from_gid;
                to_uid = i.to_uid;
                valid = i.valid;
            }
        }
    }

    public class InvalidInviteRequest
    {
        [Required(Message = "未选中邀请")]
        public int iid { get; set; }
    }
}
