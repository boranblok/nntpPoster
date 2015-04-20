using System;
using System.Text;
using System.Security.Cryptography;

namespace nntpPoster.yEncLib
{
	public class YEncEncoder
	{
		const Byte delta = 42;
        const Byte escapeAdditionalDelta = 64;
        const Byte escapeByte = 61;
        const Byte dot = 46;
        Byte[] escapeBytes = new Byte[] { 10, 13, 0, escapeByte };

        /// <summary>
        /// Encodes an entire block of bytes into yEnc format splitting the output every lineLength bytes into a new line.
        /// It is reccomended to pass in a multiple of linelength as source as long as the input file can provide this.
        /// </summary>
        /// <param name="lineLength">The length of source bytes every line represents.</param>
        /// <param name="source">the source input buffer to encode</param>
        /// <param name="offset">From which starting position to encode</param>
        /// <param name="count">How many bytes to encode from the source starting from offset.</param>
        /// <returns>A block of yEnc encoded bytes</returns>
        public Byte[] EncodeBlock(Int32 lineLength, Byte[] source, Int32 offset, Int32 count)
        {
            Byte[] buffer = new Byte[source.Length * 2];
            Int32 position = 0;
            Boolean isStartOfLine = true;
            for (Int32 i = 0; i < count; i++)
            {
                Byte b = source[offset + i];
                Boolean escape = false;
                Byte e = EncodeByte(b, out escape);
                if (escape)
                {                    
                    buffer[position] = escapeByte;
                    position++;
                    isStartOfLine = false;  //If we have an escape char as first byte it is not a dot.
                }
                if (isStartOfLine)
                {
                    isStartOfLine = false;
                    if(e == dot)
                    {
                        buffer[position] = dot;
                        position++;
                    }
                }

                buffer[position] = e;
                position++;

                if( (i + 1) % lineLength == 0)  //We add a newline every lineLength BYTES of the source input. The encoded line length might differ depending on number of escaped characters.
                {
                    buffer[position] = 13;
                    position++;

                    buffer[position] = 10;
                    position++;

                    isStartOfLine = true;
                }
            }

            if (!isStartOfLine) //If we didnt end nicely on a line we add another newline.
            {
                buffer[position] = 13;
                position++;

                buffer[position] = 10;
                position++;
            }

            Byte[] output = new Byte[position];
            Buffer.BlockCopy(buffer, 0, output, 0, position);
            return output;
        }

		/// <summary>
		/// Encodes a single byte.
		/// </summary>
		/// <param name="b">Byte to encode</param>
		/// <param name="escape">returns true if the returned byte needs to be escaped</param>
		/// <returns>the encoded byte</returns>
        private Byte EncodeByte(Byte b, out Boolean escape)
		{
            unchecked       //unchecked, so we wrap aroundthe byte due to overflow.
			{
				b += delta;

				escape = false;
                foreach(byte escb in escapeBytes)
                {
                    if (b == escb)
                    {
                        escape = true;
                        b += escapeAdditionalDelta;
                        break;
                    }
                }
			}

			return b;
		}
	}
}