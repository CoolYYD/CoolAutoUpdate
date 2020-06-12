using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace UIH.Update
{
    static class Program
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Program));

        private static string curVersion = "";
        private static int timeSpan = int.Parse(ConfigurationManager.AppSettings["timeSpan"].ToString());

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //判断升级过程是否被中断
            bool isRun = HandleProcException();

            if (isRun)
            {
                Thread.Sleep(timeSpan * 60 * 1000);
            }

            //定时检测
            Task.Factory.StartNew(RunUpdateProcByTimeSpan);

            while (true)
            {
                Thread.Sleep(1000000000);
            }
        }


        private static string basePath = AppDomain.CurrentDomain.BaseDirectory;
        private static string downloadPackagePath = "";
        private static UpdateConfig config;

        public static void RunUpdateProcByTimeSpan() {
            while (true)
            {
                Logger.Info("Start check update server version");
                BarControl.InitAllParam();
                if (CheckUpdate())
                {
                    if (Utils.ExtractFile(downloadPackagePath, basePath))
                    {
                        Logger.Info("ExtractFile success");
                        RunUpateProc();
                    }
                    else
                    {
                        Logger.Info("Extract file failed");
                        ClearAllPackage();
                        //解压文件失败
                        BarControl.IsEndFail = true;
                    }
                }
                else
                {
                    BarControl.IsEndFail = true;
                }
                BarControl.SetAllARE();
                Thread.Sleep(timeSpan * 60 * 1000);
            }
        }

        private static string updateZipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Cst.PackageName + ".zip");
        /// <summary>
        /// 处理上次可能失败的升级流程
        /// </summary>
        /// <returns>是否执行异常处理</returns>
        private static bool HandleProcException()
        {
            bool res = false;
            try
            {
                BarControl.InitAllParam();
                if (Utils.SetServerUrl())
                {
                    BarControl.IsHandleEx = true;
                    if (File.Exists(updateZipPath))
                    {
                        config = Utils.GetUpdateConfig();
                        Logger.Info("Has UpdatePackage.zip,continue operate");
                        string zipMD5 = Utils.GetMD5Value(updateZipPath);
                        if (zipMD5 == config.MD5)
                        {
                            res = true;
                            Task.Factory.StartNew(() =>
                            {
                                Application.Run(new Form1());
                            });
                            if (Utils.ExtractFile(updateZipPath, basePath))
                            {
                                Logger.Info("ExtractFile success");
                                RunUpateProc();
                            }
                            else
                            {
                                Logger.Info("Extract file failed");
                                ClearAllPackage();
                                //解压文件失败
                                BarControl.IsEndFail = true;
                            }
                        }
                        else
                        {
                            Logger.Warn("MD5 is defferent,delete zip file");
                            Utils.DeleteFile(updateZipPath);
                            ClearAllPackage();
                        }
                    }
                }
                else
                {
                    Logger.Info("UpdateUrl is null");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("HandleProcException err:" + ex);
            }
            finally
            {
                BarControl.SetAllARE();
            }
            return res;
        }


        private static void RunUpateProc()
        {
            bool isUpdateSuccess = UpdateProcess.Update();
            if (isUpdateSuccess)
            {
                Logger.Info("Update File success");
                //更新替换文件成功
                if (CheckServiceStart(config.Version))
                {
                    BarControl.IsEndSuccess = true;
                    Logger.Info("Update success version:" + config.Version);
                    ClearAllPackage();
                }
                else
                {
                    //执行检验失败，回滚操作
                    RunRollBack();
                }
            }
            else
            {
                Logger.Info("Update File failed");
                //执行更新失败，回滚操作
                RunRollBack();
            }
        }


        private static void RunRollBack()
        {
            if (UpdateProcess.RollBack() & IsStartSuccess())
            {
                Logger.Info("Roll back success version:" + curVersion);
                ClearAllPackage();
            }
            else
            {
                BarControl.IsRollBackError = true;
            }
            BarControl.IsEndFail = true;
        }

        private static bool CheckUpdate()
        {
            try
            {
                if (Utils.SetServerUrl())
                {
                    curVersion = Utils.GetCurVersion();

                    config = Utils.GetUpdateConfig();
                    if (Utils.IsNewVersion(config.Version, curVersion))
                    {
                        while (true)
                        {
                            if (config.Mandatory)
                            {
                                downloadPackagePath = GetUpdatePackage(config);
                                return true;
                            }
                            else
                            {
                                if (Utils.NoticeUser(config.Message))
                                {
                                    Task.Factory.StartNew(() =>
                                    {
                                        Application.Run(new Form1());
                                    });
                                    downloadPackagePath = GetUpdatePackage(config);
                                    return true;
                                }
                                else
                                {
                                    Thread.Sleep(config.TimSpan * 1000);
                                }
                            }
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    Logger.Info("UpdateUrl is null");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("CheckUpdate err:" + ex.Message);
                return false;
            }
            finally
            {
               BarControl.SetBarStep(BarStep.Download);
            }
        }


        private static string GetUpdatePackage(UpdateConfig config)
        {
            try
            {
                string filePath = Utils.DownloadPackage(config.Url, Cst.PackageName);
                string md5 = Utils.GetMD5Value(filePath);
                if (config.MD5 == md5)
                {
                    return filePath;
                }
                else
                {
                    Utils.DeleteFile(filePath);
                    throw new ArgumentException("Validate MD5 err");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static bool CheckServiceStart(string version)
        {
            try
            {
                Logger.Info("Start Check Service Start or not");
                bool res = false;
                for (int i = 0; i < 10; i++)
                {
                    if (Utils.GetCurVersion() == version)
                    {
                        res = true;
                        break;
                    }
                    Thread.Sleep(1000);
                }
                return res;
            }
            catch (Exception ex)
            {
                Logger.Error("CheckServiceStart err:" + ex);
                return false;
            }
        }

        private static bool IsStartSuccess() {
            try
            {
                Logger.Info("Is start success or not");
                bool res = false;
                for (int i = 0; i < 10; i++)
                {
                    string version = Utils.GetCurVersion();
                    if (!string.IsNullOrEmpty(version))
                    {
                        res = true;
                        break;
                    }
                    Thread.Sleep(1000);
                }
                return res;
            }
            catch (Exception ex)
            {
                Logger.Error("CheckServiceStart err:" + ex);
                return false;
            }
        }

        public static void ClearAllPackage()
        {
            Logger.Info("Start ClearAllPackage");
            try
            {
                if (File.Exists(updateZipPath))
                {
                    File.Delete(updateZipPath);
                }

                if (Directory.Exists(UpdateProcess.bakDirPath))
                {
                    Directory.Delete(UpdateProcess.bakDirPath, true);
                }

                if (Directory.Exists(UpdateProcess.updatePath))
                {
                    Directory.Delete(UpdateProcess.updatePath, true);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ClearAllPackage err:" + ex);
            }
        }
    }
}
