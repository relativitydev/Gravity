using System;
using System.IO;
using System.Security.Cryptography;

namespace Gravity.Base
{
	public abstract class FileDto
	{
		protected FileDto(string filePath) { }

		protected FileDto() { }

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
		public DiskFileDto() { }

		public DiskFileDto(string filePath)
			: base(filePath)
		{
			FilePath = filePath;
		}
		
		public string FilePath { get; set; }

		public ByteArrayFileDto StoreInMemory()
		{
			return new ByteArrayFileDto(FilePath);
		}

		protected override Stream GetStream() => File.OpenRead(FilePath);
	}

	public class ByteArrayFileDto : FileDto
	{
		public ByteArrayFileDto() { }

		public ByteArrayFileDto(string filePath)
			: base(filePath)
		{
			ByteArray = File.ReadAllBytes(filePath);
			FileName = Path.GetFileName(filePath);
		}

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
