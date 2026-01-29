using System.IO;
using Application = System.Windows.Application;

namespace DeposeGirls_AssetDecDL
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string Root = Environment.CurrentDirectory;
        public static string Respath = Path.Combine(App.Root, "Asset");
        public static int TotalCount = 0;
        public static int glocount = 0;
        public static string ServerURL_DMM = "https://dmm-sp.c4connect.co.jp/start.js";
        private static string ServerURL_Base = "https://fx-plat-fzsnweb.c4games.com/common/script/clientVersion";
        public static string ServerURL_Web = ServerURL_Base + "?gameId=15&platform=webdesk&branch=webgl-release";
        public static string ServerURL_Android = ServerURL_Base + "?platform=android&branch=NewGirls_HD2Releaselowdevice";
        public static string? BaseUrl = String.Empty;
        public static List<string> log = new List<string>();
    }
}
