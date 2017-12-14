
A sample is available in the [Gravity.Demo](Gravity.Demo) solution. To try it, install the latest RAP in the Installers subfolder, which will create new objects types in your Relativity workspace and add a few members. 

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

You will also need to replace the value of the "rdoArtifactID" variable in `RsapiDaoTest.cs` with the ArtifactId of the `GravityLevelOne `object demo instance.
