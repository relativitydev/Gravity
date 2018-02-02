using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Gravity.Base;
using Gravity.Exceptions;
using Gravity.Extensions;

namespace Gravity.DAL.RSAPI
{
	public partial class RsapiDao
	{
		#region RDO DELETE Protected stuff
		protected void DeleteRDO(int artifactId)
		{
			InvokeProxyWithRetry(proxyToWorkspace => proxyToWorkspace.Repositories.RDO.DeleteSingle(artifactId));
		}

		protected void DeleteRDO(Guid artifactGuid)
		{
			InvokeProxyWithRetry(proxyToWorkspace => proxyToWorkspace.Repositories.RDO.DeleteSingle(artifactGuid));
		}

		protected void DeleteRDOs(List<int> artifactIds)
		{
			InvokeProxyWithRetry(proxyToWorkspace => proxyToWorkspace.Repositories.RDO.Delete(artifactIds))
				.GetResultData(); //ensure no exceptions
		}
		#endregion

		internal void DeleteChildObjects<T>(IList<T> parentObjectList, List<int> artifactIds) where T : BaseDto, new()
		{
			var childObjectsInfo = BaseDto.GetRelativityObjectChildrenListInfos<T>();

			foreach (var parentObject in parentObjectList)
			{
				DeleteChildObjectsInner(parentObject, childObjectsInfo);
			}

			DeleteRDOs(artifactIds);
		}

		public void DeleteRelativityObjectRecusively<T>(T theObjectToDelete) where T : BaseDto, new()
		{
			var childObjectsInfo = BaseDto.GetRelativityObjectChildrenListInfos<T>();
			DeleteChildObjectsInner(theObjectToDelete, childObjectsInfo);
			DeleteRDO(theObjectToDelete.ArtifactId);
		}

		public void DeleteRelativityObjectRecusively<T>(int objectToDeleteId) where T : BaseDto, new()
		{
			T theObjectToDelete = GetRelativityObject<T>(objectToDeleteId, Base.ObjectFieldsDepthLevel.FullyRecursive);
			DeleteRelativityObjectRecusively(theObjectToDelete);
		}

		private void DeleteChildObjectsInner<T>(
				T theObjectToDelete, 
				IEnumerable<KeyValuePair<PropertyInfo, RelativityObjectChildrenListAttribute>> childObjectsInfo) 
			where T : BaseDto, new()
		{
			foreach (var childPropertyInfo in childObjectsInfo)
			{
				PropertyInfo propertyInfo = childPropertyInfo.Key;
				RelativityObjectChildrenListAttribute theChildAttribute = childPropertyInfo.Value;

				Type childType = childPropertyInfo.Value.ChildType;

				var thisChildTypeObj = propertyInfo.GetValue(theObjectToDelete, null) as IList;

				List<int> thisArtifactIDs = thisChildTypeObj.Cast<object>()
					.Select(item => (int)item.GetType().GetProperty("ArtifactId").GetValue(item, null))
					.ToList();

				if (thisArtifactIDs.Count != 0)
				{
					this.InvokeGenericMethod(childType, "DeleteChildObjects", thisChildTypeObj, thisArtifactIDs);
				}
			}
		}
	}
}
