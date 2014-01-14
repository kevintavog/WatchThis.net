using System;
using System.IO;
using System.Security.Cryptography;

namespace WatchThis.Models
{
	class FileSignature
	{
		public bool HasSignature { get { return _signature != null; } }
		public string Filename { get; set; }
		private string _signature;

		public string Signature()
		{
			lock(this)
			{
				if (_signature == null)
				{
					// Yes, if a file is less than the data read, we calculate the signature with a bunch
					// of zero byte values. It seems better than re-allocating the byte array to the proper
					// size in that case; the affect is the same, I claim.
					byte[] data = new byte[1024];
					using (var stream = File.OpenRead(Filename))
					{
						stream.Read(data, 0, data.Length);
					}
					_signature = BitConverter.ToString(SHA256.Create().ComputeHash(data)) + new FileInfo(Filename).Length;
				}
				return _signature;
			}
		}
	}

}

