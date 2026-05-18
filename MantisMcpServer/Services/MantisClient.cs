using MantisService;

namespace MantisMcpServer.Services
{
    public interface IMantisSoapClient : IDisposable
    {
        Task<IssueData> mc_issue_getAsync(string username, string password, string issue_id);
        Task<IssueData[]> mc_project_get_issuesAsync(string username, string password, string project_id, string page_number, string per_page);
        Task<string> mc_issue_addAsync(string username, string password, IssueData issue);
        Task<string> mc_issue_note_addAsync(string username, string password, string issue_id, IssueNoteData note);
        Task<bool> mc_issue_updateAsync(string username, string password, string issue_id, IssueData issue);
        Task<ProjectData[]> mc_projects_get_user_accessibleAsync(string username, string password);
        Task<string[]> mc_project_get_categoriesAsync(string username, string password, string project_id);
        Task<ObjectRef[]> mc_enum_statusAsync(string username, string password);
        Task<ObjectRef[]> mc_enum_prioritiesAsync(string username, string password);
        Task<ObjectRef[]> mc_enum_severitiesAsync(string username, string password);
        Task<ObjectRef[]> mc_enum_resolutionsAsync(string username, string password);
    }

    public class MantisSoapClientWrapper : IMantisSoapClient
    {
        private readonly MantisConnectPortTypeClient _inner;

        public MantisSoapClientWrapper(MantisConnectPortTypeClient inner)
        {
            _inner = inner;
        }

        public Task<IssueData> mc_issue_getAsync(string username, string password, string issue_id) => _inner.mc_issue_getAsync(username, password, issue_id);
        public Task<IssueData[]> mc_project_get_issuesAsync(string username, string password, string project_id, string page_number, string per_page) => _inner.mc_project_get_issuesAsync(username, password, project_id, page_number, per_page);
        public Task<string> mc_issue_addAsync(string username, string password, IssueData issue) => _inner.mc_issue_addAsync(username, password, issue);
        public Task<string> mc_issue_note_addAsync(string username, string password, string issue_id, IssueNoteData note) => _inner.mc_issue_note_addAsync(username, password, issue_id, note);
        public Task<bool> mc_issue_updateAsync(string username, string password, string issue_id, IssueData issue) => _inner.mc_issue_updateAsync(username, password, issue_id, issue);
        public Task<ProjectData[]> mc_projects_get_user_accessibleAsync(string username, string password) => _inner.mc_projects_get_user_accessibleAsync(username, password);
        public Task<string[]> mc_project_get_categoriesAsync(string username, string password, string project_id) => _inner.mc_project_get_categoriesAsync(username, password, project_id);
        public Task<ObjectRef[]> mc_enum_statusAsync(string username, string password) => _inner.mc_enum_statusAsync(username, password);
        public Task<ObjectRef[]> mc_enum_prioritiesAsync(string username, string password) => _inner.mc_enum_prioritiesAsync(username, password);
        public Task<ObjectRef[]> mc_enum_severitiesAsync(string username, string password) => _inner.mc_enum_severitiesAsync(username, password);
        public Task<ObjectRef[]> mc_enum_resolutionsAsync(string username, string password) => _inner.mc_enum_resolutionsAsync(username, password);

        public void Dispose()
        {
            if (_inner is IDisposable d) d.Dispose();
        }
    }

    public interface IMantisClient
    {
        string Username { get; }
        string Token { get; }
        IMantisSoapClient CreateSoapClient();
    }

    public class MantisClient : IMantisClient
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

        public IMantisSoapClient CreateSoapClient()
        {
            string soapEndpoint = _url.TrimEnd('/') + "/api/soap/mantisconnect.php";
            
            var client = new MantisConnectPortTypeClient(
                MantisConnectPortTypeClient.EndpointConfiguration.MantisConnectPort,
                soapEndpoint);
            
            return new MantisSoapClientWrapper(client);
        }
    }
}
