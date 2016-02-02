using System.Configuration;
using Octopus.Client;

namespace OctopusClient
{
    public class Context
    {

        #region Public Fields

        public static string ApiKey = ConfigurationManager.AppSettings["ApiKey"];
        public static string OctopusUri = ConfigurationManager.AppSettings["OctopusUri"];

        #endregion Public Fields

        #region Public Methods

        public static OctopusRepository GetOctopusRepository()
        {
            var endPoint = new OctopusServerEndpoint(OctopusUri, ApiKey);
            return new OctopusRepository(endPoint);
        }

        #endregion Public Methods

    }
}
