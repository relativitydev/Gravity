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
			rsapiProvider.DeleteSingle(artifactId);
		}

		protected void DeleteRDO(Guid artifactGuid)
		{
			rsapiProvider.DeleteSingle(artifactGuid);
		}

		protected void DeleteRDOs(List<int> artifactIds)
		{
			rsapiProvider.Delete(artifactIds)
				.GetResultData(); //ensure no exceptions
		}
		#endregion

		internal void DeleteChildObjects<T>(IList<T> parentObjectList, List<int> artifactIds) where T : BaseDto, new()
		{
			var childObjectsInfo = BaseDto.GetRelativityObjectChildrenListProperties<T>();

			foreach (var parentObject in parentObjectList)
			{
				DeleteChildObjectsInner(parentObject, childObjectsInfo);
			}

			DeleteRDOs(artifactIds);
		}

		public void DeleteRelativityObjectRecusively<T>(T theObjectToDelete) where T : BaseDto, new()
		{
			var childObjectsInfo = BaseDto.GetRelativityObjectChildrenListProperties<T>();
			DeleteChildObjectsInner(theObjectToDelete, childObjectsInfo);
			DeleteRDO(theObjectToDelete.ArtifactId);
		}

		public void DeleteRelativityObjectRecusively<T>(int objectToDeleteId) where T : BaseDto, new()
		{
			T theObjectToDelete = GetRelativityObject<T>(objectToDeleteId, Base.ObjectFieldsDepthLevel.FullyRecursive);
			DeleteRelativityObjectRecusively(theObjectToDelete);
		}

		private void DeleteChildObjectsInner<T>(T theObjectToDelete, IEnumerable<PropertyInfo> childProperties) 
			where T : BaseDto, new()
		{
			foreach (var propertyInfo in childProperties)
			{

				var thisChildTypeObj = propertyInfo.GetValue(theObjectToDelete, null) as IList;

				List<int> thisArtifactIDs = thisChildTypeObj.Cast<object>()
					.Select(item => ((BaseDto)item).ArtifactId)
					.ToList();

				if (thisArtifactIDs.Count != 0)
				{
					Type childType = propertyInfo.PropertyType.GetEnumerableInnerType();
					this.InvokeGenericMethod(childType, nameof(DeleteChildObjects), thisChildTypeObj, thisArtifactIDs);
				}
			}
		}
	}
}
