using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZrxDotNetCSProject5
{
    public class ProjectInfo
    {
        public int ID { get; set; }                    // 数据库主键
        public string 项目名称 { get; set; }
        public string 商机编号 { get; set; }
        public string WBS号 { get; set; }
        public DateTime 创建时间 { get; set; }
        public string 创建人员 { get; set; }
        public string 结构二次人员 { get; set; }
        public string 被转派人员 { get; set; }
        public string 创建方式 { get; set; }
        public string 项目状态 { get; set; }
    }
}
