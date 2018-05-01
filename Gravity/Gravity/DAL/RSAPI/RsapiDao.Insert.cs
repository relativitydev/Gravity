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
			var resultArtifactId = InvokeProxyWithRetry(proxyToWorkspace => proxyToWorkspace.Repositories.RDO.CreateSingle(newRdo));

			if (resultArtifactId <= 0)
			{
				throw new ArgumentException("Was not able to insert new RDO with resultInt <= 0, with name " + newRdo.TextIdentifier);
			}

			return resultArtifactId;
		}

		protected void InsertUpdateFileFields(BaseDto objectToInsert, int parentId)
		{
			foreach (var propertyInfo in objectToInsert.GetType()
                .GetProperties()
                .Where(c => c.GetCustomAttribute<RelativityObjectFieldAttribute>()?.FieldType == RdoFieldType.File))
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
                UploadFile(relativityFile, parentId, relativityFile.FileValue.Path);
            }
            else if (!string.IsNullOrEmpty(relativityFile.FileMetadata.FileName))
            {
                string fileName = Path.GetTempPath() + relativityFile.FileMetadata.FileName;
                File.WriteAllBytes(fileName, relativityFile.FileValue.Data);

                try
                {
                    UploadFile(relativityFile, parentId, fileName);
                }
                finally
                {
                    invokeWithRetryService.InvokeVoidMethodWithRetry(() => File.Delete(fileName));
                }
                
            }
        }

        private void UploadFile(RelativityFile relativityFile, int parentId, string fileName)
        {
            using (IRSAPIClient proxyToWorkspace = CreateProxy())
            {
                var uploadRequest = new UploadRequest(proxyToWorkspace.APIOptions);
                uploadRequest.Metadata.FileName = fileName;
                uploadRequest.Metadata.FileSize = new FileInfo(uploadRequest.Metadata.FileName).Length;
                uploadRequest.Overwrite = true;
                uploadRequest.Target.FieldId = relativityFile.ArtifactTypeId;
                uploadRequest.Target.ObjectArtifactId = parentId;
                InvokeProxyWithRetry(proxyToWorkspace, proxy => proxy.Upload(uploadRequest));
            }
        }


        private void InsertChildListObjectsWithDynamicType(BaseDto theObjectToInsert, int resultArtifactId, PropertyInfo propertyInfo)
        {
			var childObjectsList = propertyInfo.GetValue(theObjectToInsert, null) as IList;

            if (childObjectsList?.Count != 0)
            {
				var childType = propertyInfo.PropertyType.GetEnumerableInnerType();
				this.InvokeGenericMethod(childType, nameof(InsertChildListObjects), childObjectsList, resultArtifactId);
            }
        }

        private static void SetParentArtifactID<T>(T objectToBeInserted, int parentArtifactId) where T : BaseDto, new()
		{
			PropertyInfo parentArtifactIdProperty = objectToBeInserted.GetParentArtifactIdProperty();
			PropertyInfo ArtifactIdProperty = objectToBeInserted.GetType().GetProperty("ArtifactId");

			if (parentArtifactIdProperty == null)
			{
				return;
			}

			parentArtifactIdProperty.SetValue(objectToBeInserted, parentArtifactId);
			ArtifactIdProperty.SetValue(objectToBeInserted, 0);
		}
		#endregion

		public void InsertChildListObjects<T>(IList<T> objectsToInserted, int parentArtifactId)
			where T : BaseDto, new()
		{
			var childObjectsInfo = BaseDto.GetRelativityObjectChildrenListProperties<T>();

			bool isFilePropertyPresent = typeof(T).GetProperties().ToList().Any(c => c.DeclaringType.IsAssignableFrom(typeof(RelativityFile)));

			if (childObjectsInfo.Any() || isFilePropertyPresent)
			{
				foreach (var objectToBeInserted in objectsToInserted)
				{
					SetParentArtifactID(objectToBeInserted, parentArtifactId);
					int insertedRdoArtifactID = InsertRdo(objectToBeInserted.ToRdo());
					InsertUpdateFileFields(objectToBeInserted, insertedRdoArtifactID);

					foreach (var childPropertyInfo in childObjectsInfo)
					{
						InsertChildListObjectsWithDynamicType(objectToBeInserted, insertedRdoArtifactID, childPropertyInfo);
					}
				}
			}
			else
			{

				foreach (var objectToBeInserted in objectsToInserted)
				{
					SetParentArtifactID(objectToBeInserted, parentArtifactId);
				}

				var rdosToBeInserted = objectsToInserted.Select(x => x.ToRdo()).ToArray();

				InvokeProxyWithRetry(proxyToWorkspace => proxyToWorkspace.Repositories.RDO.Create(rdosToBeInserted));
			}			
		}

        public int InsertRelativityObject<T>(BaseDto theObjectToInsert)
		{

			int resultArtifactId = InsertRdo(theObjectToInsert.ToRdo());

			InsertUpdateFileFields(theObjectToInsert, resultArtifactId);

			var childObjectsInfo = BaseDto.GetRelativityObjectChildrenListProperties<T>();
			foreach (var childPropertyInfo in childObjectsInfo)
            {
                InsertChildListObjectsWithDynamicType(theObjectToInsert, resultArtifactId, childPropertyInfo);
            }

            return resultArtifactId;
		}
    }
}
