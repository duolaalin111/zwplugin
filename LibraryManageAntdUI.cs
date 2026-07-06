using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using AntdUI;
using System.IO;
using System.Linq;
using ZwSoft.ZwCAD.ApplicationServices;
using System.Threading;

namespace ZrxDotNetCSProject5
{
    public class DrawingDetail
    {
        public long Id { get; set; }           // 新增：图纸ID，用于编辑提交
        public long TypeId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string FilePath { get; set; }
        public string PreviewPath { get; set; }
        public string DescProp { get; set; }  // JSON 字符串
        public string CreateTime { get; set; }
        public string UpdateTime { get; set; }
    }

    // 接口返回的树节点结构
    public class SchemeNode
    {
        public long Id { get; set; }
        public long ParentId { get; set; }
        public int ProductAttrId { get; set; }
        public string Name { get; set; }
        public string AttrIds { get; set; }
        public string CreateTime { get; set; }
        public string UpdateTime { get; set; }
    }

    // 图纸简单信息
    public class DrawingSimple
    {
        public long Id { get; set; }
        public string Code { get; set; }
        public string ThumbPath { get; set; }
    }

    public partial class LibraryManageAntdUI : Form
    {
        private DrawingDetail currentDrawingDetail = null;
        private AntdUI.Select productSelect;
        private System.Windows.Forms.FlowLayoutPanel flowPanel;
        private AntdUI.Tree leftTree;
        private AntdUI.Table propertyTable;
        private AntdUI.Label lblCurrentPath;
        private DataTable propertyDataTable;
        private AntdUI.Select filterSelect;

        public class LibraryNode
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public List<LibraryNode> Children { get; set; } = new List<LibraryNode>();
            public string[] Drawings { get; set; } = Array.Empty<string>();
        }

        // 与后端接口对应的模型
        private class ProductGroup
        {
            public int Id { get; set; }
            public string GroupName { get; set; }
            public string Models { get; set; }
        }

        private Dictionary<TreeItem, LibraryNode> treeItemMapping;
        private Dictionary<TreeItem, TreeItem> childToParentMap;
        private Dictionary<string, List<LibraryNode>> productData;
        private string currentProduct;
        private string currentGroupName;
        private TreeItem currentTreeItem;
        private LibraryNode currentNode;
        private DrawingSimple[] currentAllDrawings;
        private DrawingSimple[] currentFilteredDrawings;
        // 当前节点的筛选相关数据
        private string currentFirstAttrName = "";
        private List<string> currentValScopeList = new List<string>();

        private HttpClient httpClient;
        private List<ProductGroup> productGroups = new List<ProductGroup>();
        private Dictionary<string, ProductGroup> modelToGroupMap = new Dictionary<string, ProductGroup>();

        private readonly SynchronizationContext _syncContext;

        public LibraryManageAntdUI()
        {
            InitializeComponent();
            _syncContext = SynchronizationContext.Current;
            httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(AppConfig.ApiBaseUrl);
            this.Load += LibraryManageAntdUI_Load;
        }

        private async void LibraryManageAntdUI_Load(object sender, EventArgs e)
        {
            SetupLibraryManagementUI();
            await LoadProductGroupsAsync();
        }

