using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Gravity.Base
{
	public abstract class FileDto
	{
		internal FileDto() { }

		internal string GetMD5()
		{
			using (var md5 = MD5.Create())
			using (var stream = GetStream())
			{
				byte[] byteHashedPassword = md5.ComputeHash(stream);
				return BitConverter.ToString(byteHashedPassword).Replace("-", "").ToLower();
			}
		}

		protected abstract Stream GetStream();
	}

	public class DiskFileDto : FileDto
	{
		public string FilePath { get; set; }

		public ByteArrayFileDto StoreInMemory()
		{
			return new ByteArrayFileDto
			{
				ByteArray = File.ReadAllBytes(this.FilePath),
				FileName = Path.GetFileName(this.FilePath)
			};
		}

		protected override Stream GetStream() => File.OpenRead(FilePath);
	}

	public class ByteArrayFileDto : FileDto
	{
		public byte[] ByteArray { get; set; }

		public string FileName { get; set; }

		public DiskFileDto WriteToFile(string filePath)
		{
			File.WriteAllBytes(filePath, this.ByteArray);
			return new DiskFileDto
			{
				FilePath = filePath
			};
		}

		protected override Stream GetStream() => new MemoryStream(ByteArray);
	}
}
