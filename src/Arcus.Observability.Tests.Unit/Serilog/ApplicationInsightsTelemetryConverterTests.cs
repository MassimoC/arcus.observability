﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Arcus.Observability.Telemetry.Core;
using Arcus.Observability.Telemetry.Serilog.Sinks.ApplicationInsights;
using Arcus.Observability.Telemetry.Serilog.Sinks.ApplicationInsights.Converters;
using Arcus.Observability.Tests.Core;
using Bogus;
using Bogus.DataSets;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Xunit;
using static Arcus.Observability.Telemetry.Core.ContextProperties;
using HttpMethod = Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Arcus.Observability.Tests.Unit.Serilog
{
    [Trait("Category", "Unit")]
    public class ApplicationInsightsTelemetryConverterTests
    {
        private readonly Faker _bogusGenerator = new Faker();

        [Fact]
        public void LogRequestMessage_WithRequestAndResponse_CreatesRequestTelemetry()
        {
            // Arrange
            var spySink = new InMemoryLogSink();
            string operationId = $"operation-id-{Guid.NewGuid()}";
            ILogger logger = CreateLogger(
                spySink, config => config.Enrich.WithProperty(ContextProperties.Correlation.OperationId, operationId));

            var telemetryContext = new Dictionary<string, object>
            {
                ["Client"] = "https://localhost",
                ["ContentType"] = "application/json",
            };
            var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, new Uri("https://" + "localhost" + "/api/v1/health"));
            var statusCode = HttpStatusCode.OK;
            var response = new HttpResponseMessage(statusCode);
            TimeSpan duration = TimeSpan.FromSeconds(5);
            logger.LogRequest(request, response, duration, telemetryContext);

            LogEvent logEvent = Assert.Single(spySink.CurrentLogEmits);
            Assert.NotNull(logEvent);

            var converter = ApplicationInsightsTelemetryConverter.Create();

            // Act
            IEnumerable<ITelemetry> telemetries = converter.Convert(logEvent, formatProvider: null);

            // Assert
            AssertDoesNotContainLogProperty(logEvent, RequestTracking.RequestMethod);
            AssertDoesNotContainLogProperty(logEvent, RequestTracking.RequestHost);
            AssertDoesNotContainLogProperty(logEvent, RequestTracking.RequestDuration);
            AssertDoesNotContainLogProperty(logEvent, RequestTracking.ResponseStatusCode);
            AssertDoesNotContainLogProperty(logEvent, RequestTracking.RequestTime);
            AssertDoesNotContainLogProperty(logEvent, RequestTracking.ResponseStatusCode);
            AssertDoesNotContainLogProperty(logEvent, ContextProperties.TelemetryContext);
            Assert.Collection(telemetries, telemetry =>
            {
                var requestTelemetry = Assert.IsType<RequestTelemetry>(telemetry);
                Assert.Equal("GET /api/v1/health", requestTelemetry.Name);
                Assert.Equal(duration, requestTelemetry.Duration);
                Assert.Equal(((int)statusCode).ToString(), requestTelemetry.ResponseCode);
                Assert.True(requestTelemetry.Success);
                Assert.Equal(operationId, requestTelemetry.Id);
                Assert.Equal(new Uri("https://localhost/api/v1/health"), requestTelemetry.Url);
                AssertOperationContext(requestTelemetry, operationId);

                AssertContainsTelemetryProperty(requestTelemetry, "Client", "https://localhost");
                AssertContainsTelemetryProperty(requestTelemetry, "ContentType", "application/json");
            });
        }

        [Fact]
        public void LogRequestMessage_WithRequestAndResponseStatusCode_CreatesRequestTelemetry()
        {
            // Arrange
            var spySink = new InMemoryLogSink();
            string operationId = $"operation-id-{Guid.NewGuid()}";
            ILogger logger = CreateLogger(
                spySink, config => config.Enrich.WithProperty(ContextProperties.Correlation.OperationId, operationId));

            var telemetryContext = new Dictionary<string, object>
            {
                ["Client"] = "https://localhost",
                ["ContentType"] = "application/json",
            };
            var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, new Uri("https://" + "localhost" + "/api/v1/health"));
            var statusCode = HttpStatusCode.OK;
            TimeSpan duration = TimeSpan.FromSeconds(5);
            logger.LogRequest(request, statusCode, duration, telemetryContext);

            LogEvent logEvent = Assert.Single(spySink.CurrentLogEmits);
            Assert.NotNull(logEvent);

            var converter = ApplicationInsightsTelemetryConverter.Create();

            // Act
            IEnumerable<ITelemetry> telemetries = converter.Convert(logEvent, formatProvider: null);

            // Assert
            AssertDoesNotContainLogProperty(logEvent, RequestTracking.RequestMethod);
            AssertDoesNotContainLogProperty(logEvent, RequestTracking.RequestHost);
            AssertDoesNotContainLogProperty(logEvent, RequestTracking.RequestDuration);
            AssertDoesNotContainLogProperty(logEvent, RequestTracking.ResponseStatusCode);
            AssertDoesNotContainLogProperty(logEvent, RequestTracking.RequestTime);
            AssertDoesNotContainLogProperty(logEvent, RequestTracking.ResponseStatusCode);
            AssertDoesNotContainLogProperty(logEvent, ContextProperties.TelemetryContext);
            Assert.Collection(telemetries, telemetry =>
            {
                var requestTelemetry = Assert.IsType<RequestTelemetry>(telemetry);
                Assert.Equal("GET /api/v1/health", requestTelemetry.Name);
                Assert.Equal(duration, requestTelemetry.Duration);
                Assert.Equal(((int)statusCode).ToString(), requestTelemetry.ResponseCode);
                Assert.True(requestTelemetry.Success);
                Assert.Equal(operationId, requestTelemetry.Id);
                Assert.Equal(new Uri("https://localhost/api/v1/health"), requestTelemetry.Url);
                AssertOperationContext(requestTelemetry, operationId);

                AssertContainsTelemetryProperty(requestTelemetry, "Client", "https://localhost");
                AssertContainsTelemetryProperty(requestTelemetry, "ContentType", "application/json");
            });
        }

        [Fact]
        public void LogRequest_WithRequestAndResponse_CreatesRequestTelemetry()
        {
            // Arrange
            var spySink = new InMemoryLogSink();
            string operationId = $"operation-id-{Guid.NewGuid()}";
            ILogger logger = CreateLogger(
                spySink, config => config.Enrich.WithProperty(ContextProperties.Correlation.OperationId, operationId));

            var telemetryContext = new Dictionary<string, object>
            {
                ["Client"] = "https://localhost",
                ["ContentType"] = "application/json",
            };
            var statusCode = HttpStatusCode.OK;
            HttpRequest request = CreateStubRequest(HttpMethod.Get, "https", "localhost", "/api/v1/health");
            HttpResponse response = CreateStubResponse(statusCode);
            TimeSpan duration = TimeSpan.FromSeconds(5);
            logger.LogRequest(request, response, duration, telemetryContext);

            LogEvent logEvent = Assert.Single(spySink.CurrentLogEmits);
            Assert.NotNull(logEvent);

            var converter = ApplicationInsightsTelemetryConverter.Create();

            // Act
            IEnumerable<ITelemetry> telemetries = converter.Convert(logEvent, formatProvider: null);

            // Assert
            AssertDoesNotContainLogProperty(logEvent, RequestTracking.RequestMethod);
            AssertDoesNotContainLogProperty(logEvent, RequestTracking.RequestHost);
            AssertDoesNotContainLogProperty(logEvent, RequestTracking.RequestDuration);
            AssertDoesNotContainLogProperty(logEvent, RequestTracking.ResponseStatusCode);
            AssertDoesNotContainLogProperty(logEvent, RequestTracking.RequestTime);
            AssertDoesNotContainLogProperty(logEvent, RequestTracking.ResponseStatusCode);
            AssertDoesNotContainLogProperty(logEvent, ContextProperties.TelemetryContext);
            Assert.Collection(telemetries, telemetry =>
            {
                var requestTelemetry = Assert.IsType<RequestTelemetry>(telemetry);
                Assert.Equal("GET /api/v1/health", requestTelemetry.Name);
                Assert.Equal(duration, requestTelemetry.Duration);
                Assert.Equal(((int)statusCode).ToString(), requestTelemetry.ResponseCode);
                Assert.True(requestTelemetry.Success);
                Assert.Equal(operationId, requestTelemetry.Id);
                Assert.Equal(new Uri("https://localhost/api/v1/health"), requestTelemetry.Url);
                AssertOperationContext(requestTelemetry, operationId);

                AssertContainsTelemetryProperty(requestTelemetry, "Client", "https://localhost");
                AssertContainsTelemetryProperty(requestTelemetry, "ContentType", "application/json");
            });
        }

        [Fact]
        public void LogRequest_WithRequestAndResponseStatusCode_CreatesRequestTelemetry()
        {
            // Arrange
            var spySink = new InMemoryLogSink();
            string operationId = $"operation-id-{Guid.NewGuid()}";
            ILogger logger = CreateLogger(
                spySink, config => config.Enrich.WithProperty(ContextProperties.Correlation.OperationId, operationId));

            var telemetryContext = new Dictionary<string, object>
            {
                ["Client"] = "https://localhost",
                ["ContentType"] = "application/json",
            };
            var statusCode = (int)HttpStatusCode.OK;
            HttpRequest request = CreateStubRequest(HttpMethod.Get, "https", "localhost", "/api/v1/health");
            TimeSpan duration = TimeSpan.FromSeconds(5);
            logger.LogRequest(request, statusCode, duration, telemetryContext);

            LogEvent logEvent = Assert.Single(spySink.CurrentLogEmits);
            Assert.NotNull(logEvent);

            var converter = ApplicationInsightsTelemetryConverter.Create();

            // Act
            IEnumerable<ITelemetry> telemetries = converter.Convert(logEvent, formatProvider: null);

            // Assert
            AssertDoesNotContainLogProperty(logEvent, RequestTracking.RequestMethod);
            AssertDoesNotContainLogProperty(logEvent, RequestTracking.RequestHost);
            AssertDoesNotContainLogProperty(logEvent, RequestTracking.RequestDuration);
            AssertDoesNotContainLogProperty(logEvent, RequestTracking.ResponseStatusCode);
            AssertDoesNotContainLogProperty(logEvent, RequestTracking.RequestTime);
            AssertDoesNotContainLogProperty(logEvent, RequestTracking.ResponseStatusCode);
            AssertDoesNotContainLogProperty(logEvent, ContextProperties.TelemetryContext);
            Assert.Collection(telemetries, telemetry =>
            {
                var requestTelemetry = Assert.IsType<RequestTelemetry>(telemetry);
                Assert.Equal("GET /api/v1/health", requestTelemetry.Name);
                Assert.Equal(duration, requestTelemetry.Duration);
                Assert.Equal(statusCode.ToString(), requestTelemetry.ResponseCode);
                Assert.True(requestTelemetry.Success);
                Assert.Equal(operationId, requestTelemetry.Id);
                Assert.Equal(new Uri("https://localhost/api/v1/health"), requestTelemetry.Url);
                AssertOperationContext(requestTelemetry, operationId);

                AssertContainsTelemetryProperty(requestTelemetry, "Client", "https://localhost");
                AssertContainsTelemetryProperty(requestTelemetry, "ContentType", "application/json");
            });
        }

        [Fact]
        public void LogDependency_WithDependency_CreatesDependencyTelemetry()
        {
            // Arrange
            var spySink = new InMemoryLogSink();
            string operationId = $"operation-id-{Guid.NewGuid()}";
            ILogger logger = CreateLogger(spySink, config => config.Enrich.WithProperty(ContextProperties.Correlation.OperationId, operationId));
            string dependencyType = $"dependency-{Guid.NewGuid()}";
            const int dependencyData = 10;
            var startTime = DateTimeOffset.UtcNow;
            var duration = TimeSpan.FromSeconds(5);
            var telemetryContext = new Dictionary<string, object>
            {
                ["CustomSetting"] = "Approved"
            };
            logger.LogDependency(dependencyType, dependencyData, isSuccessful: true, startTime: startTime, duration: duration, context: telemetryContext);
            LogEvent logEvent = Assert.Single(spySink.CurrentLogEmits);
            Assert.NotNull(logEvent);

            var converter = ApplicationInsightsTelemetryConverter.Create();

            // Act
            IEnumerable<ITelemetry> telemetries = converter.Convert(logEvent, formatProvider: null);

            // Assert
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.DependencyType);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.TargetName);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.DependencyName);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.IsSuccessful);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.DependencyData);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.ResultCode);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.StartTime);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.Duration);
            AssertDoesNotContainLogProperty(logEvent, ContextProperties.TelemetryContext);
            Assert.Collection(telemetries, telemetry =>
            {
                var dependencyTelemetry = Assert.IsType<DependencyTelemetry>(telemetry);
                Assert.Equal(dependencyType, dependencyTelemetry.Type);
                Assert.Equal(dependencyData.ToString(), dependencyTelemetry.Data);
                Assert.Equal(startTime, dependencyTelemetry.Timestamp);
                Assert.Equal(duration, dependencyTelemetry.Duration);
                Assert.True(dependencyTelemetry.Success);

                AssertContainsTelemetryProperty(dependencyTelemetry, "CustomSetting", "Approved");
            });
        }

        [Fact]
        public void LogServiceBusDependency_WithServiceBusDependency_CreatesDependencyTelemetry()
        {
            // Arrange
            var spySink = new InMemoryLogSink();
            string operationId = $"operation-id-{Guid.NewGuid()}";
            ILogger logger = CreateLogger(spySink, config => config.Enrich.WithProperty(ContextProperties.Correlation.OperationId, operationId));
            string entityName = $"entity-name-{Guid.NewGuid()}";
            const ServiceBusEntityType entityType = ServiceBusEntityType.Unknown;
            var startTime = DateTimeOffset.UtcNow;
            var duration = TimeSpan.FromSeconds(5);
            var telemetryContext = new Dictionary<string, object>
            {
                ["Namespace"] = "azure.servicebus.namespace"
            };
            logger.LogServiceBusDependency(entityName, entityType: entityType, isSuccessful: true, startTime: startTime, duration: duration, context: telemetryContext);
            LogEvent logEvent = Assert.Single(spySink.CurrentLogEmits);
            Assert.NotNull(logEvent);

            var converter = ApplicationInsightsTelemetryConverter.Create();

            // Act
            IEnumerable<ITelemetry> telemetries = converter.Convert(logEvent, formatProvider: null);

            // Assert
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.DependencyType);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.TargetName);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.DependencyName);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.IsSuccessful);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.DependencyData);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.ResultCode);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.StartTime);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.Duration);
            AssertDoesNotContainLogProperty(logEvent, ContextProperties.TelemetryContext);
            Assert.Collection(telemetries, telemetry =>
            {
                var dependencyTelemetry = Assert.IsType<DependencyTelemetry>(telemetry);
                Assert.Equal("Azure Service Bus", dependencyTelemetry.Type);
                Assert.Equal(entityName, dependencyTelemetry.Target);
                Assert.Equal(startTime, dependencyTelemetry.Timestamp);
                Assert.Equal(duration, dependencyTelemetry.Duration);
                Assert.True(dependencyTelemetry.Success);

                AssertContainsTelemetryProperty(dependencyTelemetry, "Namespace", "azure.servicebus.namespace");
                AssertContainsTelemetryProperty(dependencyTelemetry, "EntityType", entityType.ToString());
            });
        }

        [Fact]
        public void LogServiceBusQueueDependency_WithServiceBusDependency_CreatesDependencyTelemetry()
        {
            // Arrange
            var spySink = new InMemoryLogSink();
            string operationId = $"operation-id-{Guid.NewGuid()}";
            ILogger logger = CreateLogger(spySink, config => config.Enrich.WithProperty(ContextProperties.Correlation.OperationId, operationId));
            string queueName = $"queue-name-{Guid.NewGuid()}";
            var startTime = DateTimeOffset.UtcNow;
            var duration = TimeSpan.FromSeconds(5);
            var telemetryContext = new Dictionary<string, object>
            {
                ["Namespace"] = "azure.servicebus.namespace"
            };
            logger.LogServiceBusQueueDependency(queueName, isSuccessful: true, startTime: startTime, duration: duration, context: telemetryContext);
            LogEvent logEvent = Assert.Single(spySink.CurrentLogEmits);
            Assert.NotNull(logEvent);

            var converter = ApplicationInsightsTelemetryConverter.Create();

            // Act
            IEnumerable<ITelemetry> telemetries = converter.Convert(logEvent, formatProvider: null);

            // Assert
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.DependencyType);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.TargetName);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.DependencyName);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.IsSuccessful);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.DependencyData);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.ResultCode);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.StartTime);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.Duration);
            AssertDoesNotContainLogProperty(logEvent, ContextProperties.TelemetryContext);
            Assert.Collection(telemetries, telemetry =>
            {
                var dependencyTelemetry = Assert.IsType<DependencyTelemetry>(telemetry);
                Assert.Equal("Azure Service Bus", dependencyTelemetry.Type);
                Assert.Equal(queueName, dependencyTelemetry.Target);
                Assert.Equal(startTime, dependencyTelemetry.Timestamp);
                Assert.Equal(duration, dependencyTelemetry.Duration);
                Assert.True(dependencyTelemetry.Success);

                AssertContainsTelemetryProperty(dependencyTelemetry, "Namespace", "azure.servicebus.namespace");
                AssertContainsTelemetryProperty(dependencyTelemetry, "EntityType", ServiceBusEntityType.Queue.ToString());
            });
        }

        [Fact]
        public void LogServiceBusTopicDependency_WithServiceBusDependency_CreatesDependencyTelemetry()
        {
            // Arrange
            var spySink = new InMemoryLogSink();
            string operationId = $"operation-id-{Guid.NewGuid()}";
            ILogger logger = CreateLogger(spySink, config => config.Enrich.WithProperty(ContextProperties.Correlation.OperationId, operationId));
            string topicName = $"topic-name-{Guid.NewGuid()}";
            var startTime = DateTimeOffset.UtcNow;
            var duration = TimeSpan.FromSeconds(5);
            var telemetryContext = new Dictionary<string, object>
            {
                ["Namespace"] = "azure.servicebus.namespace"
            };
            logger.LogServiceBusTopicDependency(topicName, isSuccessful: true, startTime: startTime, duration: duration, context: telemetryContext);
            LogEvent logEvent = Assert.Single(spySink.CurrentLogEmits);
            Assert.NotNull(logEvent);

            var converter = ApplicationInsightsTelemetryConverter.Create();

            // Act
            IEnumerable<ITelemetry> telemetries = converter.Convert(logEvent, formatProvider: null);

            // Assert
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.DependencyType);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.TargetName);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.DependencyName);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.IsSuccessful);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.DependencyData);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.ResultCode);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.StartTime);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.Duration);
            AssertDoesNotContainLogProperty(logEvent, ContextProperties.TelemetryContext);
            Assert.Collection(telemetries, telemetry =>
            {
                var dependencyTelemetry = Assert.IsType<DependencyTelemetry>(telemetry);
                Assert.Equal("Azure Service Bus", dependencyTelemetry.Type);
                Assert.Equal(topicName, dependencyTelemetry.Target);
                Assert.Equal(startTime, dependencyTelemetry.Timestamp);
                Assert.Equal(duration, dependencyTelemetry.Duration);
                Assert.True(dependencyTelemetry.Success);

                AssertContainsTelemetryProperty(dependencyTelemetry, "Namespace", "azure.servicebus.namespace");
                AssertContainsTelemetryProperty(dependencyTelemetry, "EntityType", ServiceBusEntityType.Topic.ToString());
            });
        }

        [Fact]
        public void LogBlobStorageDependency_WithTableStorageDependency_CreatesDependencyTelemetry()
        {
            // Arrange
            var spySink = new InMemoryLogSink();
            string operationId = $"operation-id-{Guid.NewGuid()}";
            ILogger logger = CreateLogger(spySink, config => config.Enrich.WithProperty(ContextProperties.Correlation.OperationId, operationId));
            string containerName = _bogusGenerator.Commerce.ProductName();
            string accountName = _bogusGenerator.Finance.AccountName();
            var startTime = DateTimeOffset.UtcNow;
            var duration = TimeSpan.FromSeconds(5);
            var telemetryContext = new Dictionary<string, object>
            {
                ["Namespace"] = "azure.blobstorage.namespace"
            };
            logger.LogBlobStorageDependency(accountName, containerName, isSuccessful: true, startTime: startTime, duration: duration, context: telemetryContext);
            LogEvent logEvent = Assert.Single(spySink.CurrentLogEmits);
            Assert.NotNull(logEvent);

            var converter = ApplicationInsightsTelemetryConverter.Create();

            // Act
            IEnumerable<ITelemetry> telemetries = converter.Convert(logEvent, formatProvider: null);

            // Assert
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.DependencyType);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.TargetName);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.DependencyName);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.IsSuccessful);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.DependencyData);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.ResultCode);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.StartTime);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.Duration);
            AssertDoesNotContainLogProperty(logEvent, ContextProperties.TelemetryContext);
            Assert.Collection(telemetries, telemetry =>
            {
                var dependencyTelemetry = Assert.IsType<DependencyTelemetry>(telemetry);
                Assert.Equal("Azure blob", dependencyTelemetry.Type);
                Assert.Equal(containerName, dependencyTelemetry.Data);
                Assert.Equal(accountName, dependencyTelemetry.Target);
                Assert.Equal(startTime, dependencyTelemetry.Timestamp);
                Assert.Equal(duration, dependencyTelemetry.Duration);
                Assert.True(dependencyTelemetry.Success);

                AssertContainsTelemetryProperty(dependencyTelemetry, "Namespace", "azure.blobstorage.namespace");
            });
        }

        [Fact]
        public void LogTableStorageDependency_WithTableStorageDependency_CreatesDependencyTelemetry()
        {
            // Arrange
            var spySink = new InMemoryLogSink();
            string operationId = $"operation-id-{Guid.NewGuid()}";
            ILogger logger = CreateLogger(spySink, config => config.Enrich.WithProperty(ContextProperties.Correlation.OperationId, operationId));
            string tableName = _bogusGenerator.Commerce.ProductName();
            string accountName = _bogusGenerator.Finance.AccountName();
            var startTime = DateTimeOffset.UtcNow;
            var duration = TimeSpan.FromSeconds(5);
            var telemetryContext = new Dictionary<string, object>
            {
                ["Namespace"] = "azure.tablestorage.namespace"
            };
            logger.LogTableStorageDependency(accountName, tableName, isSuccessful: true, startTime: startTime, duration: duration, context: telemetryContext);
            LogEvent logEvent = Assert.Single(spySink.CurrentLogEmits);
            Assert.NotNull(logEvent);

            var converter = ApplicationInsightsTelemetryConverter.Create();

            // Act
            IEnumerable<ITelemetry> telemetries = converter.Convert(logEvent, formatProvider: null);

            // Assert
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.DependencyType);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.TargetName);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.DependencyName);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.IsSuccessful);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.DependencyData);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.ResultCode);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.StartTime);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.Duration);
            AssertDoesNotContainLogProperty(logEvent, ContextProperties.TelemetryContext);
            Assert.Collection(telemetries, telemetry =>
            {
                var dependencyTelemetry = Assert.IsType<DependencyTelemetry>(telemetry);
                Assert.Equal("Azure table", dependencyTelemetry.Type);
                Assert.Equal(tableName, dependencyTelemetry.Data);
                Assert.Equal(accountName, dependencyTelemetry.Target);
                Assert.Equal(startTime, dependencyTelemetry.Timestamp);
                Assert.Equal(duration, dependencyTelemetry.Duration);
                Assert.True(dependencyTelemetry.Success);

                AssertContainsTelemetryProperty(dependencyTelemetry, "Namespace", "azure.tablestorage.namespace");
            });
        }

        [Fact]
        public void LogEventHubsDependency_WithEventHubsDependency_CreatesDependencyTelemetry()
        {
            // Arrange
            var spySink = new InMemoryLogSink();
            string operationId = $"operation-id-{Guid.NewGuid()}";
            ILogger logger = CreateLogger(spySink, config => config.Enrich.WithProperty(ContextProperties.Correlation.OperationId, operationId));
            string eventHubName = _bogusGenerator.Commerce.ProductName();
            string namespaceName = _bogusGenerator.Finance.AccountName();
            var startTime = DateTimeOffset.UtcNow;
            var duration = TimeSpan.FromSeconds(5);
            var telemetryContext = new Dictionary<string, object>
            {
                ["Host"] = "orders.servicebus.windows.net"
            };
            logger.LogEventHubsDependency(namespaceName, eventHubName, isSuccessful: true, startTime: startTime, duration: duration, context: telemetryContext);
            LogEvent logEvent = Assert.Single(spySink.CurrentLogEmits);
            Assert.NotNull(logEvent);

            var converter = ApplicationInsightsTelemetryConverter.Create();

            // Act
            IEnumerable<ITelemetry> telemetries = converter.Convert(logEvent, formatProvider: null);

            // Assert
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.DependencyType);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.TargetName);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.DependencyName);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.IsSuccessful);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.DependencyData);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.ResultCode);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.StartTime);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.Duration);
            AssertDoesNotContainLogProperty(logEvent, ContextProperties.TelemetryContext);
            Assert.Collection(telemetries, telemetry =>
            {
                var dependencyTelemetry = Assert.IsType<DependencyTelemetry>(telemetry);
                Assert.Equal("Azure Event Hubs", dependencyTelemetry.Type);
                Assert.Equal(eventHubName, dependencyTelemetry.Target);
                Assert.Equal(namespaceName, dependencyTelemetry.Data);
                Assert.Equal(startTime, dependencyTelemetry.Timestamp);
                Assert.Equal(duration, dependencyTelemetry.Duration);
                Assert.True(dependencyTelemetry.Success);

                AssertContainsTelemetryProperty(dependencyTelemetry, "Host", "orders.servicebus.windows.net");
            });
        }

        [Fact]
        public void LogIoTHubDependency_WithTableStorageDependency_CreatesDependencyTelemetry()
        {
            // Arrange
            var spySink = new InMemoryLogSink();
            string operationId = $"operation-id-{Guid.NewGuid()}";
            ILogger logger = CreateLogger(spySink, config => config.Enrich.WithProperty(ContextProperties.Correlation.OperationId, operationId));
            string iotHubName = _bogusGenerator.Commerce.ProductName();
            var startTime = DateTimeOffset.UtcNow;
            var duration = TimeSpan.FromSeconds(5);
            var telemetryContext = new Dictionary<string, object>
            {
                ["DeviceName"] = "Sensor #102"
            };
            logger.LogIotHubDependency(iotHubName: iotHubName, isSuccessful: true, startTime: startTime, duration: duration, context: telemetryContext);
            LogEvent logEvent = Assert.Single(spySink.CurrentLogEmits);
            Assert.NotNull(logEvent);

            var converter = ApplicationInsightsTelemetryConverter.Create();

            // Act
            IEnumerable<ITelemetry> telemetries = converter.Convert(logEvent, formatProvider: null);

            // Assert
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.DependencyType);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.TargetName);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.DependencyName);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.IsSuccessful);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.DependencyData);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.ResultCode);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.StartTime);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.Duration);
            AssertDoesNotContainLogProperty(logEvent, ContextProperties.TelemetryContext);
            Assert.Collection(telemetries, telemetry =>
            {
                var dependencyTelemetry = Assert.IsType<DependencyTelemetry>(telemetry);
                Assert.Equal("Azure IoT Hub", dependencyTelemetry.Type);
                Assert.Equal(iotHubName, dependencyTelemetry.Target);
                Assert.Equal(startTime, dependencyTelemetry.Timestamp);
                Assert.Equal(duration, dependencyTelemetry.Duration);
                Assert.True(dependencyTelemetry.Success);

                AssertContainsTelemetryProperty(dependencyTelemetry, "DeviceName", "Sensor #102");
            });
        }

        [Fact]
        public void LogCosmosSqlDependency_WithTableStorageDependency_CreatesDependencyTelemetry()
        {
            // Arrange
            var spySink = new InMemoryLogSink();
            string operationId = $"operation-id-{Guid.NewGuid()}";
            ILogger logger = CreateLogger(spySink, config => config.Enrich.WithProperty(ContextProperties.Correlation.OperationId, operationId));
            string container = _bogusGenerator.Commerce.ProductName();
            string database = _bogusGenerator.Commerce.ProductName();
            string accountName = _bogusGenerator.Finance.AccountName();
            var startTime = DateTimeOffset.UtcNow;
            var duration = TimeSpan.FromSeconds(5);
            var telemetryContext = new Dictionary<string, object>
            {
                ["Namespace"] = "azure.cosmos.namespace"
            };
            logger.LogCosmosSqlDependency(accountName, database, container, isSuccessful: true, startTime: startTime, duration: duration, context: telemetryContext);
            LogEvent logEvent = Assert.Single(spySink.CurrentLogEmits);
            Assert.NotNull(logEvent);

            var converter = ApplicationInsightsTelemetryConverter.Create();

            // Act
            IEnumerable<ITelemetry> telemetries = converter.Convert(logEvent, formatProvider: null);

            // Assert
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.DependencyType);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.TargetName);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.DependencyName);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.IsSuccessful);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.DependencyData);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.ResultCode);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.StartTime);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.Duration);
            AssertDoesNotContainLogProperty(logEvent, ContextProperties.TelemetryContext);
            Assert.Collection(telemetries, telemetry =>
            {
                var dependencyTelemetry = Assert.IsType<DependencyTelemetry>(telemetry);
                Assert.Equal("Azure DocumentDB", dependencyTelemetry.Type);
                Assert.Equal($"{database}/{container}", dependencyTelemetry.Data);
                Assert.Equal(accountName, dependencyTelemetry.Target);
                Assert.Equal(startTime, dependencyTelemetry.Timestamp);
                Assert.Equal(duration, dependencyTelemetry.Duration);
                Assert.True(dependencyTelemetry.Success);

                AssertContainsTelemetryProperty(dependencyTelemetry, "Namespace", "azure.cosmos.namespace");
            });
        }

        [Fact]
        public void LogHttpDependency_WithHttpDependency_CreatesHttpDependencyTelemetry()
        {
            // Arrange
            var spySink = new InMemoryLogSink();
            string operationId = $"operation-id-{Guid.NewGuid()}";
            ILogger logger = CreateLogger(spySink, config => config.Enrich.WithProperty(ContextProperties.Correlation.OperationId, operationId));

            var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, "https://localhost/api/v1/health");
            var startTime = DateTimeOffset.UtcNow;
            var duration = TimeSpan.FromSeconds(5);
            var telemetryContext = new Dictionary<string, object>
            {
                ["Port"] = "4000"
            };
            logger.LogHttpDependency(request, HttpStatusCode.OK, startTime, duration, telemetryContext);
            LogEvent logEvent = Assert.Single(spySink.CurrentLogEmits);
            Assert.NotNull(logEvent);


            var converter = ApplicationInsightsTelemetryConverter.Create();

            // Act
            IEnumerable<ITelemetry> telemetries = converter.Convert(logEvent, formatProvider: null);

            // Assert
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.TargetName);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.DependencyName);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.IsSuccessful);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.DependencyData);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.ResultCode);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.StartTime);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.Duration);
            AssertDoesNotContainLogProperty(logEvent, ContextProperties.TelemetryContext);
            Assert.Collection(telemetries, telemetry =>
            {
                var dependencyTelemetry = Assert.IsType<DependencyTelemetry>(telemetry);
                Assert.Equal(DependencyType.Http.ToString(), dependencyTelemetry.Type);
                Assert.Equal("localhost", dependencyTelemetry.Target);
                Assert.Equal("GET /api/v1/health", dependencyTelemetry.Name);
                Assert.Null(dependencyTelemetry.Data);
                Assert.Equal(startTime, dependencyTelemetry.Timestamp);
                Assert.Equal(duration, dependencyTelemetry.Duration);
                Assert.Equal("200", dependencyTelemetry.ResultCode);
                Assert.True(dependencyTelemetry.Success);
                AssertOperationContext(dependencyTelemetry, operationId);

                AssertContainsTelemetryProperty(dependencyTelemetry, "Port", "4000");
            });
        }

        [Fact]
        public void LogSqlDependency_WithSqlDependency_CreatesSqlDependencyTelemetry()
        {
            // Arrange
            var spySink = new InMemoryLogSink();
            string operationId = $"operation-id-{Guid.NewGuid()}";
            ILogger logger = CreateLogger(spySink, config => config.Enrich.WithProperty(ContextProperties.Correlation.OperationId, operationId));
            var startTime = DateTimeOffset.UtcNow;
            var duration = TimeSpan.FromSeconds(5);
            var telemetryContext = new Dictionary<string, object>
            {
                ["Statement"] = "Query"
            };
            logger.LogSqlDependency("Server", "Database", "Users", "GET", isSuccessful: true, startTime: startTime, duration: duration, telemetryContext);
            LogEvent logEvent = Assert.Single(spySink.CurrentLogEmits);
            Assert.NotNull(logEvent);

            var converter = ApplicationInsightsTelemetryConverter.Create();

            // Act
            IEnumerable<ITelemetry> telemetries = converter.Convert(logEvent, formatProvider: null);

            // Assert
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.TargetName);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.DependencyName);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.IsSuccessful);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.DependencyData);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.ResultCode);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.StartTime);
            AssertDoesNotContainLogProperty(logEvent, DependencyTracking.Duration);
            AssertDoesNotContainLogProperty(logEvent, ContextProperties.TelemetryContext);
            Assert.Collection(telemetries, telemetry =>
            {
                var dependencyTelemetry = Assert.IsType<DependencyTelemetry>(telemetry);
                Assert.Equal(DependencyType.Sql.ToString(), dependencyTelemetry.Type);
                Assert.Equal("Server", dependencyTelemetry.Target);
                Assert.Equal("Database/Users", dependencyTelemetry.Name);
                Assert.Equal("GET", dependencyTelemetry.Data);
                Assert.Equal(startTime, dependencyTelemetry.Timestamp);
                Assert.Equal(duration, dependencyTelemetry.Duration);
                Assert.Null(dependencyTelemetry.ResultCode);
                Assert.True(dependencyTelemetry.Success);
                AssertOperationContext(dependencyTelemetry, operationId);

                AssertContainsTelemetryProperty(dependencyTelemetry, "Statement", "Query");
            });
        }

        [Fact]
        public void LogEvent_WithEvent_CreatesEventTelemetry()
        {
            // Arrange
            const string eventName = "Order Invoiced";
            var spySink = new InMemoryLogSink();
            string operationId = $"operation-id-{Guid.NewGuid()}";
            ILogger logger = CreateLogger(spySink, config => config.Enrich.WithProperty(ContextProperties.Correlation.OperationId, operationId));

            var telemetryContext = new Dictionary<string, object>
            {
                ["OrderId"] = "ABC",
                ["Vendor"] = "Contoso"
            };

            logger.LogEvent(eventName, telemetryContext);
            LogEvent logEvent = Assert.Single(spySink.CurrentLogEmits);
            Assert.NotNull(logEvent);

            var converter = ApplicationInsightsTelemetryConverter.Create();

            // Act
            IEnumerable<ITelemetry> telemetries = converter.Convert(logEvent, formatProvider: null);

            // Assert
            AssertDoesNotContainLogProperty(logEvent, EventTracking.EventName);
            AssertDoesNotContainLogProperty(logEvent, ContextProperties.TelemetryContext);
            Assert.Collection(telemetries, telemetry =>
            {
                var eventTelemetry = Assert.IsType<EventTelemetry>(telemetry);
                Assert.Equal(eventName, eventTelemetry.Name);
                AssertOperationContext(eventTelemetry, operationId);
                AssertContainsTelemetryProperty(eventTelemetry, "OrderId", "ABC");
                AssertContainsTelemetryProperty(eventTelemetry, "Vendor", "Contoso");
            });
        }

        [Fact]
        public void LogException_WithException_CreatesExceptionTelemetry()
        {
            // Arrange
            string platform = $"platform-id-{Guid.NewGuid()}";
            var exception = new PlatformNotSupportedException(platform);
            var spySink = new InMemoryLogSink();
            string operationId = $"operation-id-{Guid.NewGuid()}";
            ILogger logger = CreateLogger(spySink, config => config.Enrich.WithProperty(ContextProperties.Correlation.OperationId, operationId));

            logger.LogCritical(exception, exception.Message);
            LogEvent logEvent = Assert.Single(spySink.CurrentLogEmits);
            Assert.NotNull(logEvent);

            var converter = ApplicationInsightsTelemetryConverter.Create();

            // Act
            IEnumerable<ITelemetry> telemetries = converter.Convert(logEvent, formatProvider: null);

            // Assert
            Assert.Collection(telemetries, telemetry =>
            {
                var exceptionTelemetryType = Assert.IsType<ExceptionTelemetry>(telemetry);
                Assert.NotNull(exceptionTelemetryType);
                Assert.NotNull(exceptionTelemetryType.Exception);
                Assert.Equal(exception.Message, exceptionTelemetryType.Exception.Message);
                AssertOperationContext(exceptionTelemetryType, operationId);
            });
        }

        [Fact]
        public void LogMetric_WithMetric_CreatesMetricTelemetry()
        {
            // Arrange
            const string metricName = "Request stream";
            const double metricValue = 0.13;
            var timestamp = DateTimeOffset.UtcNow;
            var spySink = new InMemoryLogSink();
            string operationId = $"operation-id-{Guid.NewGuid()}";
            ILogger logger = CreateLogger(spySink, config => config.Enrich.WithProperty(ContextProperties.Correlation.OperationId, operationId));

            var telemetryContext = new Dictionary<string, object>
            {
                ["Capacity"] = "0.45"
            };
            logger.LogMetric(metricName, metricValue, timestamp, telemetryContext);
            LogEvent logEvent = Assert.Single(spySink.CurrentLogEmits);
            Assert.NotNull(logEvent);

            var converter = ApplicationInsightsTelemetryConverter.Create();

            // Act
            IEnumerable<ITelemetry> telemetries = converter.Convert(logEvent, formatProvider: null);

            // Assert
            AssertDoesNotContainLogProperty(logEvent, MetricTracking.MetricName);
            AssertDoesNotContainLogProperty(logEvent, MetricTracking.MetricValue);
            AssertDoesNotContainLogProperty(logEvent, MetricTracking.Timestamp);
            AssertDoesNotContainLogProperty(logEvent, ContextProperties.TelemetryContext);
            Assert.Collection(telemetries, telemetry =>
            {
                var metricTelemetry = Assert.IsType<MetricTelemetry>(telemetry);
                Assert.Equal(metricName, metricTelemetry.Name);
                Assert.Equal(metricValue, metricTelemetry.Sum);
                Assert.Equal(timestamp, metricTelemetry.Timestamp);
                AssertOperationContext(metricTelemetry, operationId);

                AssertContainsTelemetryProperty(metricTelemetry, "Capacity", "0.45");
            });
        }

        [Fact]
        public void LogInformationWithPodName_Without_CreatesTraceTelemetryWithPodNameAsRoleInstance()
        {
            // Arrange
            var spySink = new InMemoryLogSink();
            string operationId = $"operation-id-{Guid.NewGuid()}";
            ILogger logger = CreateLogger(spySink, config =>
            {
                return config.Enrich.WithProperty(ContextProperties.Correlation.OperationId, operationId)
                             .Enrich.WithProperty(General.ComponentName, "component")
                             .Enrich.WithProperty(Kubernetes.PodName, "pod");
            });

            logger.LogInformation("trace message");
            LogEvent logEvent = Assert.Single(spySink.CurrentLogEmits);
            Assert.NotNull(logEvent);

            var converter = ApplicationInsightsTelemetryConverter.Create();

            // Act
            IEnumerable<ITelemetry> telemetries = converter.Convert(logEvent, formatProvider: null);

            // Assert
            Assert.Collection(telemetries, telemetry =>
            {
                var traceTelemetry = Assert.IsType<TraceTelemetry>(telemetry);
                Assert.Equal("trace message", traceTelemetry.Message);
                Assert.Equal(logEvent.Timestamp, traceTelemetry.Timestamp);
                Assert.Equal(SeverityLevel.Information, traceTelemetry.SeverityLevel);
                Assert.Equal("component", traceTelemetry.Context.Cloud.RoleName);
                Assert.Equal("pod", traceTelemetry.Context.Cloud.RoleInstance);
                AssertOperationContext(traceTelemetry, operationId);
            });
        }

        [Fact]
        public void LogInformationWithMachineName_Without_CreatesTraceTelemetryWithPodNameAsRoleInstance()
        {
            // Arrange
            var spySink = new InMemoryLogSink();
            string operationId = $"operation-id-{Guid.NewGuid()}";
            ILogger logger = CreateLogger(spySink, config =>
            {
                return config.Enrich.WithProperty(ContextProperties.Correlation.OperationId, operationId)
                             .Enrich.WithProperty(General.ComponentName, "component")
                             .Enrich.WithProperty(General.MachineName, "machine");
            });

            logger.LogInformation("trace message");
            LogEvent logEvent = Assert.Single(spySink.CurrentLogEmits);
            Assert.NotNull(logEvent);

            var converter = ApplicationInsightsTelemetryConverter.Create();

            // Act
            IEnumerable<ITelemetry> telemetries = converter.Convert(logEvent, formatProvider: null);

            // Assert
            Assert.Collection(telemetries, telemetry =>
            {
                var traceTelemetry = Assert.IsType<TraceTelemetry>(telemetry);
                Assert.Equal("trace message", traceTelemetry.Message);
                Assert.Equal(logEvent.Timestamp, traceTelemetry.Timestamp);
                Assert.Equal(SeverityLevel.Information, traceTelemetry.SeverityLevel);
                Assert.Equal("component", traceTelemetry.Context.Cloud.RoleName);
                Assert.Equal("machine", traceTelemetry.Context.Cloud.RoleInstance);
                AssertOperationContext(traceTelemetry, operationId);
            });
        }

        private static void AssertOperationContext<TTelemetry>(TTelemetry telemetry, string operationId) where TTelemetry : ITelemetry
        {
            Assert.NotNull(telemetry.Context);
            Assert.NotNull(telemetry.Context.Operation);
            Assert.Equal(operationId, telemetry.Context.Operation.Id);
        }

        private static ILogger CreateLogger(ILogEventSink sink, Func<LoggerConfiguration, LoggerConfiguration> configureLoggerConfiguration = null)
        {
            LoggerConfiguration config = new LoggerConfiguration().WriteTo.Sink(sink);
            config = configureLoggerConfiguration?.Invoke(config) ?? config;
            Logger logger = config.CreateLogger();

            var factory = new SerilogLoggerFactory(logger);
            return factory.CreateLogger<ApplicationInsightsTelemetryConverterTests>();
        }

        private static HttpRequest CreateStubRequest(HttpMethod httpMethod, string requestScheme, string host, string path)
        {
            var request = new Mock<HttpRequest>();
            request.Setup(req => req.Method).Returns(httpMethod.ToString().ToUpper);
            request.Setup(req => req.Scheme).Returns(requestScheme);
            request.Setup(req => req.Host).Returns(new HostString(host));
            request.Setup(req => req.Path).Returns(path);

            return request.Object;
        }

        private static HttpResponse CreateStubResponse(HttpStatusCode statusCode)
        {
            var response = new Mock<HttpResponse>();
            response.Setup(res => res.StatusCode).Returns((int)statusCode);

            return response.Object;
        }

        private static void AssertDoesNotContainLogProperty(LogEvent logEvent, string name)
        {
            Assert.False(logEvent.Properties.ContainsKey(name), $"Log event should not contain log property with name '{name}'");
        }

        private static void AssertContainsTelemetryProperty(ISupportProperties telemetry, string key, string value)
        {
            Assert.Contains(telemetry.Properties, prop => prop.Equals(new KeyValuePair<string, string>(key, value)));
        }
    }
}
