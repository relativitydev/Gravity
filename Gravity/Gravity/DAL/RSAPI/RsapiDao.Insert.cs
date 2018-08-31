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

		private void InsertUpdateFileFields(BaseDto objectToInsert)
		{
			foreach (var propertyInfo in objectToInsert.GetType().GetProperties().Where(c => c.GetCustomAttribute<RelativityObjectFieldAttribute>()?.FieldType == RdoFieldType.File))
			{
				RelativityFile relativityFile = propertyInfo.GetValue(objectToInsert) as RelativityFile;
				InsertUpdateFileField(relativityFile, objectToInsert.ArtifactId);
			}
		}

		private void InsertUpdateFileField(RelativityFile relativityFile, int parentId)
		{
			if (relativityFile?.FileValue == null)
			{
				return;
			}

			if (relativityFile.FileValue.Path != null)
			{
				rsapiProvider.UploadFile(relativityFile, parentId, relativityFile.FileValue.Path);
			}
			else if (!string.IsNullOrEmpty(relativityFile.FileMetadata.FileName))
			{
				string fileName = Path.GetTempPath() + relativityFile.FileMetadata.FileName;
				File.WriteAllBytes(fileName, relativityFile.FileValue.Data);

				try
				{
					rsapiProvider.UploadFile(relativityFile, parentId, fileName);
				}
				finally
				{
					invokeWithRetryService.InvokeVoidMethodWithRetry(() => File.Delete(fileName));
				}

			}
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

				this.InvokeGenericMethod(childType, nameof(Insert), MakeGenericList(childObjectsToInsert, childType), recursive);
			}
		}

		private void InsertSingleObjectFields<T>(IEnumerable<T> objectsToInsert, bool recursive) where T : BaseDto
		{
			var singleObjectProperties =
				typeof(T).GetProperties()
				.Where(x => x.GetCustomAttribute<RelativityObjectFieldAttribute>()?.FieldType == RdoFieldType.SingleObject);

			foreach (var propertyInfo in singleObjectProperties)
			{
				var childObjectsToInsert = objectsToInsert
					.Select(objectToInsert => (BaseDto)objectToInsert.GetPropertyValue(propertyInfo.Name))
					.Where(x => x != null && x.ArtifactId == 0);

				var childType = propertyInfo.PropertyType;

				this.InvokeGenericMethod(childType, nameof(Insert), MakeGenericList(childObjectsToInsert, childType), recursive);
			}
		}

		private void InsertMultipleObjectFields<T>(IEnumerable<T> objectsToInsert, bool recursive) where T : BaseDto
		{
			var multipleObjectProperties =
				typeof(T).GetProperties()
				.Where(x => x.GetCustomAttribute<RelativityObjectFieldAttribute>()?.FieldType == RdoFieldType.MultipleObject);

			foreach (var propertyInfo in multipleObjectProperties)
			{
				var childObjectsToInsert = objectsToInsert
					.Select(objectToInsert => (IEnumerable)objectToInsert.GetPropertyValue(propertyInfo.Name))
					.Where(x => x != null)
					.SelectMany(x => x.Cast<BaseDto>())
					.Where(x => x.ArtifactId == 0);

				var childType = propertyInfo.PropertyType.GetEnumerableInnerType();

				this.InvokeGenericMethod(childType, nameof(Insert), MakeGenericList(childObjectsToInsert, childType), recursive);
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

			if (!parentOnly)
			{
				InsertChildListObjects(theObjectsToInsert, recursive);

				foreach (var theObjectToInsert in theObjectsToInsert)
				{
					InsertUpdateFileFields(theObjectToInsert);
				}
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
