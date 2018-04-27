using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExternalProcessWrappers
{
    [Serializable]
    public class Par2BlockSizeTooSmallException : Exception
    {
        public Par2BlockSizeTooSmallException() : base() { }

        public Par2BlockSizeTooSmallException(String message) : base(message) { }

        public Par2BlockSizeTooSmallException(String message, Exception innerException) : base(message, innerException) { }
    }
}
