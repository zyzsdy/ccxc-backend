using Ccxc.Core.HttpServer;
using ccxc_backend.DataModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace ccxc_backend.Controllers.Announcements
{
    public class AddAnnoRequest
    {
        [Required(Message = "内容不能为空")]
        public string content { get; set; }
    }

    public class DeleteAnnoRequest
    {
        public int aid { get; set; }
    }

    public class EditAnnoRequest
    {
        public int aid { get; set; }

        [Required(Message = "内容不能为空")]
        public string content { get; set; }
    }

    public class GetAnnoRequest
    {
        public List<int> aids { get; set; }
    }

    public class GetAnnoResponse : BasicResponse
    {
        public List<announcement> announcements { get; set; }
    }

    public class GetTempAnnoResponse : BasicResponse
    {
        public List<temp_anno> temp_anno { get; set; }
    }

    public class ConvertTempAnnoRequest
    {
        public int pid { get; set; }
    }
}
