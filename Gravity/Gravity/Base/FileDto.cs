using System;
using System.IO;
using System.Security.Cryptography;

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
		public DiskFileDto(string filePath)
		{
			FilePath = filePath;
		}

		public string FilePath { get; set; }

		public ByteArrayFileDto StoreInMemory()
		{
			return new ByteArrayFileDto() 
			{
				ByteArray = File.ReadAllBytes(FilePath),
				FileName = Path.GetFileName(FilePath)
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
			File.WriteAllBytes(filePath, ByteArray);
			return new DiskFileDto(filePath);
		}

		protected override Stream GetStream() => new MemoryStream(ByteArray);
	}
}
