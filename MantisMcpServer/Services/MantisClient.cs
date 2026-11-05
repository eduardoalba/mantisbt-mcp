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

        // Phase 4: Subprojects, Users, Custom Fields and Project Attachments
        Task<string[]> mc_project_get_all_subprojectsAsync(string username, string password, string project_id);
        Task<AccountData[]> mc_project_get_usersAsync(string username, string password, string project_id, string access);
        Task<string> mc_issue_get_id_from_summaryAsync(string username, string password, string summary);
        Task<CustomFieldDefinitionData[]> mc_project_get_custom_fieldsAsync(string username, string password, string project_id);
        Task<ProjectAttachmentData[]> mc_project_get_attachmentsAsync(string username, string password, string project_id);
        Task<string> mc_project_attachment_addAsync(string username, string password, string project_id, string name, string title, string description, string file_type, byte[] content);
        Task<byte[]> mc_project_attachment_getAsync(string username, string password, string project_attachment_id);
        Task<bool> mc_project_attachment_deleteAsync(string username, string password, string project_attachment_id);
        Task<ObjectRef[]> mc_enum_access_levelsAsync(string username, string password);
        Task<ObjectRef[]> mc_enum_project_statusAsync(string username, string password);
        Task<ObjectRef[]> mc_enum_reproducibilitiesAsync(string username, string password);

        // Phase 5: Note update/delete, attachments, batch reads and issue utilities
        Task<bool> mc_issue_note_updateAsync(string username, string password, IssueNoteData note);
        Task<bool> mc_issue_note_deleteAsync(string username, string password, string issue_note_id);
        Task<bool> mc_issue_attachment_deleteAsync(string username, string password, string issue_attachment_id);
        Task<bool> mc_issue_deleteAsync(string username, string password, string issue_id);
        Task<bool> mc_issue_existsAsync(string username, string password, string issue_id);
        Task<IssueData[]> mc_issues_getAsync(string username, string password, string[] issue_ids);
        Task<IssueHeaderData[]> mc_issues_get_headerAsync(string username, string password, string[] issue_ids);
        Task<string> mc_issue_get_biggest_idAsync(string username, string password, string project_id);
        Task<bool> mc_issue_checkinAsync(string username, string password, string issue_id, string comment, bool isFixed);
        Task<string[]> mc_filter_search_issue_idsAsync(string username, string password, FilterSearchData filter, string page_number, string per_page);

        // Phase 6: Project versions, categories and remaining enums
        Task<string> mc_project_get_id_from_nameAsync(string username, string password, string project_name);
        Task<ProjectVersionData[]> mc_project_get_versionsAsync(string username, string password, string project_id);
        Task<ProjectVersionData[]> mc_project_get_released_versionsAsync(string username, string password, string project_id);
        Task<ProjectVersionData[]> mc_project_get_unreleased_versionsAsync(string username, string password, string project_id);
        Task<string> mc_project_version_addAsync(string username, string password, ProjectVersionData version);
        Task<bool> mc_project_version_updateAsync(string username, string password, string version_id, ProjectVersionData version);
        Task<bool> mc_project_version_deleteAsync(string username, string password, string version_id);
        Task<string> mc_project_add_categoryAsync(string username, string password, string project_id, string p_category_name);
        Task<string> mc_project_delete_categoryAsync(string username, string password, string project_id, string p_category_name);
        Task<string> mc_project_rename_category_by_nameAsync(string username, string password, string project_id, string p_category_name, string p_category_name_new, string p_assigned_to);
        Task<ObjectRef[]> mc_enum_projectionsAsync(string username, string password);
        Task<ObjectRef[]> mc_enum_etasAsync(string username, string password);
        Task<ObjectRef[]> mc_enum_project_view_statesAsync(string username, string password);
        Task<ObjectRef[]> mc_enum_view_statesAsync(string username, string password);
        Task<ObjectRef[]> mc_enum_custom_field_typesAsync(string username, string password);
        Task<string> mc_enum_getAsync(string username, string password, string enumeration);
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

        // Phase 4: Subprojects, Users, Custom Fields and Project Attachments
        public Task<string[]> mc_project_get_all_subprojectsAsync(string username, string password, string project_id) => _inner.mc_project_get_all_subprojectsAsync(username, password, project_id);
        public Task<AccountData[]> mc_project_get_usersAsync(string username, string password, string project_id, string access) => _inner.mc_project_get_usersAsync(username, password, project_id, access);
        public Task<string> mc_issue_get_id_from_summaryAsync(string username, string password, string summary) => _inner.mc_issue_get_id_from_summaryAsync(username, password, summary);
        public Task<CustomFieldDefinitionData[]> mc_project_get_custom_fieldsAsync(string username, string password, string project_id) => _inner.mc_project_get_custom_fieldsAsync(username, password, project_id);
        public Task<ProjectAttachmentData[]> mc_project_get_attachmentsAsync(string username, string password, string project_id) => _inner.mc_project_get_attachmentsAsync(username, password, project_id);
        public Task<string> mc_project_attachment_addAsync(string username, string password, string project_id, string name, string title, string description, string file_type, byte[] content) => _inner.mc_project_attachment_addAsync(username, password, project_id, name, title, description, file_type, content);
        public Task<byte[]> mc_project_attachment_getAsync(string username, string password, string project_attachment_id) => _inner.mc_project_attachment_getAsync(username, password, project_attachment_id);
        public Task<bool> mc_project_attachment_deleteAsync(string username, string password, string project_attachment_id) => _inner.mc_project_attachment_deleteAsync(username, password, project_attachment_id);
        public Task<ObjectRef[]> mc_enum_access_levelsAsync(string username, string password) => _inner.mc_enum_access_levelsAsync(username, password);
        public Task<ObjectRef[]> mc_enum_project_statusAsync(string username, string password) => _inner.mc_enum_project_statusAsync(username, password);
        public Task<ObjectRef[]> mc_enum_reproducibilitiesAsync(string username, string password) => _inner.mc_enum_reproducibilitiesAsync(username, password);

        // Phase 5: Note update/delete, attachments, batch reads and issue utilities
        public Task<bool> mc_issue_note_updateAsync(string username, string password, IssueNoteData note) => _inner.mc_issue_note_updateAsync(username, password, note);
        public Task<bool> mc_issue_note_deleteAsync(string username, string password, string issue_note_id) => _inner.mc_issue_note_deleteAsync(username, password, issue_note_id);
        public Task<bool> mc_issue_attachment_deleteAsync(string username, string password, string issue_attachment_id) => _inner.mc_issue_attachment_deleteAsync(username, password, issue_attachment_id);
        public Task<bool> mc_issue_deleteAsync(string username, string password, string issue_id) => _inner.mc_issue_deleteAsync(username, password, issue_id);
        public Task<bool> mc_issue_existsAsync(string username, string password, string issue_id) => _inner.mc_issue_existsAsync(username, password, issue_id);
        public Task<IssueData[]> mc_issues_getAsync(string username, string password, string[] issue_ids) => _inner.mc_issues_getAsync(username, password, issue_ids);
        public Task<IssueHeaderData[]> mc_issues_get_headerAsync(string username, string password, string[] issue_ids) => _inner.mc_issues_get_headerAsync(username, password, issue_ids);
        public Task<string> mc_issue_get_biggest_idAsync(string username, string password, string project_id) => _inner.mc_issue_get_biggest_idAsync(username, password, project_id);
        public Task<bool> mc_issue_checkinAsync(string username, string password, string issue_id, string comment, bool isFixed) => _inner.mc_issue_checkinAsync(username, password, issue_id, comment, isFixed);
        public Task<string[]> mc_filter_search_issue_idsAsync(string username, string password, FilterSearchData filter, string page_number, string per_page) => _inner.mc_filter_search_issue_idsAsync(username, password, filter, page_number, per_page);

        // Phase 6: Project versions, categories and remaining enums
        public Task<string> mc_project_get_id_from_nameAsync(string username, string password, string project_name) => _inner.mc_project_get_id_from_nameAsync(username, password, project_name);
        public Task<ProjectVersionData[]> mc_project_get_versionsAsync(string username, string password, string project_id) => _inner.mc_project_get_versionsAsync(username, password, project_id);
        public Task<ProjectVersionData[]> mc_project_get_released_versionsAsync(string username, string password, string project_id) => _inner.mc_project_get_released_versionsAsync(username, password, project_id);
        public Task<ProjectVersionData[]> mc_project_get_unreleased_versionsAsync(string username, string password, string project_id) => _inner.mc_project_get_unreleased_versionsAsync(username, password, project_id);
        public Task<string> mc_project_version_addAsync(string username, string password, ProjectVersionData version) => _inner.mc_project_version_addAsync(username, password, version);
        public Task<bool> mc_project_version_updateAsync(string username, string password, string version_id, ProjectVersionData version) => _inner.mc_project_version_updateAsync(username, password, version_id, version);
        public Task<bool> mc_project_version_deleteAsync(string username, string password, string version_id) => _inner.mc_project_version_deleteAsync(username, password, version_id);
        public Task<string> mc_project_add_categoryAsync(string username, string password, string project_id, string p_category_name) => _inner.mc_project_add_categoryAsync(username, password, project_id, p_category_name);
        public Task<string> mc_project_delete_categoryAsync(string username, string password, string project_id, string p_category_name) => _inner.mc_project_delete_categoryAsync(username, password, project_id, p_category_name);
        public Task<string> mc_project_rename_category_by_nameAsync(string username, string password, string project_id, string p_category_name, string p_category_name_new, string p_assigned_to) => _inner.mc_project_rename_category_by_nameAsync(username, password, project_id, p_category_name, p_category_name_new, p_assigned_to);
        public Task<ObjectRef[]> mc_enum_projectionsAsync(string username, string password) => _inner.mc_enum_projectionsAsync(username, password);
        public Task<ObjectRef[]> mc_enum_etasAsync(string username, string password) => _inner.mc_enum_etasAsync(username, password);
        public Task<ObjectRef[]> mc_enum_project_view_statesAsync(string username, string password) => _inner.mc_enum_project_view_statesAsync(username, password);
        public Task<ObjectRef[]> mc_enum_view_statesAsync(string username, string password) => _inner.mc_enum_view_statesAsync(username, password);
        public Task<ObjectRef[]> mc_enum_custom_field_typesAsync(string username, string password) => _inner.mc_enum_custom_field_typesAsync(username, password);
        public Task<string> mc_enum_getAsync(string username, string password, string enumeration) => _inner.mc_enum_getAsync(username, password, enumeration);

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
            
            var binding = new System.ServiceModel.BasicHttpBinding
            {
                MaxBufferSize = int.MaxValue,
                ReaderQuotas = System.Xml.XmlDictionaryReaderQuotas.Max,
                MaxReceivedMessageSize = int.MaxValue,
                AllowCookies = true
            };

            if (soapEndpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                binding.Security.Mode = System.ServiceModel.BasicHttpSecurityMode.Transport;
            }

            var endpointAddress = new System.ServiceModel.EndpointAddress(soapEndpoint);
            var client = new MantisConnectPortTypeClient(binding, endpointAddress);
            
            return new MantisSoapClientWrapper(client);
        }
    }
}
