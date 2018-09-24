using kCura.Relativity.Client;
using kCura.Relativity.Client.DTOs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Gravity.Base;
using Gravity.Exceptions;
using Gravity.Extensions;

namespace Gravity.DAL.RSAPI
{
	public partial class RsapiDao
	{
		#region RDO INSERT Protected Stuff

		protected void InsertUpdateFileFields<T>(IEnumerable<T> objectsToInsert, bool objectsAreNew) where T : BaseDto
		{
			foreach (var propertyInfo in typeof(T).GetProperties())
			{
				var attribute = propertyInfo.GetCustomAttribute<RelativityObjectFieldAttribute>();
				if (attribute?.FieldType != RdoFieldType.File)
					continue;

				foreach (var objectToInsert in objectsToInsert)
				{ 
					var relativityFile = (FileDto)propertyInfo.GetValue(objectToInsert);
					if (relativityFile == null && objectsAreNew)
						continue; //if new objects, not clearing anything;

					InsertUpdateFileField(attribute.FieldGuid, objectToInsert.ArtifactId, relativityFile);
				}
			}
		}

		protected void InsertUpdateFileField(Guid fieldGuid, int objectArtifactId, FileDto fileDto)
		{			
			var currentMD5 = fileDto?.GetMD5() ?? "";

			if (fileMd5Cache.Get(fieldGuid, objectArtifactId) == currentMD5) //in cache and matches
			{
				return;
			}

			var fileFieldArtifactId = this.guidCache.Get(fieldGuid);

			if (fileDto == null)
			{
				rsapiProvider.ClearFile(fileFieldArtifactId, objectArtifactId);
			}
			else
			{
				DiskFileDto temporaryFileDto = null;
				if (fileDto is ByteArrayFileDto arrayFileDto)
				{
					//TODO: check file name not null or empty
					temporaryFileDto = arrayFileDto.WriteToFile(Path.Combine(Path.GetTempPath(), arrayFileDto.FileName));
				}

				try
				{
					var filePath = (temporaryFileDto ?? (DiskFileDto)fileDto).FilePath;
					rsapiProvider.UploadFile(fileFieldArtifactId, objectArtifactId, filePath);
					fileMd5Cache.Set(fieldGuid, objectArtifactId, currentMD5);
				}
				finally
				{
					if (temporaryFileDto != null)
					{
						invokeWithRetryService.InvokeVoidMethodWithRetry(() => File.Delete(temporaryFileDto.FilePath));
					}
				}
			}

			fileMd5Cache.Set(fieldGuid, objectArtifactId, currentMD5);
		}



		#endregion

		private void InsertChildListObjects<T>(IEnumerable<T> objectsToInsert, bool recursive) where T : BaseDto
		{
			var childProperties = typeof(T)
				.GetPropertyAttributeTuples<RelativityObjectChildrenListAttribute>()
				.Select(x => x.Item1);

			foreach (var childPropertyInfo in childProperties)
			{
				IEnumerable<BaseDto> GetObjectsToInsert(T theObjectToInsert)
				{
					return ((IEnumerable)childPropertyInfo.GetValue(theObjectToInsert))?
						.Cast<BaseDto>()
						.Select(childObject =>
						{
							var parentArtifactIdProperty = childObject.GetParentArtifactIdProperty();
							parentArtifactIdProperty.SetValue(childObject, theObjectToInsert.ArtifactId);
							return childObject;
						});
				}

				var childObjectsToInsert = objectsToInsert.Select(GetObjectsToInsert)
					.Where(x => x != null)
					.SelectMany(x => x);

				var childType = childPropertyInfo.PropertyType.GetEnumerableInnerType();

				this.InvokeGenericMethod(childType, nameof(Insert), BaseExtensionMethods.MakeGenericList(childObjectsToInsert, childType), recursive);
			}
		}

		private void InsertSingleObjectFields<T>(IEnumerable<T> objectsToInsert, bool recursive) where T : BaseDto
		{
			var singleObjectProperties =
				typeof(T).GetProperties()
				.Where(x => x.GetCustomAttribute<RelativityObjectFieldAttribute>()?.FieldType == RdoFieldType.SingleObject);

			foreach (var propertyInfo in singleObjectProperties)
			{
				var associatedObjectsToInsert = objectsToInsert
					.Select(objectToInsert => (BaseDto)objectToInsert.GetPropertyValue(propertyInfo.Name))
					.Where(x => x != null && x.ArtifactId == 0);

				var associatedType = propertyInfo.PropertyType;

				this.InvokeGenericMethod(associatedType, nameof(Insert), BaseExtensionMethods.MakeGenericList(associatedObjectsToInsert, associatedType), recursive);
			}
		}

		private void InsertMultipleObjectFields<T>(IEnumerable<T> objectsToInsert, bool recursive) where T : BaseDto
		{
			var multipleObjectProperties =
				typeof(T).GetProperties()
				.Where(x => x.GetCustomAttribute<RelativityObjectFieldAttribute>()?.FieldType == RdoFieldType.MultipleObject);

			foreach (var propertyInfo in multipleObjectProperties)
			{
				var associatedObjectsToInsert = objectsToInsert
					.Select(objectToInsert => (IEnumerable)objectToInsert.GetPropertyValue(propertyInfo.Name))
					.Where(x => x != null)
					.SelectMany(x => x.Cast<BaseDto>())
					.Where(x => x.ArtifactId == 0);

				var associatedType = propertyInfo.PropertyType.GetEnumerableInnerType();

				this.InvokeGenericMethod(associatedType, nameof(Insert), BaseExtensionMethods.MakeGenericList(associatedObjectsToInsert, associatedType), recursive);
			}
		}

		private void Insert<T>(IList<T> theObjectsToInsert, bool recursive) where T : BaseDto 
			=> Insert(theObjectsToInsert, recursive ? ObjectFieldsDepthLevel.FullyRecursive : ObjectFieldsDepthLevel.OnlyParentObject);

		public int Insert<T>(T theObjectToInsert, ObjectFieldsDepthLevel depthLevel) where T : BaseDto
		{
			Insert<T>(new[] { theObjectToInsert }, depthLevel);
			return theObjectToInsert.ArtifactId;
		}

		public void Insert<T>(IList<T> theObjectsToInsert, ObjectFieldsDepthLevel depthLevel) where T : BaseDto
		{
			if (theObjectsToInsert.Count == 0)
			{
				return;
			}

			var parentOnly = depthLevel == ObjectFieldsDepthLevel.OnlyParentObject;
			var recursive = depthLevel == ObjectFieldsDepthLevel.FullyRecursive;

			if (!parentOnly)
			{
				InsertSingleObjectFields(theObjectsToInsert, recursive);
				InsertMultipleObjectFields(theObjectsToInsert, recursive);
			}

			ExecuteObjectInsert(theObjectsToInsert);
			InsertUpdateFileFields(theObjectsToInsert, true);

			if (!parentOnly)
			{
				InsertChildListObjects(theObjectsToInsert, recursive);
			}
		}

		private void ExecuteObjectInsert<T>(IList<T> theObjectsToInsert) where T : BaseDto
		{
			var rdos = theObjectsToInsert.Select(x => x.ToRdo()).ToList();
			var resultData = rsapiProvider.Create(rdos).GetResultData();
			for (int i = 0; i < rdos.Count; i++)
			{
				theObjectsToInsert[i].ArtifactId = resultData[i].ArtifactID;
			}
		}
	}
}
