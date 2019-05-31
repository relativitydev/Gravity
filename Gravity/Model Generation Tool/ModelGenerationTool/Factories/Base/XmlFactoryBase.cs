using System.Xml;

namespace ModelGenerationTool.Factories.Base
{
	public abstract class XmlFactoryBase
	{
		protected XmlDocument xmlDocument;

		protected XmlFactoryBase(XmlDocument xmlDocument)
		{
			this.xmlDocument = xmlDocument;
		}
	}
}