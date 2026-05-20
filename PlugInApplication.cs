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
using ZWCAD;

[assembly: ExtensionApplication(typeof(ZrxDotNetCSProject5.PlugInApplication))]

namespace ZrxDotNetCSProject5
{
    public class PlugInApplication : IExtensionApplication
    {
        public void Initialize()
        {
            try
            {
                // 获取主菜单组（无需 GetActiveObject，因为 Application 是 .NET API 的全局对象）
                ZcadMenuGroups menuGroups = (ZcadMenuGroups)Application.MenuGroups;
                ZcadMenuGroup menuGroup = menuGroups.Item("ZWCAD");

                // === 创建主菜单 "SmartDesign" ===
                ZcadPopupMenu smartDesignMenu = menuGroup.Menus.Add("测试插件");
                smartDesignMenu.AddMenuItem(smartDesignMenu.Count, "登录", "_login\n");
                smartDesignMenu.AddMenuItem(smartDesignMenu.Count, "图库管理", "_libmgr\n");
                smartDesignMenu.AddMenuItem(smartDesignMenu.Count, "图库管理网页", "_libweb\n");
                smartDesignMenu.AddMenuItem(smartDesignMenu.Count, "项目管理", "_projmgr\n");
                // --- 新加这一行，执行刚才定义的 _projmgr_ui 命令 ---
                smartDesignMenu.AddMenuItem(smartDesignMenu.Count, "项目管理(新UI)", "_projmgrui\n");
                smartDesignMenu.AddMenuItem(smartDesignMenu.Count, "项目管理(新UI移植版)", "_qqqprojmgrui\n");
                smartDesignMenu.AddMenuItem(smartDesignMenu.Count, "非标图纸标准化", "_stdnons\n");
                smartDesignMenu.AddMenuItem(smartDesignMenu.Count, "生成回路标签", "_genloop\n");
                smartDesignMenu.AddMenuItem(smartDesignMenu.Count, "设置", "_settings\n");
                smartDesignMenu.AddMenuItem(smartDesignMenu.Count, "退出登录", "_logout\n");

                // 插入到菜单栏末尾
                ZcadMenuBar menuBar = (ZcadMenuBar)Application.MenuBar;
                smartDesignMenu.InsertInMenuBar(menuBar.Count);

         
            }
            catch (System.Exception ex)
            {
                Application.ShowAlertDialog("Plugin initialization failed:\n" + ex.Message);
            }
        }

        public void Terminate()
        {
            // 可以在这里清理资源，但通常不需要
        }
    }
}
