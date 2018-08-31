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

		protected void InsertUpdateFileFields(BaseDto objectToInsert, int parentId)
		{
			foreach (var propertyInfo in objectToInsert.GetType().GetProperties().Where(c => c.GetCustomAttribute<RelativityObjectFieldAttribute>()?.FieldType == RdoFieldType.File))
			{
				RelativityFile relativityFile = propertyInfo.GetValue(objectToInsert) as RelativityFile;
				InsertUpdateFileField(relativityFile, parentId);
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


		private void InsertChildListObjects(BaseDto theObjectToInsert, int resultArtifactId, bool recursive)
		{
			var childObjectsInfo = theObjectToInsert.GetType()
				.GetPropertyAttributeTuples<RelativityObjectChildrenListAttribute>()
				.Select(x => x.Item1);
			foreach (var childPropertyInfo in childObjectsInfo)
			{
				var childType = childPropertyInfo.PropertyType.GetEnumerableInnerType();
				var childObjectsList = childPropertyInfo.GetValue(theObjectToInsert, null) as IList;
				if (childObjectsList == null)
					continue;

				foreach (var childObject in childObjectsList)
				{
					var parentArtifactIdProperty = ((BaseDto)childObject).GetParentArtifactIdProperty();
					parentArtifactIdProperty.SetValue(childObject, resultArtifactId);					
				}

				this.InvokeGenericMethod(childType, nameof(Insert), childObjectsList, recursive);
			}
		}

		private bool InsertSingleObjectFields(BaseDto objectToInsert, bool recursive)
		{
			foreach (var propertyInfo in objectToInsert.GetType().GetProperties())
			{
				var childType = propertyInfo.PropertyType;
				var attribute = propertyInfo.GetCustomAttribute<RelativityObjectFieldAttribute>();
				if (attribute?.FieldType == RdoFieldType.SingleObject)
				{
					var fieldValue = (BaseDto)objectToInsert.GetPropertyValue(propertyInfo.Name);
					if (fieldValue?.ArtifactId == 0)
					{
						this.InvokeGenericMethod(childType, nameof(Insert), fieldValue, recursive);
					}
				}
			}
			return true;
		}

		private bool InsertMultipleObjectFields(BaseDto objectToInsert, bool recursive)
		{
			foreach (var propertyInfo in objectToInsert.GetType().GetProperties().Where(c =>
				c.GetCustomAttribute<RelativityObjectFieldAttribute>()?.FieldType == RdoFieldType.MultipleObject))
			{
				var childType = propertyInfo.PropertyType.GetEnumerableInnerType();
				var fieldValue = ((IEnumerable)objectToInsert.GetPropertyValue(propertyInfo.Name))?
					.Cast<BaseDto>()
					.Where(x => x.ArtifactId == 0);

				if (fieldValue == null)
				{
					continue;
				}

				this.InvokeGenericMethod(childType, nameof(Insert), MakeGenericList(fieldValue, childType), recursive);
			}
			return true;
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
				InsertSingleObjectFields(theObjectToInsert, recursive);
				InsertMultipleObjectFields(theObjectToInsert, recursive);
			}

			int resultArtifactId = InsertRdo(theObjectToInsert.ToRdo());
			theObjectToInsert.ArtifactId = resultArtifactId;

			if (!parentOnly)
			{
				InsertUpdateFileFields(theObjectToInsert, resultArtifactId);
				InsertChildListObjects(theObjectToInsert, resultArtifactId, recursive);
			}

			return resultArtifactId;
		}

		public void Insert<T>(IList<T> theObjectsToInsert, bool recursive) where T : BaseDto
		{
			if (theObjectsToInsert.Count == 0)
			{
				return;
			}

			foreach (var theObjectToInsert in theObjectsToInsert)
			{
				if (recursive)
				{
					InsertSingleObjectFields(theObjectToInsert, recursive);
					InsertMultipleObjectFields(theObjectToInsert, recursive);
				}
			}

			var rdos = theObjectsToInsert.Select(x => x.ToRdo()).ToList();
			var resultData = rsapiProvider.Create(rdos).GetResultData();
			for (int i = 0; i < rdos.Count; i++)
			{
				theObjectsToInsert[i].ArtifactId = resultData[i].ArtifactID;
			}

			foreach (var theObjectToInsert in theObjectsToInsert)
			{
				if (recursive)
				{
					var resultArtifactId = theObjectToInsert.ArtifactId;
					InsertUpdateFileFields(theObjectToInsert, resultArtifactId);
					InsertChildListObjects(theObjectToInsert, resultArtifactId, recursive);
				}
			}
		}
	}
}
