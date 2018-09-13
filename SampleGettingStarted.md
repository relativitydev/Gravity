
A sample is available in the [Gravity.Demo](https://github.com/relativitydev/Gravity/tree/development/Gravity/Gravity.Test/RelativityApplication) application. The rap file is located in this project at the following location Gravity\Gravity.Test\RelativityApplication\GravityDemo.rap.  The application will create new objects types and fields in your Relativity workspace. It is installed as part of the full integration test, so you will have to update the app.config file in the Gravity.Test.Integration project to point to the location on your local disk.

After the application is installed, you can modify and execute the tests in [Gravity.Test](Gravity/Gravity.Test).

In order for the tests to run, you will need to add the correct values for your Relativity instance in the App.config file of Gravity.Test project:

- `WorkspaceID` - the workspace where Gravity Demo is installed
- `SQLServerHostName` - server host name
- `SQLServerUsername` - username for server
- `SQLServerPassword` -  password for server
- `RsapiUrl` - rsapi Url
- `RsapiUsername` - rsapi username
- `RsapiPassword` - rsapi password
- `SMTPServer` - *not yet implemented*
- `SMTPPort` - *not yet implemented*
- `SMTPFromEmailAddress` - *not yet implemented*
- `SMTPUsername` - *not yet implemented*
- `SMTPPassword` - *not yet implemented*
- `TestApplicationLocation` - location on local disk of GravityDemo.rap.  I.e. "S:\SourceCode\Gravity\Gravity\Gravity.Test\RelativityApplication\GravityDemo.rap"


You will also need to replace the value of the "rdoArtifactID" variable in `RsapiDaoTest.cs` with the ArtifactId of the `GravityLevelOne `object demo instance.
