using FluentAssertions;
using MantisMcpServer.Services;
using MantisMcpServer.Tools;
using MantisService;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace MantisMcpServer.Tests
{
    public class IssueToolsTests
    {
        private readonly Mock<IMantisClient> _mantisClientMock;
        private readonly Mock<IMantisSoapClient> _soapClientMock;
        private readonly Mock<ILogger<IssueTools>> _loggerMock;
        private readonly IssueTools _issueTools;

        public IssueToolsTests()
        {
            _mantisClientMock = new Mock<IMantisClient>();
            _soapClientMock = new Mock<IMantisSoapClient>();
            _loggerMock = new Mock<ILogger<IssueTools>>();
            
            _mantisClientMock.Setup(m => m.CreateSoapClient()).Returns(_soapClientMock.Object);
            _mantisClientMock.Setup(m => m.Username).Returns("testuser");
            _mantisClientMock.Setup(m => m.Token).Returns("testtoken");

            _issueTools = new IssueTools(_mantisClientMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task GetIssueAsync_ShouldReturnJson_WhenIssueExists()
        {
            // Arrange
            var issueId = 123;
            var expectedIssue = new IssueData { id = issueId.ToString(), summary = "Test Issue" };
            
            _soapClientMock.Setup(s => s.mc_issue_getAsync("testuser", "testtoken", "123"))
                .ReturnsAsync(expectedIssue);

            // Act
            var result = await _issueTools.GetIssueAsync(issueId);

            // Assert
            result.Should().Contain("Test Issue");
            result.Should().Contain("\"id\": \"123\"");
        }

        [Fact]
        public async Task GetIssueAsync_ShouldReturnErrorMessage_WhenExceptionOccurs()
        {
            // Arrange
            var issueId = 123;
            _soapClientMock.Setup(s => s.mc_issue_getAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Soap Fault"));

            // Act
            var result = await _issueTools.GetIssueAsync(issueId);

            // Assert
            result.Should().Contain("Error retrieving issue 123");
            result.Should().Contain("Soap Fault");
        }

        [Fact]
        public async Task UpdateIssueAsync_ShouldUpdateCustomFields_WhenIssueExists()
        {
            // Arrange
            var issue = new IssueData
            {
                id = "123",
                custom_fields = new[]
                {
                    new CustomFieldValueForIssueData { field = new ObjectRef { name = "Area" }, value = "ENGINEERING" }
                }
            };
            _soapClientMock.Setup(s => s.mc_issue_getAsync("testuser", "testtoken", "123")).ReturnsAsync(issue);
            _soapClientMock.Setup(s => s.mc_issue_updateAsync("testuser", "testtoken", "123", It.IsAny<IssueData>())).ReturnsAsync(true);

            // Act
            var result = await _issueTools.UpdateIssueAsync(123, "{\"Backlogs\": \"item1\", \"Area\": \"IT\"}");

            // Assert
            result.Should().Contain("updated successfully");
            _soapClientMock.Verify(s => s.mc_issue_updateAsync(
                "testuser", "testtoken", "123",
                It.Is<IssueData>(i =>
                    i.custom_fields != null &&
                    i.custom_fields.Any(f => f.field != null && f.field.name == "Backlogs" && f.value == "item1") &&
                    i.custom_fields.Any(f => f.field != null && f.field.name == "Area" && f.value == "IT"))), Times.Once);
        }

        [Fact]
        public async Task UpdateIssueAsync_ShouldReturnError_WhenJsonInvalid()
        {
            // Act
            var result = await _issueTools.UpdateIssueAsync(123, "nao-e-json");

            // Assert
            result.Should().Contain("must be a valid JSON object");
        }

        [Fact]
        public async Task UpdateIssueAsync_ShouldReturnError_WhenExceptionOccurs()
        {
            // Arrange
            _soapClientMock.Setup(s => s.mc_issue_getAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Soap Fault"));

            // Act
            var result = await _issueTools.UpdateIssueAsync(123, "{\"Backlogs\": \"x\"}");

            // Assert
            result.Should().Contain("Error updating issue");
        }

        [Fact]
        public async Task UpdateNoteAsync_ShouldUpdateNoteText_WhenNoteExists()
        {
            // Arrange
            var note = new IssueNoteData { id = "55", text = "texto antigo" };
            var updatedNote = new IssueNoteData { id = "55", text = "texto novo" };
            _soapClientMock.Setup(s => s.mc_issue_getAsync("testuser", "testtoken", "123"))
                .ReturnsAsync(new IssueData { id = "123", notes = new[] { note } });
            _soapClientMock.SetupSequence(s => s.mc_issue_getAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new IssueData { id = "123", notes = new[] { note } })
                .ReturnsAsync(new IssueData { id = "123", notes = new[] { updatedNote } });
            _soapClientMock.Setup(s => s.mc_issue_note_updateAsync("testuser", "testtoken", It.IsAny<IssueNoteData>()))
                .ReturnsAsync(false);

            // Act
            var result = await _issueTools.UpdateNoteAsync(123, 55, "texto novo");

            // Assert
            result.Should().Contain("updated successfully");
            _soapClientMock.Verify(s => s.mc_issue_note_updateAsync(
                "testuser", "testtoken",
                It.Is<IssueNoteData>(n => n.id == "55" && n.text == "texto novo")), Times.Once);
        }

        [Fact]
        public async Task UpdateNoteAsync_ShouldPreserveVisibility_WhenNotSpecified()
        {
            // Arrange
            var note = new IssueNoteData
            {
                id = "55",
                text = "texto antigo",
                view_state = new ObjectRef { id = "50" }
            };
            var updatedNote = new IssueNoteData { id = "55", text = "texto novo", view_state = new ObjectRef { id = "50" } };
            _soapClientMock.SetupSequence(s => s.mc_issue_getAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new IssueData { id = "123", notes = new[] { note } })
                .ReturnsAsync(new IssueData { id = "123", notes = new[] { updatedNote } });
            _soapClientMock.Setup(s => s.mc_issue_note_updateAsync("testuser", "testtoken", It.IsAny<IssueNoteData>()))
                .ReturnsAsync(true);

            // Act
            var result = await _issueTools.UpdateNoteAsync(123, 55, "texto novo");

            // Assert
            result.Should().Contain("updated successfully");
            _soapClientMock.Verify(s => s.mc_issue_note_updateAsync(
                "testuser", "testtoken",
                It.Is<IssueNoteData>(n => n.view_state != null && n.view_state.id == "50")), Times.Once);
        }

        [Fact]
        public async Task UpdateNoteAsync_ShouldApplySpecifiedVisibility()
        {
            // Arrange
            var note = new IssueNoteData
            {
                id = "55",
                text = "texto antigo",
                view_state = new ObjectRef { id = "10" }
            };
            var updatedNote = new IssueNoteData { id = "55", text = "texto novo", view_state = new ObjectRef { id = "50" } };
            _soapClientMock.SetupSequence(s => s.mc_issue_getAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new IssueData { id = "123", notes = new[] { note } })
                .ReturnsAsync(new IssueData { id = "123", notes = new[] { updatedNote } });
            _soapClientMock.Setup(s => s.mc_issue_note_updateAsync("testuser", "testtoken", It.IsAny<IssueNoteData>()))
                .ReturnsAsync(true);

            // Act
            var result = await _issueTools.UpdateNoteAsync(123, 55, "texto novo", is_private: true);

            // Assert
            result.Should().Contain("updated successfully");
            _soapClientMock.Verify(s => s.mc_issue_note_updateAsync(
                "testuser", "testtoken",
                It.Is<IssueNoteData>(n => n.view_state != null && n.view_state.id == "50")), Times.Once);
        }

        [Fact]
        public async Task UpdateNoteAsync_ShouldReturnNotFound_WhenNoteDoesNotExist()
        {
            // Arrange
            var issue = new IssueData { id = "123", notes = new[] { new IssueNoteData { id = "99", text = "x" } } };
            _soapClientMock.Setup(s => s.mc_issue_getAsync("testuser", "testtoken", "123")).ReturnsAsync(issue);

            // Act
            var result = await _issueTools.UpdateNoteAsync(123, 55, "texto novo");

            // Assert
            result.Should().Contain("not found on issue");
        }

        [Fact]
        public async Task UpdateNoteAsync_ShouldReturnError_WhenExceptionOccurs()
        {
            // Arrange
            _soapClientMock.Setup(s => s.mc_issue_getAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Soap Fault"));

            // Act
            var result = await _issueTools.UpdateNoteAsync(123, 55, "texto novo");

            // Assert
            result.Should().Contain("Error updating note");
            result.Should().Contain("Soap Fault");
        }

        [Fact]
        public async Task DeleteNoteAsync_ShouldReturnSuccess_WhenNoteDeleted()
        {
            // Arrange
            _soapClientMock.Setup(s => s.mc_issue_note_deleteAsync("testuser", "testtoken", "55")).ReturnsAsync(true);

            // Act
            var result = await _issueTools.DeleteNoteAsync(123, 55);

            // Assert
            result.Should().Contain("deleted successfully");
        }

        [Fact]
        public async Task DeleteNoteAsync_ShouldReturnError_WhenExceptionOccurs()
        {
            // Arrange
            _soapClientMock.Setup(s => s.mc_issue_note_deleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Soap Fault"));

            // Act
            var result = await _issueTools.DeleteNoteAsync(123, 55);

            // Assert
            result.Should().Contain("Error deleting note");
        }

        [Fact]
        public async Task DeleteAttachmentAsync_ShouldReturnSuccess_WhenDeleted()
        {
            // Arrange
            _soapClientMock.Setup(s => s.mc_issue_attachment_deleteAsync("testuser", "testtoken", "77")).ReturnsAsync(true);

            // Act
            var result = await _issueTools.DeleteAttachmentAsync(77);

            // Assert
            result.Should().Contain("deleted successfully");
        }

        [Fact]
        public async Task IssueExistsAsync_ShouldReturnExists_WhenIssueFound()
        {
            // Arrange
            _soapClientMock.Setup(s => s.mc_issue_existsAsync("testuser", "testtoken", "123")).ReturnsAsync(true);

            // Act
            var result = await _issueTools.IssueExistsAsync(123);

            // Assert
            result.Should().Contain("exists");
        }

        [Fact]
        public async Task IssueExistsAsync_ShouldReturnNotExists_WhenIssueMissing()
        {
            // Arrange
            _soapClientMock.Setup(s => s.mc_issue_existsAsync("testuser", "testtoken", "123")).ReturnsAsync(false);

            // Act
            var result = await _issueTools.IssueExistsAsync(123);

            // Assert
            result.Should().Contain("does not exist");
        }

        [Fact]
        public async Task GetIssueBatchAsync_ShouldReturnJson_WhenIssuesFound()
        {
            // Arrange
            var issues = new[] { new IssueData { id = "123", summary = "A" }, new IssueData { id = "124", summary = "B" } };
            _soapClientMock.Setup(s => s.mc_issues_getAsync("testuser", "testtoken", It.Is<string[]>(a => a.Length == 2 && a[0] == "123")))
                .ReturnsAsync(issues);

            // Act
            var result = await _issueTools.GetIssueBatchAsync("123,124");

            // Assert
            result.Should().Contain("\"summary\": \"A\"");
            result.Should().Contain("\"id\": \"124\"");
        }

        [Fact]
        public async Task GetIssueHeadersAsync_ShouldReturnJson_WhenFound()
        {
            // Arrange
            var headers = new[] { new IssueHeaderData { id = "123", summary = "Header" } };
            _soapClientMock.Setup(s => s.mc_issues_get_headerAsync("testuser", "testtoken", It.IsAny<string[]>())).ReturnsAsync(headers);

            // Act
            var result = await _issueTools.GetIssueHeadersAsync("123");

            // Assert
            result.Should().Contain("\"summary\": \"Header\"");
        }

        [Fact]
        public async Task DeleteIssueAsync_ShouldReturnSuccess_WhenDeleted()
        {
            // Arrange
            _soapClientMock.Setup(s => s.mc_issue_deleteAsync("testuser", "testtoken", "123")).ReturnsAsync(true);

            // Act
            var result = await _issueTools.DeleteIssueAsync(123);

            // Assert
            result.Should().Contain("deleted successfully");
        }

        [Fact]
        public async Task GetBiggestIssueIdAsync_ShouldReturnId_WhenFound()
        {
            // Arrange
            _soapClientMock.Setup(s => s.mc_issue_get_biggest_idAsync("testuser", "testtoken", "58")).ReturnsAsync("9999");

            // Act
            var result = await _issueTools.GetBiggestIssueIdAsync(58);

            // Assert
            result.Should().Contain("9999");
        }

        [Fact]
        public async Task CheckinIssueAsync_ShouldReturnSuccess_WhenCheckedIn()
        {
            // Arrange
            _soapClientMock.Setup(s => s.mc_issue_checkinAsync("testuser", "testtoken", "123", "comentario", true)).ReturnsAsync(true);

            // Act
            var result = await _issueTools.CheckinIssueAsync(123, "comentario", true);

            // Assert
            result.Should().Contain("checked in successfully");
        }

        [Fact]
        public async Task SearchIssueIdsAsync_ShouldReturnIds_WhenFound()
        {
            // Arrange
            _soapClientMock.Setup(s => s.mc_filter_search_issue_idsAsync("testuser", "testtoken", It.IsAny<FilterSearchData>(), "1", "50"))
                .ReturnsAsync(new[] { "123", "124" });

            // Act
            var result = await _issueTools.SearchIssueIdsAsync("teste");

            // Assert
            result.Should().Contain("123");
            result.Should().Contain("124");
        }

        [Fact]
        public async Task SearchIssueIdsAsync_ShouldReturnNoIssues_WhenEmpty()
        {
            // Arrange
            _soapClientMock.Setup(s => s.mc_filter_search_issue_idsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<FilterSearchData>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(Array.Empty<string>());

            // Act
            var result = await _issueTools.SearchIssueIdsAsync("teste");

            // Assert
            result.Should().Contain("No issues found");
        }

        [Fact]
        public async Task UpdateIssueStatusAsync_ShouldClearExistingNotes_WhenUpdatingStatusWithoutNote()
        {
            // Arrange
            var issue = new IssueData
            {
                id = "123",
                status = new ObjectRef { id = "20" },
                notes = new[]
                {
                    new IssueNoteData { id = "1", text = "Nota antiga 1" },
                    new IssueNoteData { id = "2", text = "Nota antiga 2" }
                }
            };

            _soapClientMock.Setup(s => s.mc_issue_getAsync("testuser", "testtoken", "123"))
                .ReturnsAsync(issue);

            IssueData? updatedIssue = null;
            _soapClientMock.Setup(s => s.mc_issue_updateAsync("testuser", "testtoken", "123", It.IsAny<IssueData>()))
                .Callback<string, string, string, IssueData>((u, t, id, data) => updatedIssue = data)
                .ReturnsAsync(true);

            // Act
            var result = await _issueTools.UpdateIssueStatusAsync(123, 10);

            // Assert
            result.Should().Contain("updated successfully");
            updatedIssue.Should().NotBeNull();
            updatedIssue!.status.id.Should().Be("10");
            updatedIssue.notes.Should().BeNull();
        }

        [Fact]
        public async Task UpdateIssueStatusAsync_ShouldOnlyPassNewNote_WhenNoteIsProvided()
        {
            // Arrange
            var issue = new IssueData
            {
                id = "123",
                status = new ObjectRef { id = "20" },
                notes = new[]
                {
                    new IssueNoteData { id = "1", text = "Nota antiga" }
                }
            };

            _soapClientMock.Setup(s => s.mc_issue_getAsync("testuser", "testtoken", "123"))
                .ReturnsAsync(issue);

            IssueData? updatedIssue = null;
            _soapClientMock.Setup(s => s.mc_issue_updateAsync("testuser", "testtoken", "123", It.IsAny<IssueData>()))
                .Callback<string, string, string, IssueData>((u, t, id, data) => updatedIssue = data)
                .ReturnsAsync(true);

            // Act
            var result = await _issueTools.UpdateIssueStatusAsync(123, 50, note: "Nova nota de encaminhamento");

            // Assert
            result.Should().Contain("updated successfully");
            updatedIssue.Should().NotBeNull();
            updatedIssue!.status.id.Should().Be("50");
            updatedIssue.notes.Should().HaveCount(1);
            updatedIssue.notes![0].text.Should().Be("Nova nota de encaminhamento");
        }

        [Fact]
        public async Task UpdateIssueAsync_ShouldClearExistingNotes_WhenUpdatingCustomFields()
        {
            // Arrange
            var issue = new IssueData
            {
                id = "123",
                custom_fields = Array.Empty<CustomFieldValueForIssueData>(),
                notes = new[]
                {
                    new IssueNoteData { id = "1", text = "Nota antiga" }
                }
            };

            _soapClientMock.Setup(s => s.mc_issue_getAsync("testuser", "testtoken", "123"))
                .ReturnsAsync(issue);

            IssueData? updatedIssue = null;
            _soapClientMock.Setup(s => s.mc_issue_updateAsync("testuser", "testtoken", "123", It.IsAny<IssueData>()))
                .Callback<string, string, string, IssueData>((u, t, id, data) => updatedIssue = data)
                .ReturnsAsync(true);

            // Act
            var result = await _issueTools.UpdateIssueAsync(123, "{\"Backlogs\": \"Item 1\"}");

            // Assert
            result.Should().Contain("updated successfully");
            updatedIssue.Should().NotBeNull();
            updatedIssue!.notes.Should().BeNull();
        }
    }
}
