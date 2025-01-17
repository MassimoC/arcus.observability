﻿namespace Arcus.Observability.Telemetry.Core
{
    public static class MessagePrefixes
    {
        public const string DependencyViaHttp = "HTTP Dependency";
        public const string DependencyViaSql = "SQL Dependency";
        public const string Dependency = "Dependency";
        public const string Event = "Events";
        public const string Metric = "Metric";
        public const string RequestViaHttp = "HTTP Request";
    }
}
