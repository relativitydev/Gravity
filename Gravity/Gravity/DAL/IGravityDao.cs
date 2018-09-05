using Gravity.Base;
using System;
using System.Collections.Generic;

namespace Gravity.DAL
{
	public interface IGravityDao
	{
		void Delete<T>(int artifactID, ObjectFieldsDepthLevel depthLevel) where T : BaseDto, new();
		T Get<T>(int artifactID, ObjectFieldsDepthLevel depthLevel) where T : BaseDto, new();
		List<T> Get<T>(IList<int> artifactIDs, ObjectFieldsDepthLevel depthLevel) where T : BaseDto, new();
		int Insert<T>(T obj, ObjectFieldsDepthLevel depthLevel) where T : BaseDto;
		void Insert<T>(IList<T> objs, ObjectFieldsDepthLevel depthLevel) where T : BaseDto;
		void Update<T>(T obj, ObjectFieldsDepthLevel depthLevel) where T : BaseDto;
		void Update<T>(IList<T> objs, ObjectFieldsDepthLevel depthLevel) where T : BaseDto;
	}
}