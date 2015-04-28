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

        public String GetHumanReadableSpeed()
        {
            Double roundedValue;
            String unit;
            if (BytesPerSecond > 1024*1024)
            {
                roundedValue = Math.Round(BytesPerSecond/(1024*1024), 2, MidpointRounding.AwayFromZero);
                unit = "MB";
            }
            else if (BytesPerSecond > 1024)
            {
                roundedValue = Math.Round(BytesPerSecond/1024, 0, MidpointRounding.AwayFromZero);
                unit = "KB";
            }
            else
            {
                roundedValue = Math.Round(BytesPerSecond, 0, MidpointRounding.AwayFromZero);
                unit = "Bytes";
            }

            return roundedValue.ToString("0.00") + " " + unit + "/sec";
        }

        public override string ToString()
        {
            var tpl = TotalParts.ToString().Length;

            return String.Format("{0," + tpl + "} of {1} parts uploaded at {2}, now posting {3}", UploadedParts, TotalParts,
                GetHumanReadableSpeed(), CurrentlyPostingName);

        }
    }
}
