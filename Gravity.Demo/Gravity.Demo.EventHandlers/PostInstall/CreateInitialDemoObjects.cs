using kCura.EventHandler;
using kCura.EventHandler.CustomAttributes;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Gravity.DAL.RSAPI;
using Gravity.Demo.EventHandler.Constants;
using Gravity.Demo.EventHandlers.Models;
using Relativity.API;

namespace Gravity.Demo.EventHandlers.PostInstall
{
	[RunOnce(true)]
	[Description("This Event Handler create the initial Demo RDO instances.")]
	[Guid("B8142FE4-8495-4E6F-8627-B67CC7AFC739")]
	public class CreateInitialDemoObjects : PostInstallEventHandler
	{
		private RsapiDao gravityRsapiDao;

		public override Response Execute()
		{
			Response returnResponse = new Response() { Message = string.Empty, Success = true };

			try
			{
				gravityRsapiDao = new RsapiDao(this.Helper, this.Helper.GetActiveCaseID(), ExecutionIdentity.System);

				gravityRsapiDao.InsertRelativityObject<GravityLevelOne>(DemoModelsConstants.LevelOneObject);
				returnResponse.Message = "Demo object imported successfully";
			}
			catch (Exception ex)
			{
				returnResponse.Success = false;
				returnResponse.Message = string.Format("Demo object import failed with the following error:{0} and the following inner exception:{1}. ", ex.Message, ex.InnerException.Message );
			}

			return returnResponse;
		}
	}
}
