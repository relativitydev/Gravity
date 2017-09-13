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

namespace Gravity.DAL.RSAPI
{
	public partial class RsapiDao
	{
		#region RDO INSERT Protected Stuff
		protected int InsertRdo(RDO newRdo)
		{
			int resultArtifactId = 0;
			using (var proxyToWorkspace = CreateProxy())
			{
				try
				{
					resultArtifactId = invokeWithRetryService.InvokeWithRetry(() => proxyToWorkspace.Repositories.RDO.CreateSingle(newRdo));
				}
				catch (Exception ex)
				{
					throw new ProxyOperationFailedException("Failed in method: " + System.Reflection.MethodInfo.GetCurrentMethod(), ex);
				}
			}

			if (resultArtifactId <= 0)
			{
				throw new ArgumentException("Was not able to insert new RDO with resultInt <= 0, with name " + newRdo.TextIdentifier);
			}

			return resultArtifactId;
		}

		protected WriteResultSet<RDO> InsertRdos(params RDO[] newRdos)
		{
			WriteResultSet<RDO> resultSet = new WriteResultSet<RDO>();

			using (var proxyToWorkspace = CreateProxy())
			{
				try
				{
					resultSet = invokeWithRetryService.InvokeWithRetry(() => proxyToWorkspace.Repositories.RDO.Create(newRdos));
				}
				catch (Exception ex)
				{
					throw new ProxyOperationFailedException("Failed in method: " + System.Reflection.MethodInfo.GetCurrentMethod(), ex);
				}
			}

			return resultSet;
		}

		protected void InsertUpdateFileField(BaseDto objectToInsert, int parentId)
		{
			foreach (var propertyInfo in objectToInsert.GetType().GetProperties().Where(c => c.GetCustomAttribute<RelativityObjectFieldAttribute>() != null))
			{
				RelativityObjectFieldAttribute attributeValue = propertyInfo.GetCustomAttribute<RelativityObjectFieldAttribute>();
				if (attributeValue.FieldType == (int)RdoFieldType.File)
				{
					RelativityFile relativityFile = propertyInfo.GetValue(objectToInsert) as RelativityFile;
					if (relativityFile != null)
					{
						if (relativityFile.FileValue != null)
						{
							if (relativityFile.FileValue.Path != null)
							{
								using (IRSAPIClient proxyToWorkspace = CreateProxy())
								{
									var uploadRequest = new UploadRequest(proxyToWorkspace.APIOptions);
									uploadRequest.Metadata.FileName = relativityFile.FileValue.Path;
									uploadRequest.Metadata.FileSize = new FileInfo(uploadRequest.Metadata.FileName).Length;
									uploadRequest.Overwrite = true;
									uploadRequest.Target.FieldId = relativityFile.ArtifactTypeId;
									uploadRequest.Target.ObjectArtifactId = parentId;

									try
									{
										invokeWithRetryService.InvokeVoidMethodWithRetry(() => proxyToWorkspace.Upload(uploadRequest));
									}
									catch (Exception ex)
									{
										throw ex;
									}
								}
							}
							else if (string.IsNullOrEmpty(relativityFile.FileMetadata.FileName) == false)
							{
								string tempPath = Path.GetTempPath();
								string fileName = tempPath + relativityFile.FileMetadata.FileName;

								using (IRSAPIClient proxyToWorkspace = CreateProxy())
								{
									System.IO.File.WriteAllBytes(fileName, relativityFile.FileValue.Data);

									var uploadRequest = new UploadRequest(proxyToWorkspace.APIOptions);
									uploadRequest.Metadata.FileName = fileName;
									uploadRequest.Metadata.FileSize = new FileInfo(uploadRequest.Metadata.FileName).Length;
									uploadRequest.Overwrite = true;
									uploadRequest.Target.FieldId = relativityFile.ArtifactTypeId;
									uploadRequest.Target.ObjectArtifactId = parentId;

									try
									{
										invokeWithRetryService.InvokeVoidMethodWithRetry(() => proxyToWorkspace.Upload(uploadRequest));

										invokeWithRetryService.InvokeVoidMethodWithRetry(() => System.IO.File.Delete(fileName));
									}
									catch (Exception)
									{
										invokeWithRetryService.InvokeVoidMethodWithRetry(() => System.IO.File.Delete(fileName));
									}
								}
							}
						}
					}
				}
			}
		}

		protected void InsertUpdateFileField(RelativityFile relativityFile, int parentId)
		{
			if (relativityFile != null)
			{
				if (relativityFile.FileValue != null)
				{
					if (relativityFile.FileValue.Path != null)
					{
						using (IRSAPIClient proxyToWorkspace = CreateProxy())
						{
							var uploadRequest = new UploadRequest(proxyToWorkspace.APIOptions);
							uploadRequest.Metadata.FileName = relativityFile.FileValue.Path;
							uploadRequest.Metadata.FileSize = new FileInfo(uploadRequest.Metadata.FileName).Length;
							uploadRequest.Overwrite = true;
							uploadRequest.Target.FieldId = relativityFile.ArtifactTypeId;
							uploadRequest.Target.ObjectArtifactId = parentId;

							try
							{
								invokeWithRetryService.InvokeVoidMethodWithRetry(() => proxyToWorkspace.Upload(uploadRequest));
							}
							catch (Exception ex)
							{
								throw ex;
							}
						}
					}
					else if (string.IsNullOrEmpty(relativityFile.FileMetadata.FileName) == false)
					{
						string tempPath = Path.GetTempPath();
						string fileName = tempPath + relativityFile.FileMetadata.FileName;

						using (IRSAPIClient proxyToWorkspace = CreateProxy())
						{
							System.IO.File.WriteAllBytes(fileName, relativityFile.FileValue.Data);

							var uploadRequest = new UploadRequest(proxyToWorkspace.APIOptions);
							uploadRequest.Metadata.FileName = fileName;
							uploadRequest.Metadata.FileSize = new FileInfo(uploadRequest.Metadata.FileName).Length;
							uploadRequest.Overwrite = true;
							uploadRequest.Target.FieldId = relativityFile.ArtifactTypeId;
							uploadRequest.Target.ObjectArtifactId = parentId;

							try
							{
								invokeWithRetryService.InvokeVoidMethodWithRetry(() => proxyToWorkspace.Upload(uploadRequest));

								invokeWithRetryService.InvokeVoidMethodWithRetry(() => System.IO.File.Delete(fileName));
							}
							catch (Exception)
							{
								invokeWithRetryService.InvokeVoidMethodWithRetry(() => System.IO.File.Delete(fileName));
							}
						}
					}
				}
			}
		}
		#endregion

