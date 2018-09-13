using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Gravity.Base;
using Gravity.Extensions;

namespace Gravity.DAL.RSAPI
{
	public partial class RsapiDao
	{
		public void Delete<T>(int objectToDeleteId, ObjectFieldsDepthLevel depthLevel)
			where T : BaseDto, new()
		{
			var maxRecursionLevel =
				depthLevel == ObjectFieldsDepthLevel.OnlyParentObject ? 0
				: depthLevel == ObjectFieldsDepthLevel.FirstLevelOnly ? 1
				: int.MaxValue;

			//artifactID / recursion level tuple for items to delete
			var artifactsToDeleteList = new List<Tuple<int, int>>();

			//populate artifacts to delete
			PopulateArtifactsToDeleteList<T>(artifactsToDeleteList, maxRecursionLevel, new[] { objectToDeleteId }, 0);

			//order by items to delete by how deep in hierarchy they are
			//so don't run into "can't delete" issues
			var deleteSets = artifactsToDeleteList
				.ToLookup(x => x.Item2, x => x.Item1)
				.OrderByDescending(x => x.Key);

			foreach (var deleteSet in deleteSets)
			{
				rsapiProvider.Delete(deleteSet.ToList()).GetResultData();
			}

		}

		internal void PopulateArtifactsToDeleteList<T>(
			List<Tuple<int, int>> artifactsToDeleteList,
			int maxRecursionLevel,
			IList<int> artifactIds,
			int currentRecursionLevel) where T : BaseDto
		{
			if (!artifactIds.Any())
				return;

			if (currentRecursionLevel > maxRecursionLevel)
				throw new ArgumentOutOfRangeException(nameof(currentRecursionLevel), "Exceeded maximum recursion level.");

			artifactsToDeleteList.AddRange(artifactIds.Select(a => Tuple.Create(a, currentRecursionLevel)));

			var childProperties = BaseDto.GetRelativityObjectChildrenListProperties<T>();

			foreach (var propertyInfo in childProperties)
			{
				var childType = propertyInfo.PropertyType.GetEnumerableInnerType();
				foreach (var artifactId in artifactIds)
				{
					//TODO: amend so can pass in all artifact IDs instead of looping over artifacts
					var thisChildTypeIds = (List<int>)this.InvokeGenericMethod(childType, nameof(GetAllChildIds), new[] { artifactId });

					//recurse with child objects and next recursion level
					this.InvokeGenericMethod(childType, nameof(PopulateArtifactsToDeleteList),
						artifactsToDeleteList, maxRecursionLevel, thisChildTypeIds, currentRecursionLevel + 1);
				}
			}

		}
	}
}
