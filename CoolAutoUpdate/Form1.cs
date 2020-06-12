using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UIH.Update
{
    public partial class Form1 : Form
    {
        public delegate void MessageReceivedHandler();
        public event MessageReceivedHandler MessageReceived;
        public Form1()
        {
            InitializeComponent();
            this.TopMost = true;
            this.btnClose.Enabled = false;
            MessageReceived += Form1_MessageReceived;
            MessageReceived.Invoke();
        }

        private void Form1_MessageReceived()
        {
            this.progressBar1.Value = 0;
            var newClass = new ProgressClass();
            this.progressBar1.Maximum = newClass.counts;
            newClass.doProgress += Progress;
            newClass.CompleteProgress += Complete;
            Thread thread = new Thread(new ThreadStart(newClass.Progress));
            thread.IsBackground = true;
            thread.Start();
        }

        private Action<int> otherInvoke;
        private void Progress()
        {
            //更新进度条value和label的显示
            Action<int> otherThread = s =>
            {
                this.progressBar1.Value += s;

                if (!BarControl.IsHandleEx)
                {
                    if (this.progressBar1.Value <= 17 & this.progressBar1.Value > 8)
                    {
                        this.labTitle.Text = "正在下载升级包，请稍后...";
                    }

                    if (this.progressBar1.Value > 17 & this.progressBar1.Value <= 25)
                    {
                        this.labTitle.Text = "正在解压升级包，请稍后...";
                    }

                    if (this.progressBar1.Value > 25 & this.progressBar1.Value <= 83)
                    {
                        this.labTitle.Text = "正在执行升级操作，请稍后...";
                    }

                    if (this.progressBar1.Value > 83)
                    {
                        this.labTitle.Text = "正在校验程序功能，请稍后...";
                    }
                }
                else
                {
                    this.labTitle.Text = "检测到系统环境存在异常，正在修复...";
                }

                this.label1.Text = this.progressBar1.Value + "%";
            };
            if (InvokeRequired)
            {
                //每次增加1
                this.Invoke(otherThread, 1);
            }
        }

        private void Complete()
        {
            //完成之后做的事
            Action complete = () =>
            {
                this.progressBar1.Value = 100;
                if (BarControl.IsRollBackError)
                {
                    this.labTitle.Text = "文件损坏，请重新下载安装包。";
                }
                else
                {
                    if (!BarControl.IsHandleEx)
                    {
                        if (BarControl.IsEndFail)
                        {
                            this.labTitle.Text = "升级失败，已恢复原始文件。";
                        }
                        else
                        {
                            this.labTitle.Text = "升级完成，请重新打开浏览器！";
                        }
                    }
                    else
                    {
                        this.labTitle.Text = "您的系统环境已修复成功！";
                    }
                }
                this.label1.Text = "100%";
                this.btnClose.Enabled = true;
            };
            if (InvokeRequired)
            {
                this.Invoke(complete);
            }
        }

        private class ProgressClass
        {
            /// <summary>
            /// 循环次数
            /// </summary>
            public int counts = 100;
            /// <summary>
            /// 每次循环做的事
            /// </summary>
            public Action doProgress;
            /// <summary>
            /// 循环结束之后做的事
            /// </summary>
            public Action CompleteProgress;
            /// <summary>
            /// 循环更新进度条
            /// </summary>
            public void Progress()
            {
                for (int i = 0; i < counts; i++)
                {
                    doProgress();
                  
                    if (BarControl.IsEndSuccess|| BarControl.IsEndFail)
                    {
                        Thread.Sleep(40);
                    }
                    else
                    {
                        if (i == 17)
                        {
                            BarControl.areDownload.WaitOne();
                        }
                        if (i == 25)
                        {
                            BarControl.areExtractFile.WaitOne();
                        }
                        if (i == 83)
                        {
                            BarControl.areReplace.WaitOne();
                        }
                        if (i == 95)
                        {
                            BarControl.areFinish.WaitOne();
                        }
                        Thread.Sleep(500);
                    }
                }
                CompleteProgress();
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
