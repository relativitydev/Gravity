using Gravity.Base;
using System;
using System.Collections.Generic;

namespace Gravity.DAL
{
	public interface IGravityDao
	{
		void Delete<T>(int artifactID, ObjectFieldsDepthLevel depthLevel) where T : BaseDto, new();
		List<T> Get<T>(int[] artifactIDs, ObjectFieldsDepthLevel depthLevel) where T : BaseDto, new();
		T Get<T>(int artifactID, ObjectFieldsDepthLevel depthLevel) where T : BaseDto, new();
		int Insert<T>(T obj) where T : BaseDto;
		void Update<T>(T obj) where T : BaseDto;
	}
}