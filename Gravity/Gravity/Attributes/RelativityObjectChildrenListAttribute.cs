using System;

[AttributeUsage(AttributeTargets.Property)]
public class RelativityObjectChildrenListAttribute : Attribute
{
	public Type ChildType { get; set; }

	public RelativityObjectChildrenListAttribute(Type childType)
	{
		this.ChildType = childType;
	}
}