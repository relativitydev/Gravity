using System;
using System.Linq;
using NUnit.Framework;
using Gravity.Base;
using Gravity.Test.TestClasses;
using System.IO;
using Gravity.DAL.RSAPI;

namespace Gravity.Test.Integration
{
	public partial class RSAPI_IntegrationTest
	{
		#region "File Tests"

		private static ByteArrayFileDto GetByteArrayFile()
		{
			return new ByteArrayFileDto
			{
				FileName = "TestFile.txt",
				ByteArray = new byte[] { 65 }
			};
		}

		private static DiskFileDto GetDiskFile()
		{
			var testFile = new DiskFileDto(Path.Combine(Path.GetTempPath(), "TestFile.txt"));
			File.WriteAllBytes(testFile.FilePath, new byte[] { 65 });
			return testFile;
		}

		private static ByteArrayFileDto UpdateAndReturnFileFromServer(RsapiDao rsapiDao, GravityLevelOne testObject)
		{
			if (testObject.ArtifactId == 0)
			{
				testObject.ArtifactId = rsapiDao.Insert(testObject, ObjectFieldsDepthLevel.FirstLevelOnly);
			}
			else
			{
				rsapiDao.Update(testObject, ObjectFieldsDepthLevel.FirstLevelOnly);
			}
			var returnObject = rsapiDao.Get<GravityLevelOne>(testObject.ArtifactId, ObjectFieldsDepthLevel.OnlyParentObject);
			return (ByteArrayFileDto)returnObject.FileField;
		}


		[Test, Description("Verify file is created from byte array")]
		public void Valid_Gravity_Object_Create_ByteArrayFile()
		{
			void Inner()
			{
				LogStart("Arrangement");

				var testFile = GetByteArrayFile();

				GravityLevelOne testObject = new GravityLevelOne()
				{
					Name = $"TestObject_WithBufferFile_{Guid.NewGuid()}",
					FileField = testFile
				};

				LogEnd("Arrangement");

				LogStart("Act");
				var rsapiDao = _testObjectHelper.GetDao();
				var returnFile = UpdateAndReturnFileFromServer(rsapiDao, testObject);

				LogEnd("Act");

				LogStart("Assertion");

				Assert.AreEqual(testFile.FileName, returnFile.FileName);
				CollectionAssert.AreEqual(testFile.ByteArray, returnFile.ByteArray);

				LogEnd("Assertion");
			}
			TestWrapper(Inner);
		}


		[Test, Description("Verify file is created from file on disk")]
		public void Valid_Gravity_Object_Create_DiskFile()
		{
			void Inner()
			{
				LogStart("Arrangement");


				var testFile = GetDiskFile();
				File.WriteAllBytes(testFile.FilePath, new[] { (byte)'a' });

				GravityLevelOne testObject = new GravityLevelOne()
				{
					Name = $"TestObject_WithDiskFile_{Guid.NewGuid()}",
					FileField = testFile
				};


				try
				{
					LogEnd("Arrangement");

					LogStart("Act");
					var rsapiDao = _testObjectHelper.GetDao();
					var returnFile = UpdateAndReturnFileFromServer(rsapiDao, testObject);

					LogEnd("Act");

					LogStart("Assertion");

					Assert.AreEqual("TestFile.txt", returnFile.FileName);
					CollectionAssert.AreEqual(File.ReadAllBytes(testFile.FilePath), returnFile.ByteArray);

					LogEnd("Assertion");
				}
				finally
				{
					File.Delete(testFile.FilePath);
				}


			}
			TestWrapper(Inner);
		}


