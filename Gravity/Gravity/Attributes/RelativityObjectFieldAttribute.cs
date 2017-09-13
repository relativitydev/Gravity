using System;

// Similar to kCura's kCura.Relativity.Client.DTOs.Attributes.FieldAttribute, but we work with Field GUID instead of FieldName
// In our case, this is necessary, as their attribute is for system fields and clients are prolly not supposed to change them
// While our RDO field names could get changed somehow, and we want to work with the GUIDs.
[AttributeUsage(AttributeTargets.Property)]
public class RelativityObjectFieldAttribute : System.Attribute
{
	public Guid FieldGuid { get; set; }

	// TODO: Make this our own custom enum in RelativityShared; right now an ugly int?
	public int FieldType { get; set; }

	public int? Length { get; set; }

	public Type ObjectFieldDTOType { get; set; }

	public RelativityObjectFieldAttribute(string fieldGuid, int fieldType)
	{
		this.FieldGuid = new Guid(fieldGuid);
		this.FieldType = fieldType;
		this.Length = null;
	}

	public RelativityObjectFieldAttribute(string fieldGuid, int fieldType, int length)
	{
		this.FieldGuid = new Guid(fieldGuid);
		this.FieldType = fieldType;
		this.Length = length;
	}

	public RelativityObjectFieldAttribute(string fieldGuid, int fieldType, Type objectFieldDTOType)
	{
		this.FieldGuid = new Guid(fieldGuid);
		this.FieldType = fieldType;
		this.ObjectFieldDTOType = objectFieldDTOType;
		this.Length = null;
	}
}