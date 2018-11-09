using Gravity.Base;
using ModelGenerationTool.Attributes;
using ModelGenerationTool.Extensions;
using ModelGenerationTool.Models.NET;
using ModelGenerationTool.Models.NET.Base;
using System;
using System.Collections.Generic;
using System.Xml;

namespace ModelGenerationTool.Models.GravityRdo
{
	[XmlNodePath("Fields/Field", "Default")]
	[XmlNodePath("SystemFields/SystemField", "System")]
	internal class GravityFieldModel : INetParsableProperty
	{
		internal GravityFieldModel(XmlNode fieldNode, bool isSystemField = false)
		{
			string nameKey = typeof(GravityFieldModel).GetPropertyAttribute<XmlNodeKeyAttribute>(nameof(Name)).XmlNodeKey;
			string rdoFieldTypeKey = typeof(GravityFieldModel).GetPropertyAttribute<XmlNodeKeyAttribute>(nameof(RdoFieldType)).XmlNodeKey;
			string guidKey = typeof(GravityFieldModel).GetPropertyAttribute<XmlNodeKeyAttribute>(nameof(Guid)).XmlNodeKey;
			string associativeObjIdKey = typeof(GravityFieldModel).GetPropertyAttribute<XmlNodeKeyAttribute>(nameof(AssociativeArtifactTypeId)).XmlNodeKey;
			string maxTextLengthKey = typeof(GravityFieldModel).GetPropertyAttribute<XmlNodeKeyAttribute>(nameof(MaxTextLength)).XmlNodeKey;

			Name = fieldNode[nameKey].InnerText.ToDotNetNameFormat();
			RdoFieldType = (RdoFieldType)Convert.ToInt32(fieldNode[rdoFieldTypeKey].InnerText);
			Guid = fieldNode[guidKey].InnerText;

			int.TryParse(fieldNode[associativeObjIdKey].InnerText, out int associativeObjId);
			AssociativeArtifactTypeId = associativeObjId > 0 ? associativeObjId : (int?)null;

			int.TryParse(fieldNode[maxTextLengthKey].InnerText, out int maxTextLength);
			MaxTextLength = maxTextLength > 0 ? maxTextLength : (int?)null;

			IsSystemField = isSystemField;
		}

		internal GravityFieldModel(string name, RdoFieldType? rdoFieldType, string guid,
			int? associativeObjId = null,
			int? maxTextLength = null,
			bool isSystemField = false)
		{
			Name = name.ToDotNetNameFormat();
			RdoFieldType = rdoFieldType;
			Guid = guid;
			AssociativeArtifactTypeId = associativeObjId;
			MaxTextLength = maxTextLength;
			IsSystemField = isSystemField;
		}

		[XmlNodeKey("DisplayName")]
		internal string Name { get; set; }

		[XmlNodeKey("FieldTypeId")]
		internal RdoFieldType? RdoFieldType { get; private set; }

		[XmlNodeKey("Guid")]
		internal string Guid { get; private set; }

		[XmlNodeKey("MaxLength")]
		internal int? MaxTextLength { get; set; }

		[XmlNodeKey("AssociativeArtifactTypeId")]
		internal int? AssociativeArtifactTypeId { get; set; }

		internal bool IsSystemField { get; private set; }

		public NetProperty ConvertToDotNet()
		{
			string fieldAttr;
			string typeName = typeof(object).Name;

			if (RdoFieldType.HasValue)
			{
				fieldAttr = $"[RelativityObjectField(\"{Guid}\", RdoFieldType.{RdoFieldType.Value.ToString()})]";

				switch (RdoFieldType.Value)
				{
					case Gravity.Base.RdoFieldType.Currency:
					case Gravity.Base.RdoFieldType.Decimal:
						typeName = typeof(decimal).Name;
						break;

					case Gravity.Base.RdoFieldType.Date:
						typeName = typeof(DateTime).Name;
						break;

					case Gravity.Base.RdoFieldType.File:
						typeName = typeof(FileDto).Name;
						break;

					case Gravity.Base.RdoFieldType.FixedLengthText:
						typeName = typeof(string).Name;

						if (MaxTextLength.HasValue)
							fieldAttr = $"[RelativityObjectField(\"{Guid}\", RdoFieldType.{RdoFieldType.Value.ToString()}, {MaxTextLength.Value})]";

						break;

					case Gravity.Base.RdoFieldType.LongText:
						typeName = typeof(string).Name;
						break;

					case Gravity.Base.RdoFieldType.WholeNumber:
						typeName = typeof(int).Name;
						break;

					case Gravity.Base.RdoFieldType.User:
						// TODO: Must refer to kCura.Relativity.Client.DTOs to be able to use: typeof(User).Name;
						typeName = "User";
						break;

					case Gravity.Base.RdoFieldType.YesNo:
						typeName = typeof(bool).Name;
						break;

					case Gravity.Base.RdoFieldType.SingleChoice:
						typeName = Name;
						break;

					case Gravity.Base.RdoFieldType.MultipleChoice:
						typeName = $"IList<{Name}>";
						break;

					case Gravity.Base.RdoFieldType.SingleObject:
						typeName = Name;

						// Is parent artifact Id field..
						if (IsSystemField && AssociativeArtifactTypeId.HasValue)
						{
							fieldAttr = $"[RelativityObjectFieldParentArtifactId(\"{Guid}\")]";
							RdoFieldType = Gravity.Base.RdoFieldType.WholeNumber;
						}

						break;

					case Gravity.Base.RdoFieldType.MultipleObject:
						typeName = $"IList<{Name}>";
						break;

					case Gravity.Base.RdoFieldType.Empty:
						break;
				}
			}
			else
			{
				// Children List
				typeName = $"IList<{Name}>";
				fieldAttr = "[RelativityObjectChildrenList()]";
			}

			return new NetProperty(new List<string>() { fieldAttr }, Name, typeName);
		}
	}
}