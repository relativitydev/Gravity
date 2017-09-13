using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Gravity.Base;
using Gravity.Exceptions;

namespace Gravity.DAL.RSAPI
{
	public partial class RsapiDao
	{
		#region RDO DELETE Protected stuff
		protected void DeleteRDO(int artifactId)
		{
			using (var proxyToWorkspace = CreateProxy())
			{
				try
				{
					invokeWithRetryService.InvokeVoidMethodWithRetry(() => proxyToWorkspace.Repositories.RDO.DeleteSingle(artifactId));
				}
				catch (Exception ex)
				{
					throw new ProxyOperationFailedException("Failed in method: " + System.Reflection.MethodInfo.GetCurrentMethod(), ex);
				}
			}
		}

		protected void DeleteRDO(Guid artifactGuid)
		{
			using (var proxyToWorkspace = CreateProxy())
			{
				try
				{
					invokeWithRetryService.InvokeVoidMethodWithRetry(() => proxyToWorkspace.Repositories.RDO.DeleteSingle(artifactGuid));
				}
				catch (Exception ex)
				{
					throw new ProxyOperationFailedException("Failed in method: " + System.Reflection.MethodInfo.GetCurrentMethod(), ex);
				}
			}
		}

		protected void DeleteRDOs(List<int> artifactIds)
		{
			using (var proxyToWorkspace = CreateProxy())
			{
				try
				{
					invokeWithRetryService.InvokeVoidMethodWithRetry(() => proxyToWorkspace.Repositories.RDO.Delete(artifactIds));
				}
				catch (Exception ex)
				{
					throw new ProxyOperationFailedException("Failed in method: " + System.Reflection.MethodInfo.GetCurrentMethod(), ex);
				}
			}
		}
		#endregion

		internal void DeleteChildObjects<T>(IList<T> parentObjectList, List<int> artifactIds) where T : BaseDto, new()
		{
			Dictionary<PropertyInfo, RelativityObjectChildrenListAttribute> childObjectsInfo = BaseDto.GetRelativityObjectChildrenListInfos<T>();

			if (childObjectsInfo.Count == 0)
			{
				DeleteRDOs(artifactIds);
			}
			else
			{
				foreach (var parentObject in parentObjectList)
				{
					foreach (var childPropertyInfo in childObjectsInfo)
					{
						var propertyInfo = childPropertyInfo.Key;
						var theChildAttribute = childPropertyInfo.Value;

						Type childType = childPropertyInfo.Value.ChildType;

						var thisChildTypeObj = propertyInfo.GetValue(parentObject, null) as IList;

						List<int> thisArtifactIDs = new List<int>();

						foreach (var item in thisChildTypeObj)
						{
							thisArtifactIDs.Add((int)item.GetType().GetProperty("ArtifactId").GetValue(item, null));
						}

						if (thisArtifactIDs.Count != 0)
						{
							MethodInfo method = GetType().GetMethod("DeleteChildObjects", BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(new Type[] { childType });

							method.Invoke(this, new object[] { thisChildTypeObj, thisArtifactIDs });
						}
					}
				}

				DeleteRDOs(artifactIds);
			}
		}

		public void DeleteRelativityObjectRecusively<T>(T theObjectToDelete) where T : BaseDto, new()
		{
			Dictionary<PropertyInfo, RelativityObjectChildrenListAttribute> childObjectsInfo = BaseDto.GetRelativityObjectChildrenListInfos<T>();

			if (childObjectsInfo.Count == 0)
			{
				DeleteRDO(theObjectToDelete.ArtifactId);
			}
			else
			{
				foreach (var childPropertyInfo in childObjectsInfo)
				{
					PropertyInfo propertyInfo = childPropertyInfo.Key;
					RelativityObjectChildrenListAttribute theChildAttribute = childPropertyInfo.Value;

					Type childType = childPropertyInfo.Value.ChildType;

					var thisChildTypeObj = propertyInfo.GetValue(theObjectToDelete, null) as IList;

					List <int> thisArtifactIDs= new List<int>();

					foreach (var item in thisChildTypeObj)
					{
						thisArtifactIDs.Add((int)item.GetType().GetProperty("ArtifactId").GetValue(item, null));
					}

					if (thisArtifactIDs.Count != 0)
					{
						MethodInfo method = GetType().GetMethod("DeleteChildObjects", BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(new Type[] { childType });

						method.Invoke(this, new object[] { thisChildTypeObj, thisArtifactIDs });
					}
				}

				DeleteRDO(theObjectToDelete.ArtifactId);
			}
		}

		public void DeleteRelativityObjectRecusively<T>(int objectToDeleteId) where T : BaseDto, new()
		{
			T theObjectToDelete = GetRelativityObject<T>(objectToDeleteId, Base.ObjectFieldsDepthLevel.FullyRecursive);

			Dictionary<PropertyInfo, RelativityObjectChildrenListAttribute> childObjectsInfo = BaseDto.GetRelativityObjectChildrenListInfos<T>();

			if (childObjectsInfo.Count == 0)
			{
				DeleteRDO(theObjectToDelete.ArtifactId);
			}
			else
			{
				foreach (var childPropertyInfo in childObjectsInfo)
				{
					PropertyInfo propertyInfo = childPropertyInfo.Key;
					RelativityObjectChildrenListAttribute theChildAttribute = childPropertyInfo.Value;

					Type childType = childPropertyInfo.Value.ChildType;

					var thisChildTypeObj = propertyInfo.GetValue(theObjectToDelete, null) as IList;

					List<int> thisArtifactIDs = new List<int>();

					foreach (var item in thisChildTypeObj)
					{
						thisArtifactIDs.Add((int)item.GetType().GetProperty("ArtifactId").GetValue(item, null));
					}

					if (thisArtifactIDs.Count != 0)
					{
						MethodInfo method = GetType().GetMethod("DeleteChildObjects", BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(new Type[] { childType });

						method.Invoke(this, new object[] { thisChildTypeObj, thisArtifactIDs });
					}
				}

				DeleteRDO(theObjectToDelete.ArtifactId);
			}
		}
	}
}
