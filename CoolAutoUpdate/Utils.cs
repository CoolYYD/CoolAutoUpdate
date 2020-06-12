using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

namespace UIH.Update
{
    public static class Utils
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Utils));
        private static string packagePath = AppDomain.CurrentDomain.BaseDirectory;
        private static string UpdateUrl = "";
        public static string UpdateConfigUrl = "";
        public static string DownloadPackage(string url, string fileName)
        {
            HttpWebRequest request = null;
            try
            {
                if (!Directory.Exists(packagePath))
                    Directory.CreateDirectory(packagePath);
                var path = Path.Combine(packagePath, fileName + ".zip");
                request = WebRequest.Create(url) as HttpWebRequest;
                request.KeepAlive = false;
                ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        using (Stream stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite))
                        {
                            var length = 1024;
                            var bArr = new byte[length];
                            int size;
                            while ((size = responseStream.Read(bArr, 0, length)) > 0)
                            {
                                stream.Write(bArr, 0, size);
                            }
                            return path;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Download package error:" + url + ",Error:" + ex);
                return string.Empty;
            }
            finally
            {
                if (request != null)
                {
                    request.Abort();
                }
            }
        }

        public static bool SetServerUrl()
        {
            ConfigurationManager.RefreshSection("appSettings");
            UpdateUrl = ConfigurationManager.AppSettings["UpdateUrl"].ToString();
            if (string.IsNullOrEmpty(UpdateUrl))
            {
                return false;
            }
            else
            {
                UpdateConfigUrl = UpdateUrl + "/UpdateConfig.json";
                Logger.Info("UpdateConfig url:" + UpdateConfigUrl);
                return true;
            }
        }

        public static string GetCurVersion()
        {
            try
            {
               return  GetResponse("http://localhost:8260/UIHClientProxyService/GetProxyClientVersion").Replace("\"", string.Empty);
            }
            catch (Exception ex)
            {
                Logger.Info("GetCurVersion err:" + ex.Message);
                return "";
            }
        }

        public static UpdateConfig GetUpdateConfig()
        {
            string res = Get(UpdateConfigUrl);
            return JsonConvert.DeserializeObject<UpdateConfig>(res);
        }

        public static string Get(string Url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
            request.Proxy = null;
            request.KeepAlive = false;
            request.Method = "GET";
            request.ContentType = "application/json; charset=UTF-8";
            request.AutomaticDecompression = DecompressionMethods.GZip;
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.UTF8);
            string retString = myStreamReader.ReadToEnd();

            myStreamReader.Close();
            myResponseStream.Close();

            if (response != null)
            {
                response.Close();
            }
            if (request != null)
            {
                request.Abort();
            }

            return retString;
        }


        public static bool IsNewVersion(string newVersion, string oldVersion)
        {
            if (!string.IsNullOrEmpty(newVersion) & !string.IsNullOrEmpty(oldVersion))
            {
                Version nVersion = new Version(newVersion);
                Version oVersion = new Version(oldVersion);
                return nVersion > oVersion;
            }
            else
            {
                return false;
            }
        }

        public static bool NoticeUser(string msg)
        {
            DialogResult res = MessageBox.Show(msg, Cst.title, MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (res.ToString() == "Yes")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static string GetResponse(string url)
        {
            if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "Get";
                var response = (HttpWebResponse)request.GetResponse();
                return GetResponseResult(response);
            }
            throw new HttpRequestValidationException("URL地址不正确");
        }

        /// <summary>
        /// 获取返回字符串
        /// </summary>
        /// <param name="response">响应</param>
        /// <returns>字符串</returns>
        private static string GetResponseResult(HttpWebResponse response)
        {
            using (var responseStream = response.GetResponseStream())
            {
                if (responseStream != null)
                {
                    using (var myStreamReader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")))
                    {
                        string result = myStreamReader.ReadToEnd();
                        return result;
                    }
                }
                const string message = "There is no response with the request!";
                throw new ArgumentNullException(message);
            }
        }

        /// <summary>
        /// 获取文件的MD5值
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        public static string GetMD5Value(string filePath)
        {
            using (var fs = File.OpenRead(filePath))
            {
                return GetMD5Hash(fs);
            }
        }

        private static string GetMD5Hash(Stream inputStream)
        {
            using (var md5 = new MD5CryptoServiceProvider())
            {
                var hash = md5.ComputeHash(inputStream);
                var sBuilder = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                {
                    sBuilder.Append(hash[i].ToString("X2"));
                }
                return sBuilder.ToString();
            }
        }

        public static bool DeleteFile(string filePath)
        {
            try
            {
                File.Delete(filePath);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("DeleteFile err:" + ex);
                return false;
            }
        }

        public static void RunCmd(int type)
        {
            try
            {
                Logger.Info("RunCmd type:" + type.ToString());
                string filePath = null;
                switch (type)
                {
                    case (int)OpeType.Start:
                        filePath = packagePath + Cst.startPath;
                        break;
                    case (int)OpeType.Stop:
                        filePath = packagePath + Cst.stopPath;
                        break;
                    default:
                        break;
                }
                Process process = new Process();
                process.StartInfo.FileName = filePath;
                process.StartInfo.WorkingDirectory = Path.GetDirectoryName(filePath);
                process.Start();
                process.WaitForExit();
                process.Close();
            }

            catch (Exception ex)
            {
                Logger.Error("RunCmd err:" + ex);
            }
        }

        /// <summary>
        /// 解压zip文件
        /// </summary>
        /// <param name="zipFilePath">.zip压缩包路径</param>
        /// <param name="extractPath">存放解压后文件路径</param>
        /// <returns></returns>
        public static bool ExtractFile(string zipFilePath, string extractPath)
        {
            if (string.IsNullOrEmpty(zipFilePath))
            {
                Logger.Error("zipFilePath can not null.");
                return false;
            }
            if (!Directory.Exists(extractPath))
            {
                Directory.CreateDirectory(extractPath);
            }
            var name = Path.GetFileNameWithoutExtension(zipFilePath);
            try
            {
                var filePath = Path.Combine(extractPath, name);
                if (Directory.Exists(filePath))
                {
                    Directory.Delete(filePath, true);
                }

                ZipFile.ExtractToDirectory(zipFilePath, filePath);
                return true;
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("ExtractFile err,file:{0},{1}", zipFilePath, ex);
                return false;
            }
            finally
            {
                BarControl.SetBarStep(BarStep.Extract);
            }
        }
    }
}
