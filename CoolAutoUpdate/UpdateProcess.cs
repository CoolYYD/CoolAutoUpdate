using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UIH.Update
{
    public class UpdateProcess
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(UpdateProcess));
        private static string packagePath = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName;

        public static string bakDirPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Cst.BakPackageName);
        public static string updatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Cst.PackageName);

        private static List<string> desPathList = new List<string>();

        private static int rpcCount = 0;
        private static int bakCount = 0;

        public static bool Update()
        {

            try
            {
                desPathList.Clear();
                //Save Path
                GreateDesUpdatePath(updatePath);

                //backUp
                bakCount = 0;
                BakFile();
                Logger.Info("BakFile count:" + bakCount);

                //stop.exe
                Utils.RunCmd(1);

                //Replace
                rpcCount = 0;
                ReplaceFile();
                Logger.Info("ReplaceFile count:" + rpcCount);

                //start .exe
                Utils.RunCmd(0);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Update err:" + ex);
                return false;
            }
            finally
            {
                BarControl.SetBarStep(BarStep.Replace);
            }
        }


        public static bool RollBack()
        {
            try
            {
                Logger.Info("Start roll back process");
                //HandleEx需要重新获取updatePath
                if (desPathList.Count == 0)
                {
                    GreateDesUpdatePath(updatePath);
                }

                Utils.RunCmd(1);

                foreach (string fileName in desPathList)
                {
                    string fullName = bakDirPath + fileName.Substring(packagePath.Length);
                 
                    if (File.Exists(fullName))
                    {
                        File.Copy(fullName, fileName, true);
                    }
                    else
                    {                      
                        string bakPath = Path.Combine(fullName, @"..\");
                        if (Directory.Exists(bakPath))
                        {
                            File.Delete(fileName);
                        }
                        else
                        {
                            string desPath = Path.Combine(fileName, @"..\");
                            if(Directory.Exists(desPath))
                            {
                                Directory.Delete(desPath, true);
                            }                                             
                        }
                                       
                    }                                 
                }
                Utils.RunCmd(0);
                Logger.Info("Roll back program success");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("RollBack err:" + ex);
                return false;
            }
        }

        private static void ReplaceFile()
        {
            foreach (string fileName in desPathList)
            {
                string fullName = updatePath + fileName.Substring(packagePath.Length);

                if (File.Exists(fileName))
                {
                    File.Copy(fullName, fileName, true);
                    rpcCount++;
                }
                else
                {
                    string dirPath = Path.Combine(fileName, @"..\");
                    if (!Directory.Exists(dirPath))
                    {
                        Directory.CreateDirectory(dirPath);
                    }
                    File.Copy(fullName, fileName);
                    rpcCount++;
                }
            }            
        }
        private static void BakFile()
        {
            foreach (string fileName in desPathList)
            {
                if (File.Exists(fileName))
                {
                    string fullName = bakDirPath + fileName.Substring(packagePath.Length);
                    string path = Path.Combine(fullName, @"..\");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    File.Copy(fileName, fullName, true);
                    bakCount++;
                }
            }
        }

        private static void GreateDesUpdatePath(string updatePath)
        {
            DirectoryInfo d = new DirectoryInfo(updatePath);
            FileInfo[] files = d.GetFiles();//文件
            DirectoryInfo[] directs = d.GetDirectories();//文件夹
            foreach (FileInfo f in files)
            {
                int index = updatePath.LastIndexOf(Cst.PackageName);
                string NamePath = packagePath+ updatePath.Substring(index + Cst.PackageName.Length) + "\\" + f.Name;
                desPathList.Add(NamePath);
            }
            //获取子文件夹内的文件列表，递归遍历  
            foreach (DirectoryInfo dd in directs)
            {
                GreateDesUpdatePath(dd.FullName);
            }
        }
    }
}
