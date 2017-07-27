using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nntpPoster
{
    public class UploadSpeedReport
    {
        public Int32 TotalParts { get; set; }
        public Int32 UploadedParts { get; set; }
        public Double BytesPerSecond { get; set; }
        public String CurrentlyPostingName { get; set; }

        public override string ToString()
        {
            var tpl = TotalParts.ToString().Length;

            return String.Format("{0," + tpl + "} of {1} parts uploaded at {2}", UploadedParts, TotalParts,
                GetHumanReadableSpeed(BytesPerSecond));

        }

        public static String GetHumanReadableSpeed(Double bytesPerSecond)
        {
            return GetHumanReadableSize(bytesPerSecond) + "/sec";
        }

        public static String GetHumanReadableSize(Double bytes)
        {
            Double roundedValue;
            String unit;
            if (bytes > 1024 * 1024)
            {
                roundedValue = Math.Round(bytes / (1024 * 1024), 2, MidpointRounding.AwayFromZero);
                unit = "MB";
            }
            else if (bytes > 1024)
            {
                roundedValue = Math.Round(bytes / 1024, 0, MidpointRounding.AwayFromZero);
                unit = "KB";
            }
            else
            {
                roundedValue = Math.Round(bytes, 0, MidpointRounding.AwayFromZero);
                unit = "Bytes";
            }

            return roundedValue.ToString("0.00") + " " + unit;
        }
    }
}
