using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace PairAdmin.Security.Audit;

/// <summary>
/// Interface for audit logging
/// </summary>
public interface IAuditLogger
{
    /// <summary>
    /// Logs a command execution
    /// </summary>
    void LogCommand(
        string command,
        string result,
        TimeSpan duration,
        bool success,
        string? user = null);

    /// <summary>
    /// Logs a security event
    /// </summary>
    void LogSecurity(
        SecurityEventType eventType,
        string details,
        string? user = null);

    /// <summary>
    /// Logs a validation event
    /// </summary>
    void LogValidation(
        string command,
        string reason,
        bool blocked);

    /// <summary>
    /// Logs sensitive data detection
    /// </summary>
    void LogSensitiveData(
        string patternName,
        string context,
        bool filtered);

    /// <summary>
    /// Logs an application event
    /// </summary>
    void LogApplicationEvent(
        AuditEventType eventType,
        string details);

    /// <summary>
    /// Logs an error
    /// </summary>
    void LogError(
        string context,
        string errorMessage,
        string? stackTrace = null);

    /// <summary>
    /// Queries audit entries
    /// </summary>
    IEnumerable<AuditEntry> Query(QueryParameters parameters);

    /// <summary>
    /// Gets entry count
    /// </summary>
    int GetEntryCount();

    /// <summary>
    /// Exports audit log
    /// </summary>
    Task ExportAsync(string path, ExportFormat format, QueryParameters? parameters = null);
}
