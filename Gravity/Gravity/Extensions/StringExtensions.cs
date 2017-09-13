using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Gravity.Extensions
{
	public static class StringExtensions
	{
		private const int Keysize = 256;
		private const int DerivationIterations = 1000;

		public static string Encrypt(this string plainText, string passPhrase)
		{
			var saltStringBytes = Generate256BitsOfRandomEntropy();
			var ivStringBytes = Generate256BitsOfRandomEntropy();
			var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
			using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
			{
				var keyBytes = password.GetBytes(Keysize / 8);
				using (var symmetricKey = new RijndaelManaged())
				{
					symmetricKey.BlockSize = 256;
					symmetricKey.Mode = CipherMode.CBC;
					symmetricKey.Padding = PaddingMode.PKCS7;
					using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes))
					{
						using (var memoryStream = new MemoryStream())
						{
							using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
							{
								cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
								cryptoStream.FlushFinalBlock();
								var cipherTextBytes = saltStringBytes;
								cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
								cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
								memoryStream.Close();
								cryptoStream.Close();
								return Convert.ToBase64String(cipherTextBytes);
							}
						}
					}
				}
			}
		}

		public static string Decrypt(this string cipherText, string passPhrase)
		{
			var cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
			var saltStringBytes = cipherTextBytesWithSaltAndIv.Take(Keysize / 8).ToArray();
			var ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(Keysize / 8).Take(Keysize / 8).ToArray();
			var cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip((Keysize / 8) * 2).Take(cipherTextBytesWithSaltAndIv.Length - ((Keysize / 8) * 2)).ToArray();

			using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
			{
				var keyBytes = password.GetBytes(Keysize / 8);
				using (var symmetricKey = new RijndaelManaged())
				{
					symmetricKey.BlockSize = 256;
					symmetricKey.Mode = CipherMode.CBC;
					symmetricKey.Padding = PaddingMode.PKCS7;
					using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes))
					{
						using (var memoryStream = new MemoryStream(cipherTextBytes))
						{
							using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
							{
								var plainTextBytes = new byte[cipherTextBytes.Length];
								var decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
								memoryStream.Close();
								cryptoStream.Close();
								return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
							}
						}
					}
				}
			}
		}

		private static byte[] Generate256BitsOfRandomEntropy()
		{
			var randomBytes = new byte[32];
			using (var rngCsp = new RNGCryptoServiceProvider())
			{
				rngCsp.GetBytes(randomBytes);
			}
			return randomBytes;
		}

		public static string RemoveHtmlFromMessageContent(this string htmlMessage)
		{
			if (string.IsNullOrEmpty(htmlMessage) == false)
			{
				// make sure new lines show fine in the preview. 
				htmlMessage = htmlMessage.Replace("<br>", "&nbsp;");
				htmlMessage = htmlMessage.Replace("<br/>", "&nbsp;");
				htmlMessage = htmlMessage.Replace("<br />", "&nbsp;");

				// strip off html tags to have only text
				//string pureTextMessage = System.Text.RegularExpressions.Regex.Replace(htmlMessage, "<.*?>", string.Empty);
				string startString = "<";
				string endString = ">";
				htmlMessage = RemoveHtml(htmlMessage, startString, endString);
				startString = "&#";
				endString = ";";
				htmlMessage = RemoveHtml(htmlMessage, startString, endString);
			}

			return htmlMessage;
		}

		public static string RemoveHtml(this string htmlMessage, string startString, string endString)
		{
			while (htmlMessage.IndexOf(startString) >= 0)
			{
				int indStart = htmlMessage.IndexOf(startString);
				int indEnd = htmlMessage.IndexOf(endString);
				if (indEnd < 0)
				{
					break;
				}
				if (indEnd < indStart)
				{
					string tempStart = htmlMessage.Substring(0, indEnd);
					string tempEnd = htmlMessage.Substring(indEnd + endString.Length);
					htmlMessage = tempStart + "%%%" + tempEnd;
					continue;
				}
				string start = htmlMessage.Substring(0, indStart);
				string end = "";
				if (indEnd < (htmlMessage.Length - endString.Length))
				{
					end = htmlMessage.Substring(indEnd + endString.Length);
				}

				htmlMessage = start + end;
			}

			htmlMessage.Replace("%%%", endString);

			return htmlMessage;
		}

		public static List<int> ToListInt(this string splitIntsString, char separator)
		{
			return string.IsNullOrEmpty(splitIntsString) == true ?
				new List<int>() :
				splitIntsString.Split(new char[] { separator }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
		}

		public static string ClipInMiddle(this string stringToClip, int leftCharactersToSkip, int rightCharactersToSkip, string clipSeparator = "..")
		{
			if (stringToClip != string.Empty && stringToClip != null)
			{
				if (stringToClip.Length <= leftCharactersToSkip + rightCharactersToSkip)
				{
					return stringToClip;
				}
				else
				{
					string leftSide = stringToClip.Remove(leftCharactersToSkip);
					string rightSide = stringToClip.Remove(0, stringToClip.Length - rightCharactersToSkip);

					string clippedString = String.Format("{0}{1}{2}", leftSide, clipSeparator, rightSide);
					return clippedString;
				}
			}
			else
			{
				return string.Empty;
			}
		}
	}
}