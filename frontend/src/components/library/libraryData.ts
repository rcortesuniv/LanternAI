export interface QueryTemplate {
  question: string;
  description: string;
}

export interface QueryCategory {
  name: string;
  icon: string;
  queries: QueryTemplate[];
}

export const QUERY_LIBRARY: QueryCategory[] = [
  {
    name: "Identity & Access",
    icon: "🔐",
    queries: [
      { question: "How many failed signins in the last 24 hours?", description: "Count of authentication failures" },
      { question: "Show me failed signins by location", description: "Failed attempts grouped by country" },
      { question: "Which users have the most failed signin attempts?", description: "Top users by failure count" },
      { question: "Show me signins from external locations", description: "Signins from non-corporate regions" },
      { question: "What apps are being targeted by failed signins?", description: "Failed attempts by application" },
      { question: "Show me active user sessions longer than 60 minutes", description: "Long-running sessions" },
      { question: "Which sessions were terminated abnormally?", description: "Sessions that didn't end cleanly" },
      { question: "Show me denied audit actions in the last 24 hours", description: "Access denials on regulated resources" },
      { question: "What actions were taken on regulated resources?", description: "Audit trail by resource" },
    ],
  },
  {
    name: "Security Events",
    icon: "🛡️",
    queries: [
      { question: "Show me critical security events in the last 24 hours", description: "High-severity event log entries" },
      { question: "Security events by computer", description: "Events grouped by host" },
      { question: "Show me account lockout events", description: "Lockout activity across servers" },
      { question: "Which computers have the most security events?", description: "Top hosts by event count" },
      { question: "Show me process creation events", description: "New process activity" },
      { question: "Show me blocked network connections", description: "Denied connections between services" },
      { question: "Network connections by destination", description: "Connection targets grouped by host" },
      { question: "Show me suspicious network connections on non-standard ports", description: "Connections on unusual ports" },
      { question: "Show me container error logs", description: "Container-level error messages" },
      { question: "Container warnings by service", description: "Warning-level logs grouped by service" },
    ],
  },
  {
    name: "Application Performance",
    icon: "⚡",
    queries: [
      { question: "Average request duration by endpoint", description: "Response time per route" },
      { question: "Show me the slowest API requests", description: "Highest-latency requests" },
      { question: "How many requests failed in the last hour?", description: "Failed HTTP requests count" },
      { question: "Request error rate by endpoint", description: "Failure percentage per route" },
      { question: "Show me API errors by error type", description: "Errors grouped by category" },
      { question: "Which API routes have the most errors?", description: "Top routes by error count" },
      { question: "Show me slow database queries", description: "High-latency database operations" },
      { question: "Average database query duration by database", description: "Query time per database" },
      { question: "Show me failed database queries", description: "Unsuccessful database operations" },
      { question: "Average dependency call duration by target", description: "Downstream service latency" },
      { question: "Show me failed dependency calls", description: "Unsuccessful downstream requests" },
    ],
  },
  {
    name: "Infrastructure & Ops",
    icon: "🏗️",
    queries: [
      { question: "Show me unhealthy services", description: "Services failing health checks" },
      { question: "Average service health latency by region", description: "Check latency per region" },
      { question: "Show me dead-lettered queue messages", description: "Messages sent to dead letter queue" },
      { question: "Queue processing time by queue", description: "Processing duration per queue" },
      { question: "Show me failed deployments", description: "Unsuccessful release attempts" },
      { question: "Deployments by environment", description: "Releases grouped by environment" },
      { question: "Show me failed jobs in the last 24 hours", description: "Unsuccessful background jobs" },
      { question: "Average job duration by job name", description: "Processing time per job type" },
      { question: "Job success rate", description: "Overall job completion percentage" },
    ],
  },
  {
    name: "Compliance & Governance",
    icon: "📋",
    queries: [
      { question: "Show me failed data exports", description: "Unsuccessful compliance export jobs" },
      { question: "Data exports by requested user", description: "Exports grouped by requester" },
      { question: "Show me data exports with more than 10000 rows", description: "Large export volume" },
      { question: "Which feature flags were recently enabled?", description: "Recently activated flags" },
      { question: "Feature flag evaluations by user", description: "Flag access per user" },
      { question: "Show me feature flag variants in use", description: "Active experiment variants" },
    ],
  },
  {
    name: "Cross-Source",
    icon: "🔍",
    queries: [
      { question: "Total duration across app requests, database queries, and API dependencies", description: "Combined latency across all layers" },
      { question: "Show me failed operations across app requests, database queries, and API dependencies", description: "Combined failure view" },
      { question: "Total errors across API errors and container logs", description: "Combined error surface" },
    ],
  },
];
