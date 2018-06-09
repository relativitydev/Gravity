![Gravity](https://raw.githubusercontent.com/relativitydev/Gravity/development/images/Gravity.png) 

Open Source Community: **Gravity** is an [ORM framework](https://en.wikipedia.org/wiki/Object-relational_mapping) for Relativity custom development.  Using Gravity will greatly decrease the amount of time it takes to pick up Relativity development and allow you to write code that interacts with Relativity with commonly used C# syntax.

While this project is hosted on the RelativityDev account, support is only available through the Relativity developer community. You are welcome to use the code and solution as you see fit within the confines of the license it is released under. However, if you are looking for support or modifications to the solution, we suggest reaching out to a Relativity Development Partner.

Gravity was originally created by TSD Services.   Through their generosity and leadership, they have released the project as open source.  It is an active project and has contributions from other Relativity Development Partners.  Anyone who has a need is invited to use and contribute to the project.

We would like to recognize the following Relativity Development Partners who have made significant contributions to the Gravity project:

<p align="center>
	<img src="http://www.tsdservices.com/wp-content/uploads/2015/03/TSD_Logo-TM-for-website.png">  
</p>

![TSD Services](http://www.tsdservices.com/wp-content/uploads/2015/03/TSD_Logo-TM-for-website.png "TSD Services")  &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; ![MILYLI](http://milyli.com/wp-content/uploads/2014/07/milyli_header-regular.png "MILYLI")

This is also available as a [nuget package](https://www.nuget.org/packages/Gravity/).

## Target Frameworks
* .NET 4.5.1, .NET 4.6.2

## Dependencies
This project requires references to Relativity's Relativity® SDK dlls, which are referenced via Nuget packages. As such, DLL versions 9.4.224.2 and up are supported.

## Sample / Test Suite

Information about the demo application and accompanying integration tests is available on a [separate page](SampleGettingStarted.md).

## Usage Guide
Before using the CRUD/Q methods in Gravity you will have to create a model and decorate it with the appropriate attributes:

* `RelativityObject` - Specifies the type Guid of the RDO you are targeting.
* `RelativityObjectField` - Specifies the type Guid and the "RdoFieldType" of the RDO field you are targeting.
* `RelativityObjectChildrenList` - Used to decorate a List of child RDOs as a object List.

The following example demonstrates a RDO represented as a Model:
```csharp
[Serializable]
[RelativityObject("0B5C62E0-2AFA-4408-B7FF-789351C9BEDC")]
public class DemoPurchaseOrder : BaseDto
{
	[RelativityObjectField("E1FA93B9-C2DB-442A-9978-84EEB6B61A3F", RdoFieldType.FixedLengthText, 255)]
	public override string Name { get; set; }

	[RelativityObjectField("37159592-B5B6-4405-AF74-10B5728890B4", RdoFieldType.WholeNumber)]
	public int OrderNumber { get; set; }

	[RelativityObjectField("37159592-B5B6-4405-AF74-10B5728890B4", RdoFieldType.FixedLengthText, 100)]
	public string CustomerName { get; set; }

	[RelativityObjectField("3BDC0971-A87C-414E-9A37-FC477279BBAD", RdoFieldType.FixedLengthText, 100)]
	public string CustomerEmail { get; set; }

	[RelativityObjectField("D0770889-8A4D-436A-9647-33419B96E37E"), RdoFieldType.MultipleObject)]
	public IList<Items> Items { get; set; }

	[RelativityObjectField("D0770889-8A4D-436A-9647-33419B96E37E"), RdoFieldType.SingleObject)]
	public Address Address { get; set; }

	[RelativityObjectField("4501A308-5E68-4314-AEDC-4DEB527F12A8", RdoFieldType.Decimal)]
	public decimal Total { get; set; }

	[RelativityObjectField("CEDB347B-679D-44ED-93D3-0B3027C7E6F5", RdoFieldType.SingleChoice)]
	public OrderType OrderType { get; set; }

	[RelativityObjectChildrenList]
	public IList<RelatedPurchase> RelatedPurchases { get; set; }
}
```

* **Note:** For property of type `User` use `kCura.Relativity.Client.DTOs.User` and for property of type `FileField` use `Gravity.Base.RelativityFile`

For Choice field you must create a enum and decorate it with the appropriate attributes:

* `RelativityObject` - Specifies the choice Guid.

The following example demonstrates a choice field represented as an Enum:
```csharp
public enum OrderType
{
	[RelativityObject("4F04381D-F3E3-4DEE-8EF9-11F27047D9B4")]
	TypeOne = 1,

	[RelativityObject("8453BF3E-D95B-4BC5-BD68-3CF4277DD731")]
	TypeTwo = 2
}
```

To use Gravity for RSAPI operations, you must instantiate an `RsapiDao` object using the `RsapiDao` constructor, with `IHelper` and `WorkspaceId` as parameters.

Supported RSAPIDao methods:
 - `GetRelativityObject<T>(int artifactId, ObjectFieldsDepthLevel depthLevel)` - Get DTO by Artifact ID and specific depth level of child objects and object     fields.
 - `GetDTOs<T>(int[] artifactIDs, ObjectFieldsDepthLevel depthLevel)` - Get DTOs by Artifact IDs and specific depth level of child objects and object
 fields.
 - `List<T> GetAllChildDTOs<T>(Guid parentFieldGuid, int parentArtifactID, ObjectFieldsDepthLevel depthLevel)` - Get all child DTOs of type for parent.
 - `List<T> GetAllDTOs<T>(Condition queryCondition = null, ObjectFieldsDepthLevel depthLevel = ObjectFieldsDepthLevel.FirstLevelOnly)`
 - `List<T> GetAllDTOs<T>()` - Get all DTOs of type.
 - `DeleteRelativityObjectRecusively<T>(T theObjectToDelete)` - Delete object recursively (includes child objects).
 - `DeleteRelativityObjectRecusively<T>(int objectToDeleteId)`- Delete object recursively (includes child objects) by Artifact ID.
 - `InsertChildListObjects<T>(IList<T> objectsToInserted, int parentArtifactId)` - Insert Child objects for parent.
 - `InsertRelativityObject<T>(BaseDto theObjectToInsert)` - Insert Relativity object from RDO.
 - `UpdateRelativityObject<T>(BaseDto theObjectToUpdate)` - Update Relativity object from RDO.
 - `UpdateField<T>(int rdoID, Guid fieldGuid, object value)` - Update field value by GUID and RDO Artifact ID

### Example

The following example demonstrates a object "Get" used in Event handler. First we instantiate `RsapiDao` and then we use the Gravity RSAPI Dao `GetRelativityObject` method to get the object (`ObjectFieldsDepthLevel.OnlyParentObject` means that we want just the object - no child object fields, multiple object fields or single object fields are populated recursively):
```csharp
public override Response Execute()
{
	Response returnResponse = new Response() { Message = string.Empty, Success = true };

	RsapiDao gravityRsapiDao = new RsapiDao(this.Helper, this.Helper.GetActiveCaseID());

		DemoPurchaseOrder demoOrder =  gravityRsapiDao.GetRelativityObject<DemoPurchaseOrder>(1047088,
	  ObjectFieldsDepthLevel.OnlyParentObject);

		return returnResponse;
}
```
