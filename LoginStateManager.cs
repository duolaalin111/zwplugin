using System;
using ZwSoft.ZwCAD.ApplicationServices;

namespace ZrxDotNetCSProject5
{
    /// <summary>
    /// 全局登录状态管理器（单例模式）
    /// </summary>
    public static class LoginStateManager
    {
        private static bool _isLoggedIn = false;
        private static string _currentUser = string.Empty;

        /// <summary>
        /// 检查是否已登录
        /// </summary>
        public static bool IsLoggedIn => _isLoggedIn;

        /// <summary>
        /// 当前登录用户名
        /// </summary>
        public static string CurrentUser => _currentUser;

        /// <summary>
        /// 登录成功
        /// </summary>
        public static void Login(string username)
        {
            _isLoggedIn = true;
            _currentUser = username;
        }

        /// <summary>
        /// 退出登录
        /// </summary>
        public static void Logout()
        {
            _isLoggedIn = false;
            _currentUser = string.Empty;
        }

        /// <summary>
        /// 检查登录状态，未登录则提示
        /// </summary>
        /// <returns>已登录返回 true，未登录返回 false</returns>
        public static bool CheckLogin()
        {
            if (!_isLoggedIn)
            {
                Application.ShowAlertDialog("⚠️ 请先登录！\n\n请使用 LOGIN 命令进行登录。");
                return false;
            }
            return true;
        }
    }
}