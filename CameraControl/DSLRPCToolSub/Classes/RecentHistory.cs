using CameraControl.Core.Classes;
using CameraControl.Devices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CameraControl.DSLRPCToolSub.Classes
{
    public static class RecentHistory
    {
        public static string Last360 { 
            set 
            {
                try
                {
                    if(!File.Exists(Path.Combine(Application.LocalUserAppDataPath, "Recent.pref"))) { File.Create(Path.Combine(Application.LocalUserAppDataPath, "Recent.pref")); 
                    }
                    using (StreamWriter sw = File.AppendText(Path.Combine(Application.LocalUserAppDataPath, "Recent.pref")))
                    {
                        sw.WriteLine(value);
                    }
                }
                catch(Exception ex)
                {
                    Log.Debug("Write History Error: ",ex);
                }
            }
        }
        public static List<string> RecentFiles()
        {
            try
            {
                if(!File.Exists(Path.Combine(Application.LocalUserAppDataPath, "Recent.pref"))) { return null; }
                List<string> recentFiles = new List<string>();
                var lines = File.ReadAllLines(Path.Combine(Application.LocalUserAppDataPath, "Recent.pref")).Reverse();
                int i = 0;
                foreach (string line in lines)
                {
                    if (i == 10) { break; }
                    if (Directory.Exists(line))
                    {
                        recentFiles.Add(line);
                        i++;
                    }
                }
                if (i != 0) { return recentFiles; }
                else { return null; }
            }catch(Exception ex)
            {
                Log.Debug("Read History Exception: ", ex);
            }
            return null;
        }
        public static string getFirstFile(string _path)
        {
            string root = null;
            try
            {
                if (_path == "")
                {
                    root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "OrangeMonkie", "History");
                    if (!Directory.Exists(root)) { Directory.CreateDirectory(root); }
                }
                else { root = _path; }
                string[] supportedExtensions = new[] { ".bmp", ".jpeg", ".jpg", ".png", ".tiff" };
                var files = Directory.GetFiles(root, "*.*").Where(s => supportedExtensions.Contains(System.IO.Path.GetExtension(s).ToLower()));
                foreach (var f in files)
                {
                    root = f;
                    break;
                }
                return root;
            }
            catch (Exception exception)
            {
                Log.Error("Error set My pictures folder", exception);
                root = "C:\\";
            }
            return root;
        }
    }
}
