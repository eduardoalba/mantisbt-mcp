using MantisService;

namespace MantisMcpServer.Services
{
    public class MantisClient
    {
        private readonly string _url;
        private readonly string _username;
        private readonly string _token;

        public MantisClient(string url, string username, string token)
        {
            _url = url;
            _username = username;
            _token = token;
        }

        public string Username => _username;
        public string Token => _token;

        public MantisConnectPortTypeClient CreateSoapClient()
        {
            // O WSDL gerou o endpoint fixo, mas permitimos sobrescrever via MANTIS_URL
            string soapEndpoint = _url.TrimEnd('/') + "/api/soap/mantisconnect.php";
            
            var client = new MantisConnectPortTypeClient(
                MantisConnectPortTypeClient.EndpointConfiguration.MantisConnectPort,
                soapEndpoint);
            
            return client;
        }
    }
}