		private void Valid_Gravity_Object_SetUnsetFile_Inner(FileDto fileDto)
		{
			LogStart("Arrangement");

			var rsapiDao = _testObjectHelper.GetDao();
			GravityLevelOne testObject = new GravityLevelOne()
			{
				Name = $"TestObject_WithFile_{Guid.NewGuid()}"
			};

			LogEnd("Arrangement");

			LogStart("Act/Assert");

			//Test 1: Insert null
			Assert.Null(UpdateAndReturnFileFromServer(rsapiDao, testObject));

			//Test 2: Insert Array
			testObject.FileField = fileDto;
			var serverReadObject = UpdateAndReturnFileFromServer(rsapiDao, testObject);
			Assert.AreEqual("TestFile.txt", serverReadObject.FileName);
			Assert.AreEqual(65, serverReadObject.ByteArray.Single());

			//Test 3: Unset
			testObject.FileField = null;
			Assert.Null(UpdateAndReturnFileFromServer(rsapiDao, testObject));

			LogEnd("Act/Assert");
		}


		[Test, Description("Verify can update and unset byte array files")]
		public void Valid_Gravity_Object_SetUnsetFile_ByteArrayFile()
		{
			void Inner()
			{
				Valid_Gravity_Object_SetUnsetFile_Inner(GetByteArrayFile());
			}
			TestWrapper(Inner);
		}

		[Test, Description("Verify can update and unset disk files")]
		public void Valid_Gravity_Object_SetUnsetFile_DiskFile()
		{
			void Inner()
			{
				var fileDto = GetDiskFile();
				try
				{
					Valid_Gravity_Object_SetUnsetFile_Inner(fileDto);
				}
				finally
				{
					File.Delete(fileDto.FilePath);
				}
			}
			TestWrapper(Inner);
		}

		[Test, Description("Verify can update file, but no update if byte array unchanged")]
		[TestCase(true, false, TestName = "{m}_UpdateBytes")]
		[TestCase(true, true, TestName = "{m}_ReplaceBytes")]
		[TestCase(false, false, TestName = "{m}_UpdateName")]
		[TestCase(true, true, TestName = "{m}_ReplaceName")]
		public void Valid_Gravity_Object_UpdateFile_ByteArrayFile(
			bool updateByes,
			bool replaceExistingObject
			)
		{
			void Inner()
			{
				LogStart("Arrangement");

				var rsapiDao = _testObjectHelper.GetDao();
				GravityLevelOne testObject = new GravityLevelOne()
				{
					Name = $"TestObject_WithBufferFile_{Guid.NewGuid()}",
					FileField = GetByteArrayFile()
				};
				testObject.ArtifactId = rsapiDao.Insert(testObject, ObjectFieldsDepthLevel.FirstLevelOnly);

				LogEnd("Arrangement");

				LogStart("Act");

				if (replaceExistingObject)
				{
					testObject.FileField = new ByteArrayFileDto { ByteArray = new byte[1] };
				}
				((ByteArrayFileDto)testObject.FileField).FileName = "TestFile2.txt";
				if (updateByes)
				{
					((ByteArrayFileDto)testObject.FileField).ByteArray[0] = 66;
				}

				var returnFile = UpdateAndReturnFileFromServer(rsapiDao, testObject);

				LogEnd("Act");

				LogStart("Asserting");


				Assert.AreEqual(updateByes ? "TestFile2.txt" : "TestFile.txt", returnFile.FileName); //name update ignore if bytes not updated
				Assert.AreEqual(updateByes ? 66 : 65, returnFile.ByteArray.Single());

				LogEnd("Asserting");
			}
			TestWrapper(Inner);
		}

