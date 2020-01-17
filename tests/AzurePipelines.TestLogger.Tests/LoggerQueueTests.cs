using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using NUnit.Framework;
using Shouldly;

namespace AzurePipelines.TestLogger.Tests
{
    [TestFixture]
    public class LoggerQueueTests
    {
        private const string _dateFormatString = "yyyy-MM-ddTHH:mm:ss.FFFZ";

        [Test]
        public void CreateTestRunWithoutFilename()
        {
            // Given
            TestApiClient apiClient = new TestApiClient(_ => "{ \"id\": 1234 }");
            LoggerQueue loggerQueue = new LoggerQueue(apiClient, "987", "foo", "bar");

            // When
            int id = loggerQueue.CreateTestRun(CancellationToken.None).Result;

            // Then
            id.ShouldBe(1234);
            apiClient.Messages.ShouldBe(new[]
            {
                new ClientMessage(
                    HttpMethod.Post,
                    null,
                    "5.0",
                    $@"{{
                        ""name"": ""Unknown Test Source (OS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}, Job: bar, Agent: foo)"",
                        ""build"": {{""id"":""987""}},
                        ""startedDate"": ""{loggerQueue.StartedDate.ToString(_dateFormatString)}"",
                        ""isAutomated"": true
                    }}")
            });
        }

        [Test]
        public void CreateTestRunWithFilename()
        {
            // Given
            TestApiClient apiClient = new TestApiClient(_ => "{ \"id\": 1234 }");
            LoggerQueue loggerQueue = new LoggerQueue(apiClient, "987", "foo", "bar")
            {
                Source = "Fizz.Buzz"
            };

            // When
            int id = loggerQueue.CreateTestRun(CancellationToken.None).Result;

            // Then
            id.ShouldBe(1234);
            apiClient.Messages.ShouldBe(new[]
            {
                new ClientMessage(
                    HttpMethod.Post,
                    null,
                    "5.0",
                    $@"{{
                        ""name"": ""Fizz.Buzz (OS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}, Job: bar, Agent: foo)"",
                        ""build"": {{""id"":""987""}},
                        ""startedDate"": ""{loggerQueue.StartedDate.ToString(_dateFormatString)}"",
                        ""isAutomated"": true
                    }}")
            });
        }

        [Test]
        public void GetSourceWithoutExtension()
        {
            // Given
            TestTestResult testResult = new TestTestResult
            {
                Source = "/a/b/Foo.Bar"
            };

            // When
            string source = LoggerQueue.GetSource(new[] { testResult });

            // Then
            source.ShouldBe("Foo.Bar");
        }

        [Test]
        public void GetSourceWithExtension()
        {
            // Given
            TestTestResult testResult = new TestTestResult
            {
                Source = "/a/b/Foo.Bar.dll"
            };

            // When
            string source = LoggerQueue.GetSource(new[] { testResult });

            // Then
            source.ShouldBe("Foo.Bar");
        }

        [Test]
        public void GetMissingSource()
        {
            // Given
            TestTestResult testResult = new TestTestResult();

            // When
            string source = LoggerQueue.GetSource(new[] { testResult });

            // Then
            source.ShouldBeNull();
        }

        [Test]
        public void GroupTestResults()
        {
            // Given
            TestApiClient apiClient = new TestApiClient();
            LoggerQueue loggerQueue = new LoggerQueue(apiClient, "987", "foo", "bar")
            {
                Source = "Fizz.Buzz"
            };
            ITestResult[] testResults = new[]
            {
                new TestTestResult
                {
                    FullyQualifiedName = "Fizz.Buzz.FooFixture.BarMethod"
                },
                new TestTestResult
                {
                    FullyQualifiedName = "Fizz.Buzz.FooFixture.BazMethod"
                },
                new TestTestResult
                {
                    FullyQualifiedName = "Fizz.Buzz.FutzFixture.BooMethod(\"x.y\")"
                },
                new TestTestResult
                {
                    FullyQualifiedName = "Fizz.Buzz.FutzFixture.NestedFixture.BlitzMethod"
                }
            };

            // When
            IEnumerable<IGrouping<string, ITestResult>> testResultsByParent = loggerQueue.GroupTestResultsByParent(testResults);

            // Then
            testResultsByParent.Select(x => x.Key).ShouldBe(
                new[]
                {
                    "FooFixture",
                    "FutzFixture",
                    "FutzFixture.NestedFixture"
                },
                true);
        }

        [Test]
        public void CreateParents()
        {
            // Given
            TestApiClient apiClient = new TestApiClient(_ =>
                @"{
                    ""count"": 2,
                    ""value"": [
                        {
                            ""id"": 100
                        },
                        {
                            ""id"": 101
                        }
                    ]
                }");
            LoggerQueue loggerQueue = new LoggerQueue(apiClient, "987", "foo", "bar")
            {
                Source = "Fizz.Buzz",
                RunId = 1
            };
            loggerQueue.Parents.Add("FitzFixture", new TestResultParent(123));
            ITestResult[] testResults = new[]
            {
                new TestTestResult
                {
                    FullyQualifiedName = "Fizz.Buzz.FooFixture.BarMethod"
                },
                new TestTestResult
                {
                    FullyQualifiedName = "Fizz.Buzz.FitzFixture.BazMethod"
                },
                new TestTestResult
                {
                    FullyQualifiedName = "Fizz.Buzz.FutzFixture.NestedFixture.BooMethod(\"x.y\")"
                }
            };
            IEnumerable<IGrouping<string, ITestResult>> testResultsByParent = loggerQueue.GroupTestResultsByParent(testResults);

            // When
            loggerQueue.CreateParents(testResultsByParent, CancellationToken.None).Wait();

            // Then
            apiClient.Messages.ShouldBe(new[]
            {
                new ClientMessage(
                    HttpMethod.Post,
                    "/1/results",
                    "5.0",
                    $@"[
                        {{
                            ""testCaseTitle"": ""FooFixture"",
                            ""automatedTestName"": ""FooFixture"",
                            ""resultGroupType"": ""generic"",
                            ""outcome"": ""Passed"",
                            ""state"": ""InProgress"",
                            ""startedDate"": ""{loggerQueue.Parents["FooFixture"].StartedDate.ToString(_dateFormatString)}"",
                            ""automatedTestType"": ""UnitTest"",
                            ""automatedTestTypeId"": ""13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b"",
                            ""automatedTestStorage"": ""Fizz.Buzz""
                        }},
                        {{
                            ""testCaseTitle"": ""FutzFixture.NestedFixture"",
                            ""automatedTestName"": ""FutzFixture.NestedFixture"",
                            ""resultGroupType"": ""generic"",
                            ""outcome"": ""Passed"",
                            ""state"": ""InProgress"",
                            ""startedDate"": ""{loggerQueue.Parents["FutzFixture.NestedFixture"].StartedDate.ToString(_dateFormatString)}"",
                            ""automatedTestType"": ""UnitTest"",
                            ""automatedTestTypeId"": ""13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b"",
                            ""automatedTestStorage"": ""Fizz.Buzz""
                        }}
                    ]")
            });
            loggerQueue.Parents.Keys.ShouldBe(new[] { "FitzFixture", "FooFixture", "FutzFixture.NestedFixture" }, true);
            loggerQueue.Parents.Values.Select(x => x.Id).ShouldBe(new[] { 123, 100, 101 }, true);
        }
    }
}
