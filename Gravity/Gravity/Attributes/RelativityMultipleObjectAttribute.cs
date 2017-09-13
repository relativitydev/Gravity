using System;

[AttributeUsage(AttributeTargets.Property)]
public class RelativityMultipleObjectAttribute : Attribute
{
	public Guid FieldGuid { get; set; }

	public Type ChildType { get; set; }

	public RelativityMultipleObjectAttribute(string fieldGuid,Type childType)
	{
		this.FieldGuid = new Guid(fieldGuid);
		this.ChildType = childType;
	}
}