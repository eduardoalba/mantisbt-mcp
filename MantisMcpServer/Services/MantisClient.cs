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
        
        // Phase 2: Filters
        Task<FilterData[]> mc_filter_getAsync(string username, string password, string project_id);
        Task<IssueData[]> mc_filter_get_issuesAsync(string username, string password, string project_id, string filter_id, string page_number, string per_page);
        Task<IssueData[]> mc_filter_search_issuesAsync(string username, string password, FilterSearchData filter, string page_number, string per_page);
        
        // Phase 2: User
        Task<UserData> mc_loginAsync(string username, string password);
        Task<IssueData[]> mc_project_get_issues_for_userAsync(string username, string password, string project_id, string filter_type, AccountData target_user, string page_number, string per_page);

        // Phase 2: Attachments
        Task<byte[]> mc_issue_attachment_getAsync(string username, string password, string issue_attachment_id);
        Task<string> mc_issue_attachment_addAsync(string username, string password, string issue_id, string name, string file_type, byte[] content);

        // Phase 3: History, Relationships and Tags
        Task<HistoryData[]> mc_issue_get_historyAsync(string username, string password, string issue_id);
        Task<string> mc_issue_relationship_addAsync(string username, string password, string issue_id, RelationshipData relationship);
        Task<bool> mc_issue_relationship_deleteAsync(string username, string password, string issue_id, string relationship_id);
        Task<TagDataSearchResult> mc_tag_get_allAsync(string username, string password, string page_number, string per_page);
        Task<bool> mc_issue_set_tagsAsync(string username, string password, string issue_id, TagData[] tags);
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

        // Phase 2: Filters
        public Task<FilterData[]> mc_filter_getAsync(string username, string password, string project_id) => _inner.mc_filter_getAsync(username, password, project_id);
        public Task<IssueData[]> mc_filter_get_issuesAsync(string username, string password, string project_id, string filter_id, string page_number, string per_page) => _inner.mc_filter_get_issuesAsync(username, password, project_id, filter_id, page_number, per_page);
        public Task<IssueData[]> mc_filter_search_issuesAsync(string username, string password, FilterSearchData filter, string page_number, string per_page) => _inner.mc_filter_search_issuesAsync(username, password, filter, page_number, per_page);

        // Phase 2: User
        public Task<UserData> mc_loginAsync(string username, string password) => _inner.mc_loginAsync(username, password);
        public Task<IssueData[]> mc_project_get_issues_for_userAsync(string username, string password, string project_id, string filter_type, AccountData target_user, string page_number, string per_page) => _inner.mc_project_get_issues_for_userAsync(username, password, project_id, filter_type, target_user, page_number, per_page);

        // Phase 2: Attachments
        public Task<byte[]> mc_issue_attachment_getAsync(string username, string password, string issue_attachment_id) => _inner.mc_issue_attachment_getAsync(username, password, issue_attachment_id);
        public Task<string> mc_issue_attachment_addAsync(string username, string password, string issue_id, string name, string file_type, byte[] content) => _inner.mc_issue_attachment_addAsync(username, password, issue_id, name, file_type, content);

        // Phase 3: History, Relationships and Tags
        public Task<HistoryData[]> mc_issue_get_historyAsync(string username, string password, string issue_id) => _inner.mc_issue_get_historyAsync(username, password, issue_id);
        public Task<string> mc_issue_relationship_addAsync(string username, string password, string issue_id, RelationshipData relationship) => _inner.mc_issue_relationship_addAsync(username, password, issue_id, relationship);
        public Task<bool> mc_issue_relationship_deleteAsync(string username, string password, string issue_id, string relationship_id) => _inner.mc_issue_relationship_deleteAsync(username, password, issue_id, relationship_id);
        public Task<TagDataSearchResult> mc_tag_get_allAsync(string username, string password, string page_number, string per_page) => _inner.mc_tag_get_allAsync(username, password, page_number, per_page);
        public Task<bool> mc_issue_set_tagsAsync(string username, string password, string issue_id, TagData[] tags) => _inner.mc_issue_set_tagsAsync(username, password, issue_id, tags);

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
