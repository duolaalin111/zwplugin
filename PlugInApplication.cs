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
                // 获取主菜单组
                ZcadMenuGroups menuGroups = (ZcadMenuGroups)Application.MenuGroups;
                ZcadMenuGroup menuGroup = menuGroups.Item("ZWCAD");

                // === 创建菜单 ===
                ZcadPopupMenu menu = menuGroup.Menus.Add("图库管理");
                menu.AddMenuItem(menu.Count, "图库管理", "_libweb\n");

                // 插入到菜单栏末尾
                ZcadMenuBar menuBar = (ZcadMenuBar)Application.MenuBar;
                menu.InsertInMenuBar(menuBar.Count);

         
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
