using Gravity.Base;
using System;
using System.Collections.Generic;

namespace Gravity.DAL
{
	public interface IGravityDao
	{
		void Delete<T>(int artifactID) where T : BaseDto, new();
		void Delete<T>(T obj) where T : BaseDto;
		List<T> Get<T>(int[] artifactIDs, ObjectFieldsDepthLevel depthLevel) where T : BaseDto, new();
		T Get<T>(int artifactID, ObjectFieldsDepthLevel depthLevel) where T : BaseDto, new();
		int Insert<T>(T obj, ObjectFieldsDepthLevel depthLevel) where T : BaseDto;
		void Update<T>(T obj) where T : BaseDto;
	}
}