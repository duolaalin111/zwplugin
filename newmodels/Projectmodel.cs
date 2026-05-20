using AntdUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
namespace ZrxDotNetCSProject5.newModels
{
    /// <summary>
    /// 项目管理表格的数据模型
    /// </summary>
    public class Projectmodel : NotifyProperty
    {
        private bool _selected;
        private string _name;
        private string _businessCode;
        private string _wbs;
        private string _createTime;
        private string _creator;
        private string _structure;
        private string _assigned;
        private string _createType;
        private int _statusInt;
        private CellLink[] _cellLinks;
        private long _id;


        [JsonPropertyName("projectState")]
        public int StatusInt
        {
            get => _statusInt;
            set
            {
                if (_statusInt == value) return;
                _statusInt = value;
                OnPropertyChanged(nameof(StatusInt));
                // 当状态整数值变化时，通知状态文本也变化
            }
        }

        [JsonPropertyName("id")]
        public long Id
        {
            get => _id;
            set
            {
                if (_id == value) return;
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }
        /// <summary>
        /// 是否选中（对应复选框列 ColumnCheck("Selected")）
        /// </summary>
        public bool Selected
        {
            get => _selected;
            set
            {
                if (_selected == value) return;
                _selected = value;
                OnPropertyChanged(nameof(Selected));
            }
        }

        /// <summary>
        /// 项目名称
        /// </summary>
        [JsonPropertyName("projectName")]
        public string Name
        {
            get => _name;
            set
            {
                if (_name == value) return;
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        /// <summary>
        /// 商机编号
        /// </summary>
        [JsonPropertyName("businessCode")]
        public string BusinessCode
        {
            get => _businessCode;
            set
            {
                if (_businessCode == value) return;
                _businessCode = value;
                OnPropertyChanged(nameof(BusinessCode));
            }
        }

        /// <summary>
        /// WBS号
        /// </summary>
        [JsonPropertyName("wbs")]
        public string WBS
        {
            get => _wbs;
            set
            {
                if (_wbs == value) return;
                _wbs = value;
                OnPropertyChanged(nameof(WBS));
            }
        }

        /// <summary>
        /// 创建时间（建议用 string 格式如 "2025-03-10 14:30"）
        /// </summary>
        [JsonPropertyName("createTime")]
        public string CreateTime
        {
            get => _createTime;
            set
            {
                if (_createTime == value) return;
                _createTime = value;
                OnPropertyChanged(nameof(CreateTime));
            }
        }

        /// <summary>
        /// 创建人员
        /// </summary>
        [JsonPropertyName("createBy")]
        public string Creator
        {
            get => _creator;
            set
            {
                if (_creator == value) return;
                _creator = value;
                OnPropertyChanged(nameof(Creator));
            }
        }

        /// <summary>
        /// 结构/二次人员
        /// </summary>
        [JsonPropertyName("designBy")]
        public string Structure
        {
            get => _structure;
            set
            {
                if (_structure == value) return;
                _structure = value;
                OnPropertyChanged(nameof(Structure));
            }
        }

        /// <summary>
        /// 被转派人员
        /// </summary>
        [JsonPropertyName("transReceiver")]
        public string Assigned
        {
            get => _assigned;
            set
            {
                if (_assigned == value) return;
                _assigned = value;
                OnPropertyChanged(nameof(Assigned));
            }
        }

        /// <summary>
        /// 创建方式（如 "手动"、"导入" 等）
        /// </summary>
        [JsonPropertyName("createType")]  // 若无此字段，反序列化时会忽略
        public string CreateType
        {
            get => _createType;
            set
            {
                if (_createType == value) return;
                _createType = value;
                OnPropertyChanged(nameof(CreateType));
            }
        }

        /// <summary>
        /// 操作栏内容（可放多个链接/按钮，如 "详情"、"删除"、"编辑"）
        /// </summary>
        public CellLink[] CellLinks
        {
            get => _cellLinks;
            set
            {
                if (_cellLinks == value) return;
                _cellLinks = value;
                OnPropertyChanged(nameof(CellLinks));
            }
        }

        // 构造函数（可选，便于快速创建测试数据）
        public Projectmodel()
        {
            // 可以在这里设置默认值
            CellLinks = new CellLink[0]; // 防止 null
        }
    }
}
