using System.Diagnostics;

namespace WpfCamera
{
    public static class FileExplorerHelper
    {
        public static void RevealInFileExplorer(string path)
        {
            string cmd = "explorer.exe";
            string arg = path;
            Process.Start(cmd, arg);
        }
    }
}