		[Test, Description("Verify can update file, but no update if disk file contents unchanged")]
		[TestCase(true, false, TestName = "{m}_UpdateContent")]
		[TestCase(true, true, TestName = "{m}_ReplaceContent")]
		[TestCase(false, false, TestName = "{m}_UpdateName")]
		[TestCase(true, true, TestName = "{m}_ReplaceName")]
		public void Valid_Gravity_Object_UpdateFile_DiskFile(
			bool updateContent,
			bool replaceExistingObject
			)
		{
			void Inner()
			{
				LogStart("Arrangement");

				var rsapiDao = _testObjectHelper.GetDao();
				GravityLevelOne testObject = new GravityLevelOne()
				{
					Name = $"TestObject_WithDiskFile_{Guid.NewGuid()}",
					FileField = GetDiskFile()
				};

				DiskFileDto GetFileField() => (DiskFileDto)testObject.FileField;

				try
				{
					testObject.ArtifactId = rsapiDao.Insert(testObject, ObjectFieldsDepthLevel.FirstLevelOnly);

					LogEnd("Arrangement");

					LogStart("Act");

					File.Delete(GetFileField().FilePath);
					if (replaceExistingObject)
					{
						testObject.FileField = new DiskFileDto(Path.Combine(Path.GetTempPath(), "TestFile2.txt"));
					}

					File.WriteAllBytes(GetFileField().FilePath, new[] { (byte)(updateContent ? 66 : 65) });

					var returnFile = UpdateAndReturnFileFromServer(rsapiDao, testObject);

					LogEnd("Act");

					LogStart("Asserting");

					Assert.AreEqual(updateContent ? "TestFile2.txt" : "TestFile.txt", returnFile.FileName); //name update ignore if bytes not updated
					Assert.AreEqual(updateContent ? 66 : 65, returnFile.ByteArray.Single());

					LogEnd("Asserting");

				}
				finally
				{
					File.Delete(GetFileField().FilePath);
				}
			}
			TestWrapper(Inner);
		}

		[Test, Description("Verify switching from byte array to file alone does not update file")]
		public void Valid_Gravity_Object_UpdateFile_SwitchFromDiskToByteArray()
		{
			void Inner()
			{
				LogStart("Arrangement");

				var rsapiDao = _testObjectHelper.GetDao();
				var diskFile = GetDiskFile();
				GravityLevelOne testObject = new GravityLevelOne()
				{
					Name = $"TestObject_WithDiskFile_{Guid.NewGuid()}",
					FileField = diskFile
				};

				DiskFileDto GetFileField() => (DiskFileDto)testObject.FileField;

				try
				{
					testObject.ArtifactId = rsapiDao.Insert(testObject, ObjectFieldsDepthLevel.FirstLevelOnly);

					LogEnd("Arrangement");

					LogStart("Act");

					var arrayFile = GetFileField().StoreInMemory();
					arrayFile.FileName = "TestFile2.txt";

					var returnFile = UpdateAndReturnFileFromServer(rsapiDao, testObject);

					LogEnd("Act");

					LogStart("Asserting");

					Assert.AreEqual("TestFile.txt", returnFile.FileName); //name update ignore if bytes not updated

					LogEnd("Asserting");
				}
				finally
				{
					File.Delete(GetFileField().FilePath);
				}
			}
			TestWrapper(Inner);
		}

		[Test, Description("Verify switching from byte array to file alone does not update file")]
		public void Valid_Gravity_Object_UpdateFile_SwitchFromByteArrayToDisk()
		{
			void Inner()
			{
				LogStart("Arrangement");

				var rsapiDao = _testObjectHelper.GetDao();
				GravityLevelOne testObject = new GravityLevelOne()
				{
					Name = $"TestObject_WithBufferFile_{Guid.NewGuid()}",
					FileField = GetByteArrayFile()
				};
				testObject.ArtifactId = rsapiDao.Insert(testObject, ObjectFieldsDepthLevel.FirstLevelOnly);

				LogEnd("Arrangement");

				LogStart("Act");

				testObject.FileField = GetByteArrayFile().WriteToFile(Path.Combine(Path.GetTempPath(), "TestFile2.txt"));

				try
				{
					var returnFile = UpdateAndReturnFileFromServer(rsapiDao, testObject);

					LogEnd("Act");

					LogStart("Asserting");


					Assert.AreEqual("TestFile.txt", returnFile.FileName); //name update ignore if bytes not updated

					LogEnd("Asserting");
				}
				finally
				{
					File.Delete(((DiskFileDto)testObject.FileField).FilePath);
				}
			}
			TestWrapper(Inner);
		}

		#endregion
	}
}
