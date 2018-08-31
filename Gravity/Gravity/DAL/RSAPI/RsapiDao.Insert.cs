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
		protected int InsertRdo(RDO newRdo)
		{
			var resultArtifactId = rsapiProvider.CreateSingle(newRdo);

			if (resultArtifactId <= 0)
			{
				throw new ArgumentException("Was not able to insert new RDO with resultInt <= 0, with name " + newRdo.TextIdentifier);
			}

			return resultArtifactId;
		}

		protected void InsertUpdateFileFields(BaseDto objectToInsert)
		{
			foreach (var propertyInfo in objectToInsert.GetType().GetProperties().Where(c => c.GetCustomAttribute<RelativityObjectFieldAttribute>()?.FieldType == RdoFieldType.File))
			{
				RelativityFile relativityFile = propertyInfo.GetValue(objectToInsert) as RelativityFile;
				InsertUpdateFileField(relativityFile, objectToInsert.ArtifactId);
			}
		}

		protected void InsertUpdateFileField(RelativityFile relativityFile, int parentId)
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

		private int Insert<T>(T theObjectToInsert, bool recursive) where T : BaseDto 
			=> Insert(theObjectToInsert, recursive ? ObjectFieldsDepthLevel.FullyRecursive : ObjectFieldsDepthLevel.OnlyParentObject);

		public int Insert<T>(T theObjectToInsert, ObjectFieldsDepthLevel depthLevel) where T : BaseDto
		{
			var parentOnly = depthLevel == ObjectFieldsDepthLevel.OnlyParentObject;
			var recursive = depthLevel == ObjectFieldsDepthLevel.FullyRecursive;

			//TODO: should think about some sort of transaction type around this.  If any parts of this fail, it should all fail
			if (!parentOnly)
			{
				InsertSingleObjectFields(new[] { theObjectToInsert }, recursive);
				InsertMultipleObjectFields(new[] { theObjectToInsert }, recursive);
			}

			int resultArtifactId = InsertRdo(theObjectToInsert.ToRdo());
			theObjectToInsert.ArtifactId = resultArtifactId;

			if (!parentOnly)
			{
				InsertUpdateFileFields(theObjectToInsert);
				InsertChildListObjects(new[] { theObjectToInsert }, recursive);
			}

			return resultArtifactId;
		}

		public void Insert<T>(IList<T> theObjectsToInsert, bool recursive) where T : BaseDto
		{
			if (theObjectsToInsert.Count == 0)
			{
				return;
			}

			if (recursive)
			{
				InsertSingleObjectFields(theObjectsToInsert, recursive);
				InsertMultipleObjectFields(theObjectsToInsert, recursive);
			}

			var rdos = theObjectsToInsert.Select(x => x.ToRdo()).ToList();
			var resultData = rsapiProvider.Create(rdos).GetResultData();
			for (int i = 0; i < rdos.Count; i++)
			{
				theObjectsToInsert[i].ArtifactId = resultData[i].ArtifactID;
			}

			if (recursive)
			{
				InsertChildListObjects(theObjectsToInsert, recursive);

				foreach (var theObjectToInsert in theObjectsToInsert)
				{
					InsertUpdateFileFields(theObjectToInsert);
				}
			}
		}
	}
}