		public void InsertChildListObjects<T>(IList<T> objectsToInserted, int parentArtifactId)
			where T : BaseDto, new()
		{
			Dictionary<PropertyInfo, RelativityObjectChildrenListAttribute> childObjectsInfo = BaseDto.GetRelativityObjectChildrenListInfos<T>();
			if (childObjectsInfo.Count == 0)
			{
				bool isFilePropertyPresent = typeof(T).GetProperties().ToList().Where(c => c.DeclaringType.IsAssignableFrom(typeof(RelativityFile))).Count() > 0;
				if (isFilePropertyPresent == false)
				{
					List<RDO> rdosToBeInserted = new List<RDO>();

					foreach (var objectToBeInserted in objectsToInserted)
					{
						PropertyInfo parentArtifactIdProperty = objectToBeInserted.GetParentArtifactIdProperty();
						PropertyInfo ArtifactIdProperty = objectToBeInserted.GetType().GetProperty("ArtifactId");

						if (parentArtifactIdProperty != null)
						{
							parentArtifactIdProperty.SetValue(objectToBeInserted, parentArtifactId);
							ArtifactIdProperty.SetValue(objectToBeInserted, 0);
						}

						rdosToBeInserted.Add(objectToBeInserted.ToRdo());
					}

					InsertRdos(rdosToBeInserted.ToArray());
				}
				else
				{
					foreach (var objectToBeInserted in objectsToInserted)
					{
						PropertyInfo parentArtifactIdProperty = objectToBeInserted.GetParentArtifactIdProperty();
						PropertyInfo ArtifactIdProperty = objectToBeInserted.GetType().GetProperty("ArtifactId");

						if (parentArtifactIdProperty != null)
						{
							parentArtifactIdProperty.SetValue(objectToBeInserted, parentArtifactId);
							ArtifactIdProperty.SetValue(objectToBeInserted, 0);
						}

						int insertedRdoArtifactID = InsertRdo(objectToBeInserted.ToRdo());
						InsertUpdateFileField(objectToBeInserted, insertedRdoArtifactID);
					}
				}
			}
			else
			{
				foreach (var objectToBeInserted in objectsToInserted)
				{
					PropertyInfo parentArtifactIdProperty = objectToBeInserted.GetParentArtifactIdProperty();
					PropertyInfo ArtifactIdProperty = objectToBeInserted.GetType().GetProperty("ArtifactId");

					if (parentArtifactIdProperty != null)
					{
						parentArtifactIdProperty.SetValue(objectToBeInserted, parentArtifactId);
						ArtifactIdProperty.SetValue(objectToBeInserted, 0);
					}

					int insertedRdoArtifactID = InsertRdo(objectToBeInserted.ToRdo());
					InsertUpdateFileField(objectToBeInserted, insertedRdoArtifactID);

					foreach (var childPropertyInfo in childObjectsInfo)
					{
						var propertyInfo = childPropertyInfo.Key;
						var theChildAttribute = childPropertyInfo.Value;

						Type childType = childPropertyInfo.Value.ChildType;

						var childObjectsList = childPropertyInfo.Key.GetValue(objectToBeInserted, null) as IList;

						if (childObjectsList != null && childObjectsList.Count != 0)
						{
							MethodInfo method = GetType().GetMethod("InsertChildListObjects", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(new Type[] { childType });

							method.Invoke(this, new object[] { childObjectsList, insertedRdoArtifactID });
						}
					}
				}
			}
		}

		public int InsertRelativityObject<T>(BaseDto theObjectToInsert)
		{
			RDO rdo = theObjectToInsert.ToRdo();

			int resultArtifactId = InsertRdo(rdo);

			InsertUpdateFileField(theObjectToInsert, resultArtifactId);

			Dictionary<PropertyInfo, RelativityObjectChildrenListAttribute> childObjectsInfo = BaseDto.GetRelativityObjectChildrenListInfos<T>();
			if (childObjectsInfo.Count != 0)
			{
				foreach (var childPropertyInfo in childObjectsInfo)
				{
					var propertyInfo = childPropertyInfo.Key;
					var theChildAttribute = childPropertyInfo.Value;

					Type childType = childPropertyInfo.Value.ChildType;

					var childObjectsList = childPropertyInfo.Key.GetValue(theObjectToInsert, null) as IList;

					if (childObjectsList != null && childObjectsList.Count != 0)
					{
						MethodInfo method = GetType().GetMethod("InsertChildListObjects", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(new Type[] { childType });

						method.Invoke(this, new object[] { childObjectsList, resultArtifactId });
					}
				}
			}

			return resultArtifactId;
		}
	}
}
