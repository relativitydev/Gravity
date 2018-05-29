using System;
using Gravity.Base;

// Similar to kCura's kCura.Relativity.Client.DTOs.Attributes.FieldAttribute, but we work with Field GUID instead of FieldName
// In our case, this is necessary, as their attribute is for system fields and clients are prolly not supposed to change them
// While our RDO field names could get changed somehow, and we want to work with the GUIDs.
[AttributeUsage(AttributeTargets.Property)]
public class RelativityObjectFieldAttribute : System.Attribute
{
	public Guid FieldGuid { get; set; }

	public RdoFieldType FieldType { get; set; }

	public int? Length { get; set; }

	public RelativityObjectFieldAttribute(string fieldGuid, RdoFieldType fieldType)
	{
		this.FieldGuid = new Guid(fieldGuid);
		this.FieldType = fieldType;
	}

	public RelativityObjectFieldAttribute(string fieldGuid, RdoFieldType fieldType, int length)
		: this (fieldGuid, fieldType)
	{
		this.Length = length;
	}
}