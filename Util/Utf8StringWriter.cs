using System.IO;
using System.Text;

namespace Util
{
    public class Utf8StringWriter : StringWriter
    {
        public Utf8StringWriter(StringBuilder sb) : base(sb)
        { }

        public override Encoding Encoding
        {
            get { return Encoding.UTF8; }
        }
    }
}
