using System.Collections.Generic;
using FakeItEasy;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using NUnit.Framework;

namespace AzurePipelines.TestLogger.Tests
{
    [TestFixture]
    public class TestLoggerTests
    {
        [Test]
        public void InitializeWithAccessToken()
        {
            // Given
            IEnvironmentVariableProvider environmentVariableProvider = A.Fake<IEnvironmentVariableProvider>();
            IApiClientFactory apiClientFactory = A.Fake<IApiClientFactory>();
            AzurePipelinesTestLogger testLogger = new AzurePipelinesTestLogger(environmentVariableProvider, apiClientFactory);
            TestLoggerEvents events = A.Fake<TestLoggerEvents>();

            A.CallTo(() => environmentVariableProvider.GetEnvironmentVariable(EnvironmentVariableNames.AccessToken)).Returns("accessToken");
            A.CallTo(() => environmentVariableProvider.GetEnvironmentVariable(EnvironmentVariableNames.TeamFoundationCollectionUri)).Returns("teamFoundationCollectionUri");
            A.CallTo(() => environmentVariableProvider.GetEnvironmentVariable(EnvironmentVariableNames.TeamProject)).Returns("teamProject");
            A.CallTo(() => environmentVariableProvider.GetEnvironmentVariable(EnvironmentVariableNames.BuildId)).Returns("buildId");
            A.CallTo(() => environmentVariableProvider.GetEnvironmentVariable(EnvironmentVariableNames.BuildRequestedFor)).Returns("buildRequestedFor");
            A.CallTo(() => environmentVariableProvider.GetEnvironmentVariable(EnvironmentVariableNames.AgentName)).Returns("agentName");
            A.CallTo(() => environmentVariableProvider.GetEnvironmentVariable(EnvironmentVariableNames.AgentJobName)).Returns("agentJobName");

            // When
            testLogger.Initialize(events, string.Empty);

            // Then
            A.CallTo(() => apiClientFactory.CreateWithAccessToken(null, null, null, null)).WithAnyArguments().MustHaveHappenedOnceExactly();
        }

        [Test]
        public void InitializeWithDefaultCredentials()
        {
            // Given
            IEnvironmentVariableProvider environmentVariableProvider = A.Fake<IEnvironmentVariableProvider>();
            IApiClientFactory apiClientFactory = A.Fake<IApiClientFactory>();
            AzurePipelinesTestLogger testLogger = new AzurePipelinesTestLogger(environmentVariableProvider, apiClientFactory);
            TestLoggerEvents events = A.Fake<TestLoggerEvents>();
            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                { "UseDefaultCredentials", "true" }
            };

            A.CallTo(() => environmentVariableProvider.GetEnvironmentVariable(EnvironmentVariableNames.TeamFoundationCollectionUri)).Returns("teamFoundationCollectionUri");
            A.CallTo(() => environmentVariableProvider.GetEnvironmentVariable(EnvironmentVariableNames.TeamProject)).Returns("teamProject");
            A.CallTo(() => environmentVariableProvider.GetEnvironmentVariable(EnvironmentVariableNames.BuildId)).Returns("buildId");
            A.CallTo(() => environmentVariableProvider.GetEnvironmentVariable(EnvironmentVariableNames.BuildRequestedFor)).Returns("buildRequestedFor");
            A.CallTo(() => environmentVariableProvider.GetEnvironmentVariable(EnvironmentVariableNames.AgentName)).Returns("agentName");
            A.CallTo(() => environmentVariableProvider.GetEnvironmentVariable(EnvironmentVariableNames.AgentJobName)).Returns("agentJobName");

            // When
            testLogger.Initialize(events, parameters);

            // Then
            A.CallTo(() => apiClientFactory.CreateWithDefaultCredentials(null, null, null)).WithAnyArguments().MustHaveHappenedOnceExactly();
        }
    }
}
