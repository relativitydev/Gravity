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


		private void InsertChildListObjects<T>(T theObjectToInsert, int resultArtifactId) where T : BaseDto
		{

			var childObjectsInfo = BaseDto.GetRelativityObjectChildrenListProperties<T>();
			foreach (var childPropertyInfo in childObjectsInfo)
			{
				var childObjectsList = childPropertyInfo.GetValue(theObjectToInsert, null) as IList;

				if (childObjectsList?.Count > 0)
				{
					foreach (var childObject in childObjectsList)
					{
						var parentArtifactIdProperty = ((BaseDto)childObject).GetParentArtifactIdProperty();
						parentArtifactIdProperty.SetValue(childObject, resultArtifactId);
						//TODO: bulk operation if no recursion
						this.InvokeGenericMethod(childObject.GetType(), nameof(Insert), childObject);
					}
				}
			}
		}

		private bool InsertSingleObjectFields(BaseDto objectToInsert)
		{
			foreach (var propertyInfo in objectToInsert.GetType().GetProperties())
			{
				var attribute = propertyInfo.GetCustomAttribute<RelativityObjectFieldAttribute>();
				if (attribute?.FieldType == RdoFieldType.SingleObject)
				{
					var fieldValue = (BaseDto)objectToInsert.GetPropertyValue(propertyInfo.Name);
					if (fieldValue != null)
					{
						this.InvokeGenericMethod(fieldValue.GetType(), nameof(Insert), fieldValue);
					}
				}
			}
			return true;
		}

		private bool InsertMultipleObjectFields(BaseDto objectToInsert)
		{
			foreach (var propertyInfo in objectToInsert.GetType().GetProperties().Where(c =>
				c.GetCustomAttribute<RelativityObjectFieldAttribute>()?.FieldType == RdoFieldType.MultipleObject))
			{
				IEnumerable<object> fieldValue = (IEnumerable<object>)objectToInsert.GetPropertyValue(propertyInfo.Name);
				if (fieldValue == null)
				{
					continue;
				}

				foreach (var childObject in fieldValue)
				{
					if ((childObject as BaseDto).ArtifactId == 0)
					{
						//TODO: bulk operation if no recursion
						this.InvokeGenericMethod(childObject.GetType(), nameof(Insert), childObject);
					}
				}
			}
			return true;
		}

		public int Insert<T>(T theObjectToInsert, ObjectFieldsDepthLevel depthLevel) where T : BaseDto
		{
			
			//TODO: should think about some sort of transaction type around this.  If any parts of this fail, it should all fail
			
			InsertSingleObjectFields(theObjectToInsert);
			InsertMultipleObjectFields(theObjectToInsert);

			int resultArtifactId = InsertRdo(theObjectToInsert.ToRdo());
			theObjectToInsert.ArtifactId = resultArtifactId;

			InsertUpdateFileFields(theObjectToInsert, resultArtifactId);
			InsertChildListObjects(theObjectToInsert, resultArtifactId);

			return resultArtifactId;
		}
	}
}
