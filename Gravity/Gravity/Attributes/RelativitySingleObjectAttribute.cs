using System;

[AttributeUsage(AttributeTargets.Property)]
public class RelativitySingleObjectAttribute : Attribute
{
	public Guid FieldGuid { get; set; }

	public Type ChildType { get; set; }

	public RelativitySingleObjectAttribute(string fieldGuid,Type childType)
	{
		this.FieldGuid = new Guid(fieldGuid);
		this.ChildType = childType;
	}
}