using System;

[AttributeUsage(AttributeTargets.Property)]
public class RelativityObjectFieldParentArtifactIdAttribute : Attribute
{
	public Guid FieldGuid { get; set; }

	public RelativityObjectFieldParentArtifactIdAttribute(string fieldGuid)
	{
		this.FieldGuid = new Guid(fieldGuid);
	}
}