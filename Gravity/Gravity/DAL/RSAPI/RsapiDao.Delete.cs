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
			var scanRecursionLevel =
				depthLevel == ObjectFieldsDepthLevel.OnlyParentObject
					? ObjectFieldsDepthLevel.FirstLevelOnly //won't need to look past this to throw error
					: ObjectFieldsDepthLevel.FullyRecursive;
			T theObjectToDelete = Get<T>(objectToDeleteId, scanRecursionLevel);

			var maxRecursionLevel =
				depthLevel == ObjectFieldsDepthLevel.OnlyParentObject ? 0
				: depthLevel == ObjectFieldsDepthLevel.FirstLevelOnly ? 1
				: int.MaxValue;

			//artifactID / recursion level tuple for items to delete
			var artifactsToDeleteList = new List<Tuple<int, int>>();

			//populate artifacts to delete
			PopulateArtifactsToDeleteList(artifactsToDeleteList, maxRecursionLevel, new[] { theObjectToDelete }, 0);

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
			IList<T> artifacts,
			int currentRecursionLevel) where T : BaseDto
		{
			if (!artifacts.Any())
				return;

			if (currentRecursionLevel > maxRecursionLevel)
				throw new ArgumentOutOfRangeException(nameof(maxRecursionLevel), "Exceeded maximum recursion level.");

			artifactsToDeleteList.AddRange(artifacts.Select(a => Tuple.Create(a.ArtifactId, currentRecursionLevel)));


			var childProperties = BaseDto.GetRelativityObjectChildrenListProperties<T>();

			foreach (var propertyInfo in childProperties)
			{
				var childType = propertyInfo.PropertyType.GetEnumerableInnerType();
				foreach (var parentObject in artifacts)
				{
					var thisChildTypeObjs = propertyInfo.GetValue(parentObject, null) as IList;

					//recurse with child objects and next recursion level
					this.InvokeGenericMethod(childType, nameof(PopulateArtifactsToDeleteList),
						artifactsToDeleteList, maxRecursionLevel, thisChildTypeObjs, currentRecursionLevel + 1);
				}
			}

		}
	}
}
