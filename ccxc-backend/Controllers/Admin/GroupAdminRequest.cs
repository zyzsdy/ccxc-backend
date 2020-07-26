using System;
using System.Collections.Generic;
using System.Text;

namespace ccxc_backend.Controllers.Admin
{
    public class UserGroupNameListResponse : BasicResponse
    {
        public List<UserGroupNameInfo> group_name_list { get; set; }
    }

    public class UserGroupNameInfo
    {
        public int gid { get; set; }
        public string groupname { get; set; }
    }

    public class GroupAdminRequest
    {
        public int gid { get; set; }
    }

    public class GetPenaltyResponse : BasicResponse
    {
        public double penalty { get; set; }
    }
}