        // 从接口获取产品组数据
        private async Task LoadProductGroupsAsync()
        {
            try
            {
                productSelect.Enabled = false;
                productSelect.PlaceholderText = "加载中...";

                var response = await httpClient.GetAsync("api/model-groups");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    if (root.GetProperty("code").GetInt32() != 200)
                    {
                        AntdUI.Message.error(this, $"接口返回错误：{root.GetProperty("message").GetString()}");
                        return;
                    }

                    var dataArray = root.GetProperty("data").EnumerateArray();
                    productGroups.Clear();
                    modelToGroupMap.Clear();

                    var newProductData = new Dictionary<string, List<LibraryNode>>();

                    foreach (var item in dataArray)
                    {
                        var group = new ProductGroup
                        {
                            Id = item.GetProperty("id").GetInt32(),
                            GroupName = item.GetProperty("groupName").GetString(),
                            Models = item.GetProperty("models").GetString()
                        };
                        productGroups.Add(group);

                        if (!string.IsNullOrEmpty(group.Models))
                        {
                            var models = group.Models.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var model in models)
                            {
                                modelToGroupMap[model.Trim()] = group;
                            }
                        }

                        newProductData[group.GroupName] = new List<LibraryNode>();
                    }

                    productData = newProductData;

                    productSelect.Items.Clear();
                    foreach (var model in modelToGroupMap.Keys)
                    {
                        productSelect.Items.Add(new SelectItem(model, model));
                    }

                    if (productSelect.Items.Count > 0)
                        productSelect.SelectedIndex = 0;

                    productSelect.Enabled = true;
                    productSelect.PlaceholderText = "请选择产品";
                    AntdUI.Message.success(this, $"已加载 {productSelect.Items.Count} 个产品型号");
                }
            }
            catch (Exception ex)
            {
                AntdUI.Message.error(this, $"加载产品失败：{ex.Message}");
                productSelect.Enabled = true;
                productSelect.PlaceholderText = "加载失败";
            }
        }

        // 根据产品ID加载树结构
        private async Task<List<LibraryNode>> LoadTreeDataAsync(int productId)
        {
            try
            {
                var response = await httpClient.GetAsync($"api/product-schemes?productId={productId}");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    if (root.GetProperty("code").GetInt32() != 200)
                    {
                        AntdUI.Message.error(this, $"加载树结构失败：{root.GetProperty("message").GetString()}");
                        return new List<LibraryNode>();
                    }

                    var dataArray = root.GetProperty("data").EnumerateArray();
                    var nodes = new List<SchemeNode>();
                    foreach (var item in dataArray)
                    {
                        nodes.Add(new SchemeNode
                        {
                            Id = item.GetProperty("id").GetInt64(),
                            ParentId = item.GetProperty("parentId").GetInt64(),
                            ProductAttrId = item.GetProperty("productAttrId").GetInt32(),
                            Name = item.GetProperty("name").GetString(),
                            AttrIds = item.GetProperty("attrIds").GetString(),
                            CreateTime = item.GetProperty("createTime").GetString(),
                            UpdateTime = item.GetProperty("updateTime").GetString()
                        });
                    }

                    return BuildTreeFromNodes(nodes);
                }
            }
            catch (Exception ex)
            {
                AntdUI.Message.error(this, $"加载树结构失败：{ex.Message}");
                return new List<LibraryNode>();
            }
        }

        // 将平面节点列表转换为树形结构
        private List<LibraryNode> BuildTreeFromNodes(List<SchemeNode> nodes)
        {
            var nodeDict = new Dictionary<long, LibraryNode>();
            var rootNodes = new List<LibraryNode>();

            foreach (var node in nodes)
            {
                var libNode = new LibraryNode
                {
                    Id = node.Id,
                    Name = node.Name,
                    Children = new List<LibraryNode>(),
                    Drawings = Array.Empty<string>()
                };
                nodeDict[node.Id] = libNode;
            }

            foreach (var node in nodes)
            {
                if (node.ParentId == 0)
                {
                    rootNodes.Add(nodeDict[node.Id]);
                }
                else
                {
                    if (nodeDict.TryGetValue(node.ParentId, out var parent))
                    {
                        parent.Children.Add(nodeDict[node.Id]);
                    }
                    else
                    {
                        rootNodes.Add(nodeDict[node.Id]);
                    }
                }
            }

            return rootNodes;
        }

        // 异步加载叶子节点的图纸，并更新筛选器
        //old
        private async Task LoadDrawingsForNode(LibraryNode node)
        {
            try
            {
                flowPanel.Controls.Clear();
                AntdUI.Message.info(this, "加载图纸中...");

                var response = await httpClient.GetAsync($"api/drawings/simple?cabinetId={node.Id}");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    if (root.GetProperty("code").GetInt32() != 200)
                    {
                        AntdUI.Message.error(this, $"加载图纸失败：{root.GetProperty("message").GetString()}");
                        return;
                    }

                    var dataArray = root.GetProperty("data").EnumerateArray();
                    var drawings = new List<DrawingSimple>();
                    string firstAttrName = "";
                    string valScopeList = "";

                    foreach (var item in dataArray)
                    {
                        drawings.Add(new DrawingSimple
                        {
                            Id = item.GetProperty("id").GetInt64(),
                            Code = item.GetProperty("code").GetString(),
                            ThumbPath = item.GetProperty("thumbPath").GetString()
                        });
                        // 提取筛选元数据（每个项都有，取第一个即可）
                        if (string.IsNullOrEmpty(firstAttrName) && item.TryGetProperty("firstAttrName", out var fName))
                        {
                            firstAttrName = fName.GetString();
                        }
                        if (string.IsNullOrEmpty(valScopeList) && item.TryGetProperty("valScopeList", out var vList))
                        {
                            valScopeList = vList.GetString();
                        }
                    }

                    // 更新筛选器
                    UpdateFilter(firstAttrName, valScopeList);

                    if (drawings.Count == 0)
                    {
                        AntdUI.Message.warn(this, "该分类下暂无图纸");
                        return;
                    }

                    // 保存原始图纸列表用于筛选
                    currentAllDrawings = drawings.ToArray();
                    // 默认显示全部
                    await LoadFilteredDrawings(node.Id, ""); // 空字符串表示全部
                }
            }
            catch (Exception ex)
            {
                AntdUI.Message.error(this, $"加载图纸失败：{ex.Message}");
            }
        }

        // 更新筛选器UI
        private void UpdateFilter(string firstAttrName, string valScopeList)
        {
            currentFirstAttrName = firstAttrName;
            currentValScopeList.Clear();

            if (!string.IsNullOrEmpty(valScopeList))
            {
                var scopes = valScopeList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var scope in scopes)
                {
                    currentValScopeList.Add(scope.Trim());
                }
            }

            // 更新标签文本
            if (!string.IsNullOrEmpty(currentFirstAttrName))
            {
                var lblFilter = this.Controls.Find("lblFilter", true)[0] as AntdUI.Label;
                if (lblFilter != null) lblFilter.Text = currentFirstAttrName + "：";
            }

            // 更新下拉框选项
            filterSelect.Items.Clear();
            filterSelect.Items.Add(new SelectItem("全部", "all"));
            foreach (var scope in currentValScopeList)
            {
                filterSelect.Items.Add(new SelectItem(scope, scope));
            }
            filterSelect.SelectedIndex = 0;
        }

        // 根据筛选值加载图纸列表（调用过滤接口）
        private async Task LoadFilteredDrawings(long cabinetId, string valScope)
        {
            try
            {
                var requestBody = new { cabinetId, valScope };
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("api/drawings/filter", content);
                response.EnsureSuccessStatusCode();
                var responseJson = await response.Content.ReadAsStringAsync();

                using (JsonDocument doc = JsonDocument.Parse(responseJson))
                {
                    var root = doc.RootElement;
                    if (root.GetProperty("code").GetInt32() != 200)
                    {
                        AntdUI.Message.error(this, $"筛选图纸失败：{root.GetProperty("message").GetString()}");
                        return;
                    }

                    var dataArray = root.GetProperty("data").EnumerateArray();
                    var drawings = new List<DrawingSimple>();
                    foreach (var item in dataArray)
                    {
                        drawings.Add(new DrawingSimple
                        {
                            Id = item.GetProperty("id").GetInt64(),
                            Code = item.GetProperty("code").GetString(),
                            ThumbPath = item.GetProperty("thumbPath").GetString()
                        });
                    }

                    // 显示图纸卡片
                    LoadDrawingsCards(drawings);
                }
            }
            catch (Exception ex)
            {
                AntdUI.Message.error(this, $"筛选图纸失败：{ex.Message}");
            }
        }

        private async Task LoadImageAsync(PictureBox pictureBox, string thumbPath)
        {
            if (string.IsNullOrWhiteSpace(thumbPath)) return;

            try
            {
                // 自动补全 URL
                if (!thumbPath.StartsWith("http"))
                {
                    thumbPath = httpClient.BaseAddress + thumbPath.TrimStart('/');
                }

                var bytes = await httpClient.GetByteArrayAsync(thumbPath);
                using (var ms = new MemoryStream(bytes))
                {
                    var img = Image.FromStream(ms);
                    if (!pictureBox.IsDisposed)
                    {
                        pictureBox.BeginInvoke(new Action(() =>
                        {
                            pictureBox.Image = img;
                        }));
                    }
                }
            }
            catch (Exception)
            {
                pictureBox.Invoke(new Action(() =>
                {
                    pictureBox.BackColor = Color.FromArgb(80, 50, 50);
                    pictureBox.Image = null;
                }));
            }
        }

        // 显示图纸卡片（带缩略图），点击时加载详情
        private void LoadDrawingsCards(List<DrawingSimple> drawings)
        {
            flowPanel.Controls.Clear();

            foreach (var drawing in drawings)
            {
                var card = new AntdUI.Panel
                {
                    Width = 240,
                    Height = 180,
                    BackColor = Color.FromArgb(30, 30, 40),
                    Margin = new Padding(6),
                    Cursor = Cursors.Hand,
                    Tag = drawing
                };

                // 图片框
                var pic = new PictureBox
                {
                    Width = 220,
                    Height = 120,
                    Location = new Point(10, 10),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.FromArgb(45, 45, 60)
                };

                // 底部文字
                var lbl = new AntdUI.Label
                {
                    Text = drawing.Code,
                    ForeColor = Color.White,
                    Font = new Font("微软雅黑", 9, FontStyle.Bold),
                    AutoEllipsis = true,
                    Width = 220,
                    Height = 20,
                    Location = new Point(10, 140)
                };

                card.Controls.Add(pic);
                card.Controls.Add(lbl);

                // 点击事件
                card.Click += async (s, e) =>
                {
                    await LoadDrawingDetailAndUpdateTable(drawing.Id);
                };
                pic.Click += async (s, e) =>
                {
                    await LoadDrawingDetailAndUpdateTable(drawing.Id);
                };

                flowPanel.Controls.Add(card);

                // 异步加载图片
                _ = LoadImageAsync(pic, drawing.ThumbPath);
            }
        }

        // 根据图纸ID加载详情并更新右侧表格
        private async Task LoadDrawingDetailAndUpdateTable(long drawingId)
        {
            try
            {
                var response = await httpClient.GetAsync($"api/drawings/detail?id={drawingId}");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    if (root.GetProperty("code").GetInt32() != 200)
                    {
                        AntdUI.Message.error(this, $"获取图纸详情失败：{root.GetProperty("message").GetString()}");
                        return;
                    }

                    var dataElement = root.GetProperty("data");
                    var detail = new DrawingDetail
                    {
                        Id = drawingId,   // 保存图纸ID，用于后续编辑提交
                        TypeId = dataElement.GetProperty("typeId").GetInt64(),
                        Code = dataElement.GetProperty("code").GetString(),
                        Name = dataElement.GetProperty("name").GetString(),
                        FilePath = dataElement.GetProperty("filePath").GetString(),
                        PreviewPath = dataElement.GetProperty("previewPath").GetString(),
                        DescProp = dataElement.GetProperty("descProp").GetString(),
                        CreateTime = dataElement.GetProperty("createTime").GetString(),
                        UpdateTime = dataElement.GetProperty("updateTime").GetString()
                    };

                    currentDrawingDetail = detail; // 保存当前详情
                    UpdatePropertyTableFromDetail(detail);
                    AntdUI.Message.success(this, $"已选中：{detail.Code}");
                }
            }
            catch (Exception ex)
            {
                AntdUI.Message.error(this, $"加载图纸详情失败：{ex.Message}");
            }
        }

        // 根据详情数据动态生成属性表格行
        private void UpdatePropertyTableFromDetail(DrawingDetail detail)
        {
            propertyDataTable.Rows.Clear();

            propertyDataTable.Rows.Add("图纸编号", detail.Code);
            propertyDataTable.Rows.Add("图纸名称", detail.Name);
            propertyDataTable.Rows.Add("创建时间", detail.CreateTime);
            propertyDataTable.Rows.Add("修改时间", detail.UpdateTime);

            if (!string.IsNullOrEmpty(detail.DescProp))
            {
                try
                {
                    using (JsonDocument descDoc = JsonDocument.Parse(detail.DescProp))
                    {
                        var root = descDoc.RootElement;
                        foreach (var property in root.EnumerateObject())
                        {
                            string displayName = property.Name;
                            string value = property.Value.GetString();
                            propertyDataTable.Rows.Add(displayName, value);
                        }
                    }
                }
                catch
                {
                    propertyDataTable.Rows.Add("扩展属性", "解析失败");
                }
            }

            propertyTable?.Refresh();
        }

        // 保存图纸详情到后端（实际调用编辑接口）
        private async Task SaveDrawingDetailAsync(DrawingDetail detail)
        {
            try
            {
                var requestBody = new
                {
                    id = detail.Id,
                    properties = detail.DescProp   // DescProp 已经是 JSON 字符串
                };
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("api/drawings/edit", content);
                response.EnsureSuccessStatusCode();
                var responseJson = await response.Content.ReadAsStringAsync();

                using (JsonDocument doc = JsonDocument.Parse(responseJson))
                {
                    var root = doc.RootElement;
                    if (root.GetProperty("code").GetInt32() == 200)
                    {
                        AntdUI.Message.success(this, "属性保存成功");
                        // 更新本地缓存
                        currentDrawingDetail = detail;
                        UpdatePropertyTableFromDetail(detail);
                    }
                    else
                    {
                        AntdUI.Message.error(this, $"保存失败：{root.GetProperty("message").GetString()}");
                    }
                }
            }
            catch (Exception ex)
            {
                AntdUI.Message.error(this, $"保存失败：{ex.Message}");
            }
        }

        // UI 初始化
        private void SetupLibraryManagementUI()
        {
            this.Controls.Clear();
            this.Text = "图库管理";
            this.Size = new Size(1450, 900);    //原1400，800
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 245, 245);
            this.MinimumSize = new Size(1400, 700);  //原1200，600

            // 顶部栏
            System.Windows.Forms.Panel topBar = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(10),
                BackColor = Color.White
            };

            var lblTop = new AntdUI.Label
            {
                Text = "选择产品：",
                AutoSize = true,
                Location = new Point(10, 18),
                Font = new Font("微软雅黑", 10)
            };

            productSelect = new AntdUI.Select
            {
                PlaceholderText = "请选择产品",
                Location = new Point(90, 12),
                Size = new Size(200, 36)
            };

            topBar.Controls.Add(lblTop);
            topBar.Controls.Add(productSelect);

            // 主体布局
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(10)
            };

            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));

            this.Controls.Add(mainLayout);
            this.Controls.Add(topBar);

            // 左侧树形面板
            System.Windows.Forms.Panel pnlLeft = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 10, 0)
            };

            var lblLeft = new AntdUI.Label
            {
                Text = " 图库结构",
                Dock = DockStyle.Top,
                Height = 40,
                Font = new Font("微软雅黑", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 100, 100)
            };

            leftTree = new AntdUI.Tree
            {
                Dock = DockStyle.Fill,
                BlockNode = true,
                Font = new Font("微软雅黑", 9)
            };
            leftTree.SelectChanged += LeftTree_SelectChanged;

            pnlLeft.Controls.Add(leftTree);
            pnlLeft.Controls.Add(lblLeft);
            mainLayout.Controls.Add(pnlLeft, 0, 0);

            // 中间内容面板
            System.Windows.Forms.Panel pnlMid = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 10, 0)
            };

            System.Windows.Forms.Panel midTop = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.White
            };

            var lblPathTitle = new AntdUI.Label
            {
                Text = "当前路径：",
                AutoSize = true,
                Location = new Point(10, 15),
                Font = new Font("微软雅黑", 10)
            };

            lblCurrentPath = new AntdUI.Label
            {
                Text = "未选择",
                AutoSize = true,
                Location = new Point(100, 15),
                Font = new Font("微软雅黑", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(24, 144, 255)
            };

            var lblFilter = new AntdUI.Label
            {
                Name = "lblFilter",
                Text = "主属性：",
                AutoSize = true,
                Location = new Point(400, 15),
                Font = new Font("微软雅黑", 10)
            };

            filterSelect = new AntdUI.Select
            {
                PlaceholderText = "全部",
                Location = new Point(490, 10),  //原470
                Size = new Size(150, 30)
            };
            filterSelect.SelectedIndexChanged += FilterSelect_SelectedIndexChanged;

            midTop.Controls.Add(lblPathTitle);
            midTop.Controls.Add(lblCurrentPath);
            midTop.Controls.Add(lblFilter);
            midTop.Controls.Add(filterSelect);


            this.flowPanel = new System.Windows.Forms.FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(248, 248, 248)
            };

            pnlMid.Controls.Add(flowPanel);
            pnlMid.Controls.Add(midTop);
            mainLayout.Controls.Add(pnlMid, 1, 0);

            // 右侧属性面板
            System.Windows.Forms.Panel pnlRight = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(0)
            };

            var lblRightTop = new AntdUI.Label
            {
                Text = "📋 图片详细信息",
                Dock = DockStyle.Top,
                Height = 45,
                Font = new Font("微软雅黑", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 80, 80),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };

            // 扩展属性表格数据源
            propertyDataTable = new DataTable();
            propertyDataTable.Columns.Add("属性", typeof(string));
            propertyDataTable.Columns.Add("值", typeof(string));
            propertyDataTable.Rows.Add("提示", "点击图纸查看详情");

            propertyTable = new AntdUI.Table
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                DataSource = propertyDataTable,
                Font = new Font("微软雅黑", 9)
            };

            if (propertyTable.Columns.Count >= 2)
            {
                propertyTable.Columns[0].Width = "90";
                propertyTable.Columns[1].Width = "210";
            }

            // 底部按钮区
            TableLayoutPanel btnGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 100,   //原80，替换为100
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(5),
                BackColor = Color.FromArgb(250, 250, 250)
            };

            btnGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            btnGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            btnGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            btnGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            var btnIn = new AntdUI.Button
            {
                Text = "📥 入库",
                Type = TTypeMini.Primary,
                Dock = DockStyle.Fill,
                Margin = new Padding(2),
                Font = new Font("微软雅黑", 9)
            };
            //btnIn.Click += (s, e) => AntdUI.Message.info(this, "入库操作");
            btnIn.Click += async (s, e) =>
            {
                // 1. 验证是否选中了叶子节点
                if (currentNode == null || (currentNode.Children != null && currentNode.Children.Count > 0))
                {
                    AntdUI.Message.warn(this, "请先在左侧树中选择具体的入库分类（叶子节点）！");
                    return;
                }

                // 获取选中的分类ID
                long selectedTypeId = currentNode.Id;

                var doc = ZwSoft.ZwCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                if (doc == null)
                {
                    AntdUI.Message.error(this, "未检测到CAD文档");
                    return;
                }

                // 执行CAD命令
                bool isSuccess = await SendCommandAndWaitAsync(doc, "ZWCAD_入库 ", "ZWCAD_入库");
                if (isSuccess)
                {
                    await UploadDrawingsToBackend(selectedTypeId);
                }
                else
                {
                    AntdUI.Message.warn(this, "入库命令被取消或执行失败，已终止上传。");
                }
            };
            
            var btnOut = new AntdUI.Button
            {
                Text = "📤 出库",
                Type = TTypeMini.Default,
                Dock = DockStyle.Fill,
                Margin = new Padding(2),
                Font = new Font("微软雅黑", 9)
            };
            //btnOut.Click += (s, e) => AntdUI.Message.info(this, "出库操作");       
            btnOut.Click += async (s, e) =>
            {
                
                // 1. 检查是否选中图纸
                if (currentDrawingDetail == null)
                {
                    AntdUI.Message.warn(this, "请先选择一张图纸再进行出库！");
                    return;
                }
                


                long drawingId = currentDrawingDetail.Id;
                string fileName = currentDrawingDetail.Code + ".dwg";
                string fileUrl = currentDrawingDetail.FilePath;
                
                try
                {
                    AntdUI.Message.info(this, "正在准备出库文件...");
                    
                    // 2. 确定 chuku 文件夹
                    string projectDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    string chukuDir = Path.Combine(projectDir, "chuku");
                    if (!Directory.Exists(chukuDir)) Directory.CreateDirectory(chukuDir);

                    // 清理旧文件
                    foreach (var file in Directory.GetFiles(chukuDir, "*.dwg")) File.Delete(file);

                    string localSavePath = Path.Combine(chukuDir, fileName);

                    // 3. 下载文件
                    await DownloadFileAsync(fileUrl, localSavePath);
                    
                    // 4. 弹出对话框询问出库方式
                    var doc = ZwSoft.ZwCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                    if (doc == null)
                    {
                        AntdUI.Message.error(this, "未找到活动的 CAD 文档");
                        return;
                    }

                    // 使用 AntdUI 模态框
                    AntdUI.Modal.open(new AntdUI.Modal.Config(this, "出库选项", "请选择出库方式：", AntdUI.TType.Info)
                    {
                        Btns = new AntdUI.Modal.Btn[]
                        {
                new AntdUI.Modal.Btn("normal", "普通出库", AntdUI.TTypeMini.Primary),
                new AntdUI.Modal.Btn("explode", "炸开出库", AntdUI.TTypeMini.Primary)
                        },
                        CancelText = null,   // 隐藏默认的取消按钮
                        OkText = "取消",       // 隐藏默认的确定按钮
                        OnBtns = btn =>
                        {
                            if (btn.Name == "normal")
                            {
                                doc.SendStringToExecute("ZWCAD_出库1 ", true, false, false);
                                AntdUI.Message.success(this, "出库文件已下载，请在 CAD 中操作。");
                            }
                            else if (btn.Name == "explode")
                            {
                                doc.SendStringToExecute("ZWCAD_出库1_Explode ", true, false, false);
                                AntdUI.Message.success(this, "出库文件已下载，请在 CAD 中操作。");
                            }
                            return true; // 返回 true 表示关闭对话框
                        },
                        OnOk = config =>
                        {
                            AntdUI.Message.info(this, "取消出库");
                            return true;
                        }

                    });

                    
                }
                catch (Exception ex)
                {
                    AntdUI.Message.error(this, $"准备出库失败: {ex.Message}");
                }
            };
            var btnEdit = new AntdUI.Button
            {
                Text = "✏️ 编辑",
                Type = TTypeMini.Default,
                Dock = DockStyle.Fill,
                Margin = new Padding(2),
                Font = new Font("微软雅黑", 9)
            };
            btnEdit.Click += async (s, e) =>
            {
                if (currentDrawingDetail == null)
                {
                    AntdUI.Message.warn(this, "请先选择一张图纸！");
                    return;
                }

                var editForm = new DrawingPropertiesEditorForm(currentDrawingDetail);
                if (editForm.ShowDialog() == DialogResult.OK)
                {
                    var updatedDetail = editForm.UpdatedDrawingDetail;
                    await SaveDrawingDetailAsync(updatedDetail);
                }
            };

            var btnDelete = new AntdUI.Button
            {
                Text = "🗑️ 删除",
                Type = TTypeMini.Error,
                Dock = DockStyle.Fill,
                Margin = new Padding(2),
                Font = new Font("微软雅黑", 9)
            };
            btnDelete.Click += async (s, e) =>
            {
                // 检查是否选中图纸

                if (currentDrawingDetail == null)
                {
                    AntdUI.Message.warn(this, "请先选择一张图纸！");
                    return;
                }

                var confirm = MessageBox.Show($"确定要删除图纸“{currentDrawingDetail.Code}”吗？", "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (confirm != DialogResult.Yes) return;

                try
                {
                    var response = await httpClient.DeleteAsync($"api/drawings?id={currentDrawingDetail.Id}");
                    response.EnsureSuccessStatusCode();
                    var json = await response.Content.ReadAsStringAsync();

                    using (JsonDocument doc = JsonDocument.Parse(json))
                    {
                        var root = doc.RootElement;
                        if (root.GetProperty("code").GetInt32() == 200)
                        {
                            AntdUI.Message.success(this, "删除成功");
                            currentDrawingDetail = null;
                            propertyDataTable.Rows.Clear();
                            propertyDataTable.Rows.Add("提示", "请点击图纸查看详情");
                            propertyTable?.Refresh();
                            // 重新加载当前节点的图纸列表（刷新卡片区）
                            if (currentNode != null)
                            {
                                await RefreshCurrentNodeDrawings();
                            }
                        }
                        else
                        {
                            AntdUI.Message.error(this, $"删除失败：{root.GetProperty("message").GetString()}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    AntdUI.Message.error(this, $"删除失败：{ex.Message}");
                }
            };

            btnGrid.Controls.Add(btnIn, 0, 0);
            btnGrid.Controls.Add(btnOut, 1, 0);
            btnGrid.Controls.Add(btnEdit, 0, 1);
            btnGrid.Controls.Add(btnDelete, 1, 1);

            pnlRight.Controls.Add(propertyTable);
            pnlRight.Controls.Add(btnGrid);
            pnlRight.Controls.Add(lblRightTop);
            mainLayout.Controls.Add(pnlRight, 2, 0);

            treeItemMapping = new Dictionary<TreeItem, LibraryNode>();
            childToParentMap = new Dictionary<TreeItem, TreeItem>();
            productSelect.SelectedIndexChanged += ProductSelect_SelectedIndexChanged;
        }
        //定义监听
        private async Task<bool> SendCommandAndWaitAsync(Document doc, string executeString, string commandName)
        {
            return await CadHelper.SendCommandAndWaitAsync(doc, executeString, commandName);
        }
        //根据节点 ID 获取属性 JSON 字符串
        private async Task<string> GetDescPropForNode(long cabinetId)
        {
            try
            {
                var result = await CadHelper.GetDescPropForNode(httpClient, cabinetId);
                if (result == null)
                    AntdUI.Message.error(this, $"获取属性定义失败");
                return result;
            }
            catch (Exception ex)
            {
                AntdUI.Message.error(this, $"获取属性定义失败：{ex.Message}");
                return null;
            }
        }
        private async Task UploadDrawingsToBackend(long typeId)
        {
            try
            {
                string projectDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string outputDir = Path.Combine(projectDir, "ruku");

                if (!Directory.Exists(outputDir)) return;

                // 获取最新的 dwg, png 和 缩略图
                var files = new DirectoryInfo(outputDir).GetFiles().OrderByDescending(f => f.LastWriteTime).ToList();

                var dwgFile = files.FirstOrDefault(f => f.Extension.ToLower() == ".dwg");
                var pngFile = files.FirstOrDefault(f => f.Name.EndsWith(".png") && !f.Name.Contains("缩略图"));
                var thumbFile = files.FirstOrDefault(f => f.Name.Contains("缩略图"));

                if (dwgFile == null || pngFile == null || thumbFile == null)
                {
                    AntdUI.Message.error(this, "未找到生成的图纸文件，请重试入库命令。");
                    return;
                }
                AntdUI.Message.success(this, "已找到图纸文件");
                string descProp = await GetDescPropForNode(typeId);
                if (descProp == null) return; // 获取失败则终止上传
                AntdUI.Message.info(this, "属性："+descProp);
                using (var content = new MultipartFormDataContent())
                {
                    // 1. 添加参数 TypeId
                    content.Add(new StringContent(typeId.ToString()), "typeId");

                    // 2. 添加文件流 (注意：此处字段名需与后端接口一致)
                    var dwgContent = new StreamContent(dwgFile.OpenRead());
                    content.Add(dwgContent, "file", dwgFile.Name);

                    var previewContent = new StreamContent(pngFile.OpenRead());
                    content.Add(previewContent, "previewFile", pngFile.Name);

                    var thumbContent = new StreamContent(thumbFile.OpenRead());
                    content.Add(thumbContent, "thumbFile", thumbFile.Name);

                    content.Add(new StringContent(descProp, Encoding.UTF8), "descProp");

                    AntdUI.Message.info(this, "正在上传至服务器...");

                    var response = await httpClient.PostAsync("api/drawings/create", content);
                    var resultJson = await response.Content.ReadAsStringAsync();

                    using (JsonDocument doc = JsonDocument.Parse(resultJson))
                    {
                        var root = doc.RootElement;
                        // 【关键修复】：判断当前是否在后台线程，如果是，强制切回主 UI 线程执行界面刷新
                        if (this.InvokeRequired)
                        {
                            this.Invoke(new Action(async () =>
                            {
                                if (root.GetProperty("code").GetInt32() == 200)
                                {
                                    AntdUI.Message.success(this, "入库成功！数据库已同步。");
                                    await RefreshCurrentNodeDrawings();
                                }
                                else
                                {
                                    AntdUI.Message.error(this, $"上传失败：{root.GetProperty("message").GetString()}");
                                }
                            }));
                        }
                        else
                        {
                            if (root.GetProperty("code").GetInt32() == 200)
                            {
                                AntdUI.Message.success(this, "入库成功！数据库已同步。");
                                await RefreshCurrentNodeDrawings();
                            }
                            else
                            {
                                AntdUI.Message.error(this, $"上传失败：{root.GetProperty("message").GetString()}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AntdUI.Message.error(this, $"网络或系统错误：{ex.Message}");
            }
        }
        private async Task DownloadFileAsync(string url, string localPath)
        {
            await CadHelper.DownloadFileAsync(httpClient, url, localPath);
        }
        private void LeftTree_SelectChanged(object sender, TreeSelectEventArgs e)
        {
            if (e.Item != null)
                HandleTreeSelect(e.Item);
        }

        private async void ProductSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(productSelect.Text) || productData == null) return;

            currentProduct = productSelect.Text;
            if (modelToGroupMap.TryGetValue(currentProduct, out var group))
            {
                currentGroupName = group.GroupName;

                try
                {
                    leftTree.Enabled = false;
                    productSelect.Enabled = false;
                    AntdUI.Message.info(this, "正在加载图库结构...");

                    var treeNodes = await LoadTreeDataAsync(group.Id);

                    if (productData.ContainsKey(currentGroupName))
                        productData[currentGroupName] = treeNodes;
                    else
                        productData.Add(currentGroupName, treeNodes);

                    currentTreeItem = null;
                    leftTree.Items.Clear();
                    treeItemMapping.Clear();
                    childToParentMap.Clear();
                    flowPanel.Controls.Clear();
                    lblCurrentPath.Text = "未选择";
                    propertyDataTable.Rows.Clear();
                    propertyDataTable.Rows.Add("提示", "请点击图纸查看详情");
                    propertyTable?.Refresh();

                    currentNode = null;
                    currentAllDrawings = null;
                    currentFilteredDrawings = null;
                    // 重置筛选器
                    filterSelect.Items.Clear();
                    filterSelect.Items.Add(new SelectItem("全部", "all"));
                    filterSelect.SelectedIndex = 0;

                    var items = BuildTreeItems(treeNodes, null);
                    foreach (var item in items)
                        leftTree.Items.Add(item);

                    leftTree.Enabled = true;
                    productSelect.Enabled = true;
                    AntdUI.Message.success(this, "图库结构加载完成");
                }
                catch (Exception ex)
                {
                    AntdUI.Message.error(this, $"加载树结构失败：{ex.Message}");
                    leftTree.Enabled = true;
                    productSelect.Enabled = true;
                }
            }
            else
            {
                AntdUI.Message.warn(this, "未找到产品对应的组信息");
            }
        }

        private List<TreeItem> BuildTreeItems(List<LibraryNode> nodes, TreeItem parentItem)
        {
            var treeItems = new List<TreeItem>();
            foreach (var node in nodes)
            {
                var treeItem = new TreeItem { Text = node.Name, Tag = node, Expand = false };
                treeItemMapping[treeItem] = node;
                if (parentItem != null)
                    childToParentMap[treeItem] = parentItem;
                if (node.Children != null && node.Children.Count > 0)
                {
                    var childItems = BuildTreeItems(node.Children, treeItem);
                    foreach (var childItem in childItems)
                        treeItem.Sub.Add(childItem);
                }
                treeItems.Add(treeItem);
            }
            return treeItems;
        }

        private async void HandleTreeSelect(TreeItem item)
        {
            if (item == null) return;
            if (!treeItemMapping.TryGetValue(item, out var dataNode))
                dataNode = item.Tag as LibraryNode;
            if (dataNode == null) return;

            currentNode = dataNode;
            currentTreeItem = item;
            UpdatePathDisplay(item);

            if (dataNode.Children.Count > 0)
            {
                flowPanel.Controls.Clear();
                AntdUI.Message.info(this, $"已进入分类：{dataNode.Name}");
            }
            else
            {
                await LoadDrawingsForNode(dataNode);
            }
        }

        private void UpdatePathDisplay(TreeItem item)
        {
            var pathList = new List<string>();
            var current = item;
            while (current != null)
            {
                pathList.Insert(0, current.Text);
                childToParentMap.TryGetValue(current, out current);
            }
            lblCurrentPath.Text = string.Join(" > ", pathList);
        }

        private async void FilterSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (currentNode == null) return;
            string valScope = filterSelect.SelectedValue?.ToString();
            if (valScope == "all") valScope = "";
            await LoadFilteredDrawings(currentNode.Id, valScope);
        }

        private void LoadDrawingsCards(string[] drawings, string nodeName)
        {
            // 旧方法，保留空实现
        }
        private async Task RefreshCurrentNodeDrawings()
        {
            if (currentNode == null) return;
            // 重新加载图纸（会重置筛选器）
            await LoadDrawingsForNode(currentNode);
        }
    }
}