using FluentAssertions;
using MantisMcpServer.Services;
using MantisMcpServer.Tools;
using MantisService;
using Microsoft.Extensions.Logging;
using Moq;

namespace MantisMcpServer.Tests
{
    public class ProjectToolsTests
    {
        private readonly Mock<IMantisClient> _mantisClientMock;
        private readonly Mock<IMantisSoapClient> _soapClientMock;
        private readonly Mock<ILogger<ProjectTools>> _loggerMock;
        private readonly ProjectTools _projectTools;

        public ProjectToolsTests()
        {
            _mantisClientMock = new Mock<IMantisClient>();
            _soapClientMock = new Mock<IMantisSoapClient>();
            _loggerMock = new Mock<ILogger<ProjectTools>>();

            _mantisClientMock.Setup(m => m.CreateSoapClient()).Returns(_soapClientMock.Object);
            _mantisClientMock.Setup(m => m.Username).Returns("testuser");
            _mantisClientMock.Setup(m => m.Token).Returns("testtoken");

            _projectTools = new ProjectTools(_mantisClientMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task GetProjectIdByNameAsync_ShouldReturnId_WhenFound()
        {
            // Arrange
            _soapClientMock.Setup(s => s.mc_project_get_id_from_nameAsync("testuser", "testtoken", "FBRLA")).ReturnsAsync("58");

            // Act
            var result = await _projectTools.GetProjectIdByNameAsync("FBRLA");

            // Assert
            result.Should().Contain("Project ID: 58");
        }

        [Fact]
        public async Task GetProjectIdByNameAsync_ShouldReturnNotFound_WhenMissing()
        {
            // Arrange
            _soapClientMock.Setup(s => s.mc_project_get_id_from_nameAsync("testuser", "testtoken", "XYZ")).ReturnsAsync("0");

            // Act
            var result = await _projectTools.GetProjectIdByNameAsync("XYZ");

            // Assert
            result.Should().Contain("No project found");
        }

        [Fact]
        public async Task GetProjectVersionsAsync_ShouldReturnJson_WhenFound()
        {
            // Arrange
            var versions = new[] { new ProjectVersionData { id = "1", name = "1.0.0", project_id = "58" } };
            _soapClientMock.Setup(s => s.mc_project_get_versionsAsync("testuser", "testtoken", "58")).ReturnsAsync(versions);

            // Act
            var result = await _projectTools.GetProjectVersionsAsync(58);

            // Assert
            result.Should().Contain("\"name\": \"1.0.0\"");
        }

        [Fact]
        public async Task GetReleasedVersionsAsync_ShouldReturnJson_WhenFound()
        {
            // Arrange
            var versions = new[] { new ProjectVersionData { id = "1", name = "1.0.0", released = true, releasedSpecified = true } };
            _soapClientMock.Setup(s => s.mc_project_get_released_versionsAsync("testuser", "testtoken", "58")).ReturnsAsync(versions);

            // Act
            var result = await _projectTools.GetReleasedVersionsAsync(58);

            // Assert
            result.Should().Contain("\"name\": \"1.0.0\"");
        }

        [Fact]
        public async Task GetUnreleasedVersionsAsync_ShouldReturnJson_WhenFound()
        {
            // Arrange
            var versions = new[] { new ProjectVersionData { id = "1", name = "2.0.0", released = false, releasedSpecified = true } };
            _soapClientMock.Setup(s => s.mc_project_get_unreleased_versionsAsync("testuser", "testtoken", "58")).ReturnsAsync(versions);

            // Act
            var result = await _projectTools.GetUnreleasedVersionsAsync(58);

            // Assert
            result.Should().Contain("\"name\": \"2.0.0\"");
        }

        [Fact]
        public async Task CreateProjectVersionAsync_ShouldReturnSuccess_WhenCreated()
        {
            // Arrange
            _soapClientMock.Setup(s => s.mc_project_version_addAsync("testuser", "testtoken", It.IsAny<ProjectVersionData>())).ReturnsAsync("77");

            // Act
            var result = await _projectTools.CreateProjectVersionAsync(58, "3.0.0");

            // Assert
            result.Should().Contain("Version created successfully");
            result.Should().Contain("77");
            _soapClientMock.Verify(s => s.mc_project_version_addAsync(
                "testuser", "testtoken",
                It.Is<ProjectVersionData>(v => v.project_id == "58" && v.name == "3.0.0")), Times.Once);
        }

        [Fact]
        public async Task UpdateProjectVersionAsync_ShouldReturnSuccess_WhenUpdated()
        {
            // Arrange
            _soapClientMock.Setup(s => s.mc_project_version_updateAsync("testuser", "testtoken", "77", It.IsAny<ProjectVersionData>())).ReturnsAsync(true);

            // Act
            var result = await _projectTools.UpdateProjectVersionAsync(77, name: "3.1.0");

            // Assert
            result.Should().Contain("updated successfully");
            _soapClientMock.Verify(s => s.mc_project_version_updateAsync(
                "testuser", "testtoken", "77",
                It.Is<ProjectVersionData>(v => v.id == "77" && v.name == "3.1.0")), Times.Once);
        }

        [Fact]
        public async Task DeleteProjectVersionAsync_ShouldReturnSuccess_WhenDeleted()
        {
            // Arrange
            _soapClientMock.Setup(s => s.mc_project_version_deleteAsync("testuser", "testtoken", "77")).ReturnsAsync(true);

            // Act
            var result = await _projectTools.DeleteProjectVersionAsync(77);

            // Assert
            result.Should().Contain("deleted successfully");
        }

        [Fact]
        public async Task CreateCategoryAsync_ShouldReturnSuccess_WhenCreated()
        {
            // Arrange
            _soapClientMock.Setup(s => s.mc_project_add_categoryAsync("testuser", "testtoken", "58", "NPI")).ReturnsAsync("9");

            // Act
            var result = await _projectTools.CreateCategoryAsync(58, "NPI");

            // Assert
            result.Should().Contain("Category created successfully");
        }

        [Fact]
        public async Task DeleteCategoryAsync_ShouldReturnSuccess_WhenDeleted()
        {
            // Arrange
            _soapClientMock.Setup(s => s.mc_project_delete_categoryAsync("testuser", "testtoken", "58", "NPI")).ReturnsAsync("ok");

            // Act
            var result = await _projectTools.DeleteCategoryAsync(58, "NPI");

            // Assert
            result.Should().Contain("deleted successfully");
        }

        [Fact]
        public async Task RenameCategoryAsync_ShouldReturnSuccess_WhenRenamed()
        {
            // Arrange
            _soapClientMock.Setup(s => s.mc_project_rename_category_by_nameAsync("testuser", "testtoken", "58", "NPI", "NPI2", "")).ReturnsAsync("ok");

            // Act
            var result = await _projectTools.RenameCategoryAsync(58, "NPI", "NPI2");

            // Assert
            result.Should().Contain("renamed successfully");
        }

        [Theory]
        [InlineData("GetProjectionsAsync", "projections")]
        [InlineData("GetEtasAsync", "etas")]
        [InlineData("GetProjectViewStatesAsync", "project_view_states")]
        [InlineData("GetViewStatesAsync", "view_states")]
        [InlineData("GetCustomFieldTypesAsync", "custom_field_types")]
        public async Task EnumTools_ShouldReturnJson(string methodName, string kind)
        {
            // Arrange
            var values = new[] { new ObjectRef { id = "10", name = "valor" } };
            SetupEnum(kind, values);

            // Act
            var result = methodName switch
            {
                "GetProjectionsAsync" => await _projectTools.GetProjectionsAsync(),
                "GetEtasAsync" => await _projectTools.GetEtasAsync(),
                "GetProjectViewStatesAsync" => await _projectTools.GetProjectViewStatesAsync(),
                "GetViewStatesAsync" => await _projectTools.GetViewStatesAsync(),
                "GetCustomFieldTypesAsync" => await _projectTools.GetCustomFieldTypesAsync(),
                _ => throw new InvalidOperationException()
            };

            // Assert
            result.Should().Contain("\"name\": \"valor\"");
        }

        [Fact]
        public async Task GetEnumAsync_ShouldReturnRawValue()
        {
            // Arrange
            _soapClientMock.Setup(s => s.mc_enum_getAsync("testuser", "testtoken", "status")).ReturnsAsync("10:new,50:assigned");

            // Act
            var result = await _projectTools.GetEnumAsync("status");

            // Assert
            result.Should().Contain("10:new");
        }

        private void SetupEnum(string kind, ObjectRef[] values)
        {
            switch (kind)
            {
                case "projections":
                    _soapClientMock.Setup(s => s.mc_enum_projectionsAsync("testuser", "testtoken")).ReturnsAsync(values);
                    break;
                case "etas":
                    _soapClientMock.Setup(s => s.mc_enum_etasAsync("testuser", "testtoken")).ReturnsAsync(values);
                    break;
                case "project_view_states":
                    _soapClientMock.Setup(s => s.mc_enum_project_view_statesAsync("testuser", "testtoken")).ReturnsAsync(values);
                    break;
                case "view_states":
                    _soapClientMock.Setup(s => s.mc_enum_view_statesAsync("testuser", "testtoken")).ReturnsAsync(values);
                    break;
                case "custom_field_types":
                    _soapClientMock.Setup(s => s.mc_enum_custom_field_typesAsync("testuser", "testtoken")).ReturnsAsync(values);
                    break;
            }
        }
    }
}
