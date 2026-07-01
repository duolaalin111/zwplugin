using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZwSoft.ZwCAD.Runtime;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.Geometry;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.EditorInput;
using System.IO;
using ZwSoft.ZwCAD.PlottingServices;
using PlotType = ZwSoft.ZwCAD.DatabaseServices.PlotType; // CommandMethod, CommandClass
using ZwSoft.ZwCAD.Colors;
using System.Windows.Forms;
[assembly: CommandClass(typeof(ZrxDotNetCSProject5.Commands))]

namespace ZrxDotNetCSProject5
{
    class Commands
    {
        [CommandMethod("ZWCAD_入库1", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void ImportToLibrary1()
        {
            Document doc = ZwSoft.ZwCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            // ====================== 新增：每次先清空 ruku 文件夹 ======================
            string projectDir = Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location);
            string outputDir = Path.Combine(projectDir, "ruku");

            try
            {
                if (Directory.Exists(outputDir))
                {
                    // 清空文件夹里所有文件（不删除文件夹）
                    foreach (string file in Directory.GetFiles(outputDir))
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch { /* 忽略被占用等极少数情况 */ }
                    }
                }
                else
                {
                    Directory.CreateDirectory(outputDir);
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n清空 ruku 文件夹失败: {ex.Message}");
                return;   // 清空失败就不要继续了
            }
            // =====================================================================

            // 1. 选择对象（支持预选 + 框选）
            PromptSelectionOptions pso = new PromptSelectionOptions();
            pso.MessageForAdding = "\n请框选要入库的内容（自动结束）:";
            PromptSelectionResult psr = ed.GetSelection(pso);

            if (psr.Status != PromptStatus.OK || psr.Value.Count == 0)
            {
                ed.WriteMessage("\n未选择任何对象，入库取消。");
                return;
            }

            // 2. 弹出自定义命名输入框
            using (InputNameForm nameForm = new InputNameForm())
            {
                if (nameForm.ShowDialog() == DialogResult.OK)
                {
                    string customName = nameForm.InputName;
                    if (string.IsNullOrEmpty(customName))
                    {
                        ed.WriteMessage("\n名称不能为空，入库取消。");
                        return;
                    }

                    // 继续执行导出操作
                    // 3. 创建输出目录
                    if (!Directory.Exists(outputDir))
                        Directory.CreateDirectory(outputDir);

                    // 4. 生成文件名（以用户输入的名称为基础）
                    string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    string safeName = MakeValidFileName(customName);   // 去掉非法字符

                    string dwgPath = Path.Combine(outputDir, $"{safeName}_{timestamp}.dwg");
                    string pngPath = Path.Combine(outputDir, $"{safeName}_{timestamp}.png");
                    string thumbPath = Path.Combine(outputDir, $"{safeName}_{timestamp}_缩略图.png");   // 缩略图

                    try
                    {
                        // === 先计算包围盒，取中心作为基点 ===
                        Extents3d? ext = GetSelectionExtents(psr.Value, doc);
                        if (!ext.HasValue)
                        {
                            ed.WriteMessage("\n无法计算选区范围，入库取消。");
                            return;
                        }

                        Point3d center = new Point3d(
                            (ext.Value.MinPoint.X + ext.Value.MaxPoint.X) / 2,
                            (ext.Value.MinPoint.Y + ext.Value.MaxPoint.Y) / 2, 0);

                        // === 导出 DWG（以选取中心为基点）===
                        ExportSelectionToDWG(psr.Value, dwgPath, doc, center);

                        // === 导出 PNG ===
                        ExportExtentsToPng(doc, ext.Value, pngPath);
                        ExportExtentsToPng(doc, ext.Value, thumbPath);


                        ed.WriteMessage($"\n入库文件已生成：");
                        ed.WriteMessage($"   名称   → {customName}");
                        ed.WriteMessage($"   DWG    → {dwgPath}");
                        ed.WriteMessage($"   PNG    → {pngPath}");
                        ed.WriteMessage($"   缩略图 → {thumbPath}");

                        // TODO: 后面在这里把三个文件路径传给后端数据库
                        // UploadToBackend(dwgPath, pngPath, thumbPath, customName);
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage($"\n入库失败: {ex.Message}");
                    }
                }
                else
                {
                    ed.WriteMessage("\n入库操作已取消。");
                }
            }
        }

        [CommandMethod("ZWCAD_入库", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void ImportToLibrary()
        {
            Document doc = ZwSoft.ZwCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            // 1. 获取 projectDir 和 outputDir
            string projectDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string outputDir = Path.Combine(projectDir, "ruku");

            try
            {
                // 2. 准备输出目录
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }
                else
                {
                    // 清理旧文件
                    foreach (string file in Directory.GetFiles(outputDir))
                    {
                        try { File.Delete(file); } catch { }
                    }
                }

                // 3. 选择对象
                PromptSelectionOptions pso = new PromptSelectionOptions();
                pso.MessageForAdding = "\n请框选要入库的内容:";
                PromptSelectionResult psr = ed.GetSelection(pso);

                if (psr.Status != PromptStatus.OK || psr.Value.Count == 0)
                {
                    ed.WriteMessage("\n未选择对象，入库取消。");
                    return;
                }

                // 4. 生成以当前时间命名的文件名
                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                string baseFileName = $"入库_{timestamp}";

                string dwgPath = Path.Combine(outputDir, $"{baseFileName}.dwg");
                string pngPath = Path.Combine(outputDir, $"{baseFileName}.png");
                string thumbPath = Path.Combine(outputDir, $"{baseFileName}_缩略图.png");

                // 计算包围盒，取中心作为WBLOCK基点
                Extents3d? ext = GetSelectionExtents(psr.Value, doc);
                if (!ext.HasValue)
                {
                    ed.WriteMessage("\n无法计算选区范围，入库取消。");
                    return;
                }

                Point3d center = new Point3d((ext.Value.MinPoint.X + ext.Value.MaxPoint.X) / 2,
                                              (ext.Value.MinPoint.Y + ext.Value.MaxPoint.Y) / 2, 0);

                // 导出 DWG（以选取中心为基点，居中显示）
                ExportSelectionToDWG(psr.Value, dwgPath, doc, center);
                ed.WriteMessage($"\n[成功] DWG 已生成：{baseFileName}.dwg");

                // 导出 PNG 预览图和缩略图
                ExportExtentsToPng(doc, ext.Value, pngPath);
                ExportExtentsToPng(doc, ext.Value, thumbPath);
                ed.WriteMessage($"\n[成功] 预览图已生成：{baseFileName}.png");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n[错误] 入库处理失败: {ex.Message}");
            }
        }
        // ==================== 辅助方法 ====================

        // 把用户输入的名称转成合法文件名（去掉 \ / : * ? " < > | 等非法字符）
        private string MakeValidFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c.ToString(), "_");
            }
            return name.Trim();
        }
        private static void ExportToPNGWithPlot(SelectionSet ss, string pngPath, Document doc)
        {
            // 使用 PlotSettings + Plot 导出 PNG（ZWCAD 支持类似 AutoCAD 的 Plot API）
            // 或者最简单：调用 PNGOUT + 预选
            doc.Editor.WriteMessage("\n正在导出 PNG...");

            // 推荐直接 SendCommand（PNGOUT 支持选中对象）
            string cmd = $"_PNGOUT\n{pngPath}\n";
            doc.SendStringToExecute(cmd, true, false, false);
            // 用户会看到选择对象提示，此时可以再 SendStringToExecute("P\n") 或提前用 PickFirst
        }
        // 计算选中对象的包围盒（从你原来的代码提取出来）
        private Extents3d? GetSelectionExtents(SelectionSet ss, Document doc)
        {
            Database db = doc.Database;
            Extents3d? ext = null;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject so in ss)
                {
                    Entity ent = tr.GetObject(so.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    try
                    {
                        Extents3d e = ent.GeometricExtents;
                        if (ext == null)
                            ext = e;
                        else
                        {
                            ext = new Extents3d(
                                new Point3d(Math.Min(ext.Value.MinPoint.X, e.MinPoint.X),
                                            Math.Min(ext.Value.MinPoint.Y, e.MinPoint.Y), 0),
                                new Point3d(Math.Max(ext.Value.MaxPoint.X, e.MaxPoint.X),
                                            Math.Max(ext.Value.MaxPoint.Y, e.MaxPoint.Y), 0));
                        }
                    }
                    catch { }
                }
            }
            return ext;
        }

        private void ExportSelectionToDWG(SelectionSet ss, string filePath, Document doc, Point3d basePoint)
        {
            if (ss == null || ss.Count == 0) return;

            Database db = doc.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ObjectIdCollection objIds = new ObjectIdCollection();
                foreach (ObjectId id in ss.GetObjectIds())
                    objIds.Add(id);

                using (Database newDb = new Database(true, false))
                {
                    // 以选取中心为基点WBLOCK，确保DWGu查看器中居中显示
                    db.Wblock(newDb, objIds, basePoint, DuplicateRecordCloning.Ignore);

                    // 设置导出DWG的单位为毫米
                    newDb.Insunits = UnitsValue.Millimeters;

                    newDb.SaveAs(filePath, DwgVersion.AC1024);   // AutoCAD 2010 格式，兼容 DWG 查看器
                }

                tr.Commit();
            }

            doc.Editor.WriteMessage($"\nDWG 导出完成：{filePath}");
        }

        private void ExportExtentsToPng(Document doc, Extents3d ext, string outputPath)
        {
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayoutManager lm = LayoutManager.Current;
                ObjectId layoutId = lm.GetLayoutId(lm.CurrentLayout);
                Layout layout = (Layout)tr.GetObject(layoutId, OpenMode.ForRead);

                PlotInfo pi = new PlotInfo();
                pi.Layout = layoutId;

                PlotSettings ps = new PlotSettings(layout.ModelType);
                ps.CopyFrom(layout);

                PlotSettingsValidator psv = PlotSettingsValidator.Current;

                // 中望CAD 常用 PNG 配置是 "DWG To PNG.pc5"（或 "ZWPLOT-PNG.pc5"，视版本而定）
                // 如果你的 ZWCAD 安装目录有 "DWG To PNG.pc5"，就用这个；否则用系统自带的虚拟打印机名
                string plotDeviceName = "ZWPLOT_PNG.pc5";
                string mediaName = null;

                try
                {
                    psv.SetPlotConfigurationName(ps, plotDeviceName, mediaName);
                    psv.RefreshLists(ps);
                }
                catch
                {
                    // 备用打印机名
                    try
                    {
                        plotDeviceName = "DWG To PNG.pc5";
                        psv.SetPlotConfigurationName(ps, plotDeviceName, null);
                        psv.RefreshLists(ps);
                    }
                    catch (System.Exception ex2)
                    {
                        MessageBox.Show($"PNG打印机配置失败，请检查ZWCAD是否安装了PNG打印机\n\n错误: {ex2.Message}",
                            "预览图生成失败");
                        return;
                    }
                }

                // 设置打印类型为 Window（窗口范围）
                psv.SetPlotType(ps, ZwSoft.ZwCAD.DatabaseServices.PlotType.Window);

                // 设置窗口范围
                psv.SetPlotWindowArea(ps,
                    new Extents2d(
                        new Point2d(ext.MinPoint.X, ext.MinPoint.Y),
                        new Point2d(ext.MaxPoint.X, ext.MaxPoint.Y)));

                // 比例自适应（Scale to Fit）
                psv.SetUseStandardScale(ps, true);
                psv.SetStdScaleType(ps, StdScaleType.ScaleToFit);

                // 居中
                psv.SetPlotCentered(ps, true);

                pi.OverrideSettings = ps;

                PlotInfoValidator piv = new PlotInfoValidator();
                piv.MediaMatchingPolicy = MatchingPolicy.MatchEnabled;
                piv.Validate(pi);

                try
                {
                    using (PlotEngine pe = PlotFactory.CreatePublishEngine())
                    {
                        using (PlotProgressDialog ppd = new PlotProgressDialog(false, 1, true))
                        {
                            ppd.OnBeginPlot();
                            ppd.IsVisible = false;

                            pe.BeginPlot(ppd, null);
                            pe.BeginDocument(pi, doc.Name, null, 1, true, outputPath);

                            PlotPageInfo ppi = new PlotPageInfo();
                            pe.BeginPage(ppi, pi, true, null);
                            pe.BeginGenerateGraphics(null);
                            pe.EndGenerateGraphics(null);
                            pe.EndPage(null);
                            pe.EndDocument(null);
                            pe.EndPlot(null);

                            ppd.OnEndPlot();
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"PNG打印失败：\n\n错误: {ex.Message}\n打印机: {plotDeviceName}",
                        "预览图生成失败");
                    tr.Commit();
                    return;
                }

                tr.Commit();
            }
        }

        [CommandMethod("ZWCAD_出库", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void ExportFromLibrary()
        {
            Document doc = ZwSoft.ZwCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;
            //此方法前已从数据库拿到了url下载了文件到文件夹内
            // ====================== 1. 准备 chuku 文件夹 ======================
            string projectDir = Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location);
            string chukuDir = Path.Combine(projectDir, "chuku");

            // 已经有一个 dwg 文件在 chuku 文件夹里，我们找它
            string[] dwgFiles = Directory.GetFiles(chukuDir, "*.dwg");
            if (dwgFiles.Length == 0)
            {
                ed.WriteMessage("\nchuku 文件夹中没有找到 DWG 文件，出库取消。");
                return;
            }

            string dwgPath = dwgFiles[0];   // 目前取第一个（后面可以改成按名称规则取最新或指定）
            ed.WriteMessage($"\n找到出库 DWG 文件：{Path.GetFileName(dwgPath)}");

            // ====================== 3. 把 DWG 内容插入到当前图纸 ======================
            try
            {
                 InsertDwgAsBlock(doc, dwgPath);
                ed.WriteMessage("\nDWG 内容已成功插入到当前图纸！");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n插入 DWG 失败: {ex.Message}");
            }
        }
        private void InsertDwgAsBlock(Document doc, string dwgPath)
        {
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // 1. 打开当前图的块表
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

                // 2. 生成一个唯一块名（避免重复）
                string blockName = Path.GetFileNameWithoutExtension(dwgPath);

                if (bt.Has(blockName))
                {
                    blockName = blockName + "_" + DateTime.Now.ToString("HHmmss");
                }

                // 3. 创建一个临时数据库，读取外部DWG
                using (Database sourceDb = new Database(false, true))
                {
                    sourceDb.ReadDwgFile(dwgPath, FileOpenMode.OpenForReadAndAllShare, true, "");

                    // 4. 插入为块定义
                    db.Insert(blockName, sourceDb, false);
                }

                // 5. 在当前空间插入块参照
                bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord space =
                    (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                BlockReference br = new BlockReference(Point3d.Origin, bt[blockName]);

                space.AppendEntity(br);
                tr.AddNewlyCreatedDBObject(br, true);

                tr.Commit();
            }
        }

        [CommandMethod("ZWCAD_出库1", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void ExportFromLibrary1()
        {
            Document doc = ZwSoft.ZwCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            // ====================== 1. 准备 chuku 文件夹 ======================
            string projectDir = Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location);
            string chukuDir = Path.Combine(projectDir, "chuku");

            // 检查并获取 dwg 文件
            string[] dwgFiles = Directory.GetFiles(chukuDir, "*.dwg");
            if (dwgFiles.Length == 0)
            {
                ed.WriteMessage("\nchuku 文件夹中没有找到 DWG 文件，出库取消。");
                return;
            }

            string dwgPath = dwgFiles[0];
            ed.WriteMessage($"\n找到出库 DWG 文件：{Path.GetFileName(dwgPath)}");

            // ====================== 2. 提示用户选择插入点 ======================
            PromptPointOptions ppo = new PromptPointOptions("\n请在图纸中点击选择图块插入点: ");
            PromptPointResult ppr = ed.GetPoint(ppo);

            // 检查用户是否按了 ESC 键取消
            if (ppr.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\n已取消插入操作。");
                return;
            }

            // 获取用户鼠标点击的真实坐标
            Point3d insertPoint = ppr.Value;

            // ====================== 3. 把 DWG 内容插入到当前图纸 ======================
            try
            {
                // 将用户的坐标传给插入方法
                InsertDwgAsBlock1(doc, dwgPath, insertPoint);
                ed.WriteMessage("\nDWG 内容已成功插入到当前图纸！");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n插入 DWG 失败: {ex.Message}");
            }
        }
        // 注意参数列表多了一个 Point3d position
        private void InsertDwgAsBlock1(Document doc, string dwgPath, Point3d position)
        {
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

                string blockName = Path.GetFileNameWithoutExtension(dwgPath);

                if (bt.Has(blockName))
                {
                    blockName = blockName + "_" + DateTime.Now.ToString("HHmmss");
                }

                using (Database sourceDb = new Database(false, true))
                {
                    sourceDb.ReadDwgFile(dwgPath, FileOpenMode.OpenForReadAndAllShare, true, "");

                    // 保持单位统一，不做自动缩放
                    db.Insert(blockName, sourceDb, false);
                }

                bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

                BlockTableRecord space =
                    (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                BlockReference br = new BlockReference(position, bt[blockName]);
                space.AppendEntity(br);
                tr.AddNewlyCreatedDBObject(br, true);

                tr.Commit();
            }
        }
        [CommandMethod("ZWCAD_出库1_Explode", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void ExportFromLibrary1_Explode()
        {
            Document doc = ZwSoft.ZwCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            // 1. 准备 chuku 文件夹
            string projectDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string chukuDir = Path.Combine(projectDir, "chuku");

            string[] dwgFiles = Directory.GetFiles(chukuDir, "*.dwg");
            if (dwgFiles.Length == 0)
            {
                ed.WriteMessage("\nchuku 文件夹中没有找到 DWG 文件，出库取消。");
                return;
            }

            string dwgPath = dwgFiles[0];
            ed.WriteMessage($"\n找到出库 DWG 文件：{Path.GetFileName(dwgPath)}");

            // 2. 选择插入点
            PromptPointOptions ppo = new PromptPointOptions("\n请在图纸中点击选择图块插入点: ");
            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\n已取消插入操作。");
                return;
            }

            Point3d insertPoint = ppr.Value;

            // 3. 插入 DWG（炸开模式）
            try
            {
                InsertDwgAsBlockAndExplode(doc, dwgPath, insertPoint);
                ed.WriteMessage("\nDWG 内容已成功插入并炸开！");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n插入 DWG 失败: {ex.Message}");
            }
        }
        private void InsertDwgAsBlockAndExplode(Document doc, string dwgPath, Point3d position)
        {
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // 1. 获取块表
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

                // 2. 生成唯一块名
                string blockName = Path.GetFileNameWithoutExtension(dwgPath);
                if (bt.Has(blockName))
                    blockName = blockName + "_" + DateTime.Now.ToString("HHmmss");

                // 3. 读取外部 DWG 并插入为块定义
                using (Database sourceDb = new Database(false, true))
                {
                    sourceDb.ReadDwgFile(dwgPath, FileOpenMode.OpenForReadAndAllShare, true, "");
                    db.Insert(blockName, sourceDb, false);
                }

                // 4. 在当前空间插入块参照
                bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                BlockReference br = new BlockReference(position, bt[blockName]);
                space.AppendEntity(br);
                tr.AddNewlyCreatedDBObject(br, true);

                // 5. 炸开块参照
                using (DBObjectCollection exploded = new DBObjectCollection())
                {
                    br.Explode(exploded);
                    foreach (Entity ent in exploded)
                    {
                        space.AppendEntity(ent);
                        tr.AddNewlyCreatedDBObject(ent, true);
                    }
                }

                // 6. 删除原块参照（若需要，可选）
                br.Erase();

                tr.Commit();
            }
        }


        [CommandMethod("HelloCS")]
        static public void HelloCS()
        {
            Database db = HostApplicationServices.WorkingDatabase;
            Document doc = ZwSoft.ZwCAD.ApplicationServices.Application.DocumentManager.GetDocument(db);
            ZwSoft.ZwCAD.EditorInput.Editor ed = doc.Editor;

            ed.WriteMessage("\nHello World!");
        }
        //    [CommandMethod("LOGIN")]
        //    public void LoginCommand()
        //    {
        //        //Login login = new Login();
        //        loginAntdUI login= new loginAntdUI();
        //        Application.ShowModelessDialog(login);
        //    }

        //    [CommandMethod("LIBMGR")]
        //    public void LibraryManagementCommand()
        //    {
        //        //LibraryManage libraryManage = new LibraryManage();
        //        LibraryManageAntdUI libraryManage = new LibraryManageAntdUI();
        //        Application.ShowModelessDialog(libraryManage);
        //    }

        //    [CommandMethod("PROJMGR")]
        //    public void ProjectManagementCommand()
        //    {
        //        ProjectManagement projectManagement = new ProjectManagement();
        //        Application.ShowModelessDialog(projectManagement);
        //    }

        //    [CommandMethod("STDNONS")]
        //    public void NonStandardDrawingStandardizationCommand()
        //    {
        //        Form1 form1 = new Form1();
        //        Application.ShowModelessDialog(form1);
        //    }

        //    [CommandMethod("GENLOOP")]
        //    public void GenerateLoopTagsCommand()
        //    {
        //        GenerateLoopTags generateLoopTags = new GenerateLoopTags();
        //        Application.ShowModelessDialog(generateLoopTags);
        //    }

        //    [CommandMethod("SETTINGS")]
        //    public void SettingsCommand()
        //    {
        //        Settings settings = new Settings();
        //        Application.ShowModelessDialog(settings);
        //    }

        //    [CommandMethod("LOGOUT")]
        //    public void LogoutCommand()
        //    {
        //        Application.ShowAlertDialog("Logout clicked!");
        //    }
        [CommandMethod("LOGIN")]
        public void LoginCommand()
        {
            // 如果已经登录，提示是否重新登录
            if (LoginStateManager.IsLoggedIn)
            {
                var result = System.Windows.Forms.MessageBox.Show(
                    $"⚠️ 当前已登录用户：{LoginStateManager.CurrentUser}\n\n确定要重新登录吗？",
                    "确认重新登录",
                    System.Windows.Forms.MessageBoxButtons.YesNo,
                    System.Windows.Forms.MessageBoxIcon.Warning);

                if (result != System.Windows.Forms.DialogResult.Yes)
                    return;

                // 退出当前登录
                LoginStateManager.Logout();
            }

            loginAntdUI login = new loginAntdUI();
            ZwSoft.ZwCAD.ApplicationServices.Application.ShowModelessDialog(login);
        }

        [CommandMethod("LIBMGR")]
        public void LibraryManagementCommand()
        {
            // ✅ 添加登录验证
            if (!LoginStateManager.CheckLogin()) return;

            LibraryManageAntdUI libraryManage = new LibraryManageAntdUI();
            ZwSoft.ZwCAD.ApplicationServices.Application.ShowModelessDialog(libraryManage);
        }

        [CommandMethod("PROJMGR")]
        public void ProjectManagementCommand()
        {
            // ✅ 添加登录验证
            if (!LoginStateManager.CheckLogin()) return;

            ProjectManagement projectManagement = new ProjectManagement();
            ZwSoft.ZwCAD.ApplicationServices.Application.ShowModelessDialog(projectManagement);
        }

        [CommandMethod("PROJMGRUI")]
        public void ProjectOverviewFormCommand()
        {
            // ✅ 添加登录验证
            if (!LoginStateManager.CheckLogin()) return;
            ProjectOverviewForm projectOverviewForm = new ProjectOverviewForm();
            ZwSoft.ZwCAD.ApplicationServices.Application.ShowModelessDialog(projectOverviewForm);
        }

        [CommandMethod("QQQPROJMGRUI")]
        public void Form1tryCommand()
        {
            // ✅ 添加登录验证
            if (!LoginStateManager.CheckLogin()) return;
            Form1try form1Try = new Form1try();
            ZwSoft.ZwCAD.ApplicationServices.Application.ShowModelessDialog(form1Try);
        }

        [CommandMethod("STDNONS")]
        public void NonStandardDrawingStandardizationCommand()
        {
            // ✅ 添加登录验证
            if (!LoginStateManager.CheckLogin()) return;

            Form1 form1 = new Form1();
            ZwSoft.ZwCAD.ApplicationServices.Application.ShowModelessDialog(form1);
        }

        [CommandMethod("LIBWEB")]
        public void LibWebCommand()
        {
            LibraryManageWeb libraryManageWeb = new LibraryManageWeb();
            ZwSoft.ZwCAD.ApplicationServices.Application.ShowModelessDialog(libraryManageWeb);
        }

        [CommandMethod("GENLOOP")]
        public void GenerateLoopTagsCommand()
        {
            // ✅ 添加登录验证
            if (!LoginStateManager.CheckLogin()) return;

            GenerateLoopTags generateLoopTags = new GenerateLoopTags();
            ZwSoft.ZwCAD.ApplicationServices.Application.ShowModelessDialog(generateLoopTags);
        }

        [CommandMethod("SETTINGS")]
        public void SettingsCommand()
        {
            // ✅ 添加登录验证
            if (!LoginStateManager.CheckLogin()) return;

            Settings settings = new Settings();
            ZwSoft.ZwCAD.ApplicationServices.Application.ShowModelessDialog(settings);
        }

        [CommandMethod("LOGOUT")]
        public void LogoutCommand()
        {
            if (LoginStateManager.IsLoggedIn)
            {
                string currentUser = LoginStateManager.CurrentUser;
                LoginStateManager.Logout();
                ZwSoft.ZwCAD.ApplicationServices.Application.ShowAlertDialog($"✅ 用户 {currentUser} 已退出登录！");
            }
            else
            {
                ZwSoft.ZwCAD.ApplicationServices.Application.ShowAlertDialog("ℹ️ 当前未登录，无需退出。");
            }
        }
    }

}
