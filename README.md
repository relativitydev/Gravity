# Gravity
CRUDQ Framework for Relativity Custom Development

This is also available as a [nuget package].(https://www.nuget.org/packages/RelativityTestHelpersNuget/)

This project requires references to Relativity's Relativity® SDK dlls. These dlls are not part of the open source project and can be obtained 
by contacting support@relativity.com, getting it from your Relativity instance, or installing the SDK from the [Community Portal]
(https://community.relativity.com/s/files).

Under "relativity-integration-test-helpers\Source\Relativity.Test.Helpers\" you will need to create a "Packages" folder if one does not exist 
and you will need to add the following dlls to this folder.Once you do that you should be able to run your integration tests against an
environment.

• kCura.Relativity.Client.dll
• Relativity.API.dll
• kCura.Data.RowDataGateway.dll
