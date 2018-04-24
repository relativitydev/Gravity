using System;

[AttributeUsage(AttributeTargets.Property)]
public class RelativitySingleObjectAttribute : Attribute
{
	public Guid FieldGuid { get; set; }

	public RelativitySingleObjectAttribute(string fieldGuid)
	{
		this.FieldGuid = new Guid(fieldGuid);
	}
}