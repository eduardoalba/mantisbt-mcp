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
    }
}
