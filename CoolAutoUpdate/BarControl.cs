using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UIH.Update
{
    public static class BarControl
    {
        public static AutoResetEvent areDownload;

        public static AutoResetEvent areExtractFile;

        public static AutoResetEvent areReplace;

        public static AutoResetEvent areFinish;

        public static bool IsEndSuccess { get; set; }

        public static bool IsEndFail { get; set; }

        public static bool IsHandleEx { get; set; }

        public static bool IsRollBackError { get; set; }

        public static void SetAllARE() {
            areDownload.Set();
            areExtractFile.Set();
            areReplace.Set();
            areFinish.Set();
        }

        public static void InitAllParam() {
            areDownload = new AutoResetEvent(false);
            areExtractFile = new AutoResetEvent(false);
            areReplace = new AutoResetEvent(false);
            areFinish = new AutoResetEvent(false);
            IsEndSuccess = false;
            IsEndFail = false;
            IsHandleEx = false;
            IsRollBackError = false;
        }

        public static void SetBarStep(BarStep step) {
            switch (step)
            {
                case BarStep.Download:
                    areDownload.Set();
                    break;
                case BarStep.Extract:
                    areDownload.Set();
                    areExtractFile.Set();
                    break;
                case BarStep.Replace:
                    areDownload.Set();
                    areExtractFile.Set();
                    areReplace.Set();
                    break;
                case BarStep.Finish:
                    areDownload.Set();
                    areExtractFile.Set();
                    areReplace.Set();
                    areFinish.Set();
                    break;
                default:
                    break;
            }
        }
    }

    public enum BarStep
    {
        Download = 0,
        Extract = 1,
        Replace = 2,
        Finish = 3,
    }
}
