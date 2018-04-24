using System;

[AttributeUsage(AttributeTargets.Property)]
public class RelativityMultipleObjectAttribute : Attribute
{
	public Guid FieldGuid { get; set; }

	public RelativityMultipleObjectAttribute(string fieldGuid)
	{
		this.FieldGuid = new Guid(fieldGuid);
	}
}