using Ccxc.Core.HttpServer;
using ccxc_backend.DataModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace ccxc_backend.Controllers.Admin
{
    public class AddPuzzleGroupRequest
    {
        [Required(Message = "分区名称不能为空")]
        public string pg_name { get; set; }
        public string pg_desc { get; set; }
    }
    
    public class DeletePuzzleGroupRequest
    {
        public int pgid { get; set; }
    }

    public class EditPuzzleGroupRequest
    {
        public int pgid { get; set; }

        [Required(Message = "分区名称不能为空")]
        public string pg_name { get; set; }
        public string pg_desc { get; set; }
    }

    public class GetPuzzleGroupResponse : BasicResponse
    {
        public List<puzzle_group> puzzle_group { get; set; }
    }
}
