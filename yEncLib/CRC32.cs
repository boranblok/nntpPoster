using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;

namespace nntpPoster.yEncLib
{
	/// <summary>
	/// Implementation of CRC32 algorithm.
	/// </summary>
	/// <remarks>
	/// Copied & Pasted from Phil Bolduc's implementation at http://www.codeproject.com/csharp/crc32_dotnet.asp,
	/// with modifications to reverse (reflect) the polynomial, to eliminate redundant methods,
	/// and to work incrementally.
	/// </remarks>
	public class CRC32 : HashAlgorithm
	{
		protected static uint AllOnes = 0xffffffff;
		protected static Hashtable cachedCRC32Tables;
		protected static bool autoCache;
	
		protected uint[] crc32Table; 
		private uint m_crc;
		
		/// <summary>
		/// Returns the default polynomial (used in WinZip, Ethernet, etc)
		/// </summary>
		public static uint DefaultPolynomial
		{
			get { return 0x04C11DB7; }
		}

		/// <summary>
		/// Gets or sets the auto-cache setting of this class.
		/// </summary>
		public static bool AutoCache
		{
			get { return autoCache; }
			set { autoCache = value; }
		}

		/// <summary>
		/// Initialize the cache
		/// </summary>
		static CRC32()
		{
			cachedCRC32Tables = Hashtable.Synchronized( new Hashtable() );
			autoCache = true;
		}

		public static void ClearCache()
		{
			cachedCRC32Tables.Clear();
		}

		private static uint Reflect(uint val)
		{
			uint oval = 0;
			for (int i=0; i<32; i++)
			{
				oval = (oval<<1) + (val&1);
				val >>= 1;
			}
			return oval;
		}

		/// <summary>
		/// Builds a crc32 table given a polynomial
		/// </summary>
		/// <param name="ulPolynomial"></param>
		/// <returns></returns>
		protected static uint[] BuildCRC32Table( uint ulPolynomial )
		{
			uint dwCrc;
			uint[] table = new uint[256];

			ulPolynomial = Reflect(ulPolynomial);

			// 256 values representing ASCII character codes. 
			for (int i = 0; i < 256; i++)
			{
				dwCrc = (uint)i;
				for (int j = 8; j > 0; j--)
				{
					if((dwCrc & 1) == 1)
						dwCrc = (dwCrc >> 1) ^ ulPolynomial;
					else
						dwCrc >>= 1;
				}
				table[i] = dwCrc;
			}

			return table;
		}

		/// <summary>
		/// Added for testing purposes
		/// </summary>
		public uint[] CurrentTable
		{
			get { return crc32Table; }
		}
	

		/// <summary>
		/// Creates a CRC32 object using the DefaultPolynomial
		/// </summary>
		public CRC32() : this(DefaultPolynomial)
		{
		}

		/// <summary>
		/// Creates a CRC32 object using the specified Creates a CRC32 object 
		/// </summary>
		public CRC32(uint aPolynomial) : this(aPolynomial, CRC32.AutoCache)
		{
		}
	
		/// <summary>
		/// Construct the 
		/// </summary>
		public CRC32(uint aPolynomial, bool cacheTable)
		{
			this.HashSizeValue = 32;

			crc32Table = (uint []) cachedCRC32Tables[aPolynomial];
			if ( crc32Table == null )
			{
				crc32Table = CRC32.BuildCRC32Table(aPolynomial);
				if ( cacheTable )
                {
                    // Note: this class sometimes creates duplicate values;
                    // since we arent using crc32 for now, the below is here
                    // to make the decoding work without throwing unhandled
                    // exceptions;
                    // ToDo: fix before we start using CRC32.
                    if (!cachedCRC32Tables.ContainsKey(aPolynomial)) 
                    {
                        cachedCRC32Tables.Add(aPolynomial, crc32Table);
                    }
                }
			}
			Initialize();
		}
	
		/// <summary>
		/// Initializes an implementation of HashAlgorithm.
		/// </summary>
		public override void Initialize()
		{
			m_crc = AllOnes;
			this.State = 0;
		}
	
		/// <summary>
		/// 
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="offset"></param>
		/// <param name="count"></param>
		protected override void HashCore(byte[] buffer, int offset, int count)
		{
			for (int i = offset; i < offset + count; i++)
			{
				ulong tabPtr = (m_crc & 0xFF) ^ buffer[i];
				m_crc >>= 8;
				m_crc ^= crc32Table[tabPtr];
			}

			this.State = 1;
		}
	
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		protected override byte[] HashFinal()
		{
			byte [] finalHash = new byte [ 4 ];
			ulong finalCRC = m_crc ^ AllOnes;
		
			finalHash[0] = (byte) ((finalCRC >> 24) & 0xFF);
			finalHash[1] = (byte) ((finalCRC >> 16) & 0xFF);
			finalHash[2] = (byte) ((finalCRC >>  8) & 0xFF);
			finalHash[3] = (byte) ((finalCRC >>  0) & 0xFF);
		
			this.State = 0;
			return finalHash;
		}

        public string HashAsHexString
        {
            get
            {
                string ret = string.Empty;
                if (Hash != null && Hash.Length > 0)
                {
                    for (int i = 0; i < Hash.Length; i++)
                    {
                        ret += Hash[i].ToString("X2");
                    }
                }
                return ret;
            }
        }
	}
}