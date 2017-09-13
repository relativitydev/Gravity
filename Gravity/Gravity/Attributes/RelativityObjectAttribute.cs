using System;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Field)]
public class RelativityObjectAttribute : Attribute
{
	public Guid ObjectTypeGuid { get; set; }

	public RelativityObjectAttribute(string objectTypeGuid)
	{
		this.ObjectTypeGuid = new Guid(objectTypeGuid);
	}
}