// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

internal sealed partial class DefaultHealthCheckService : HealthCheckService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<HealthCheckServiceOptions> _healthCheckServiceOptions;
    private readonly IOptions<HealthCheckPublisherOptions> _healthCheckPublisherOptions;
    private readonly ILogger<DefaultHealthCheckService> _logger;
    private readonly ConcurrentQueue<KeyValuePair<string, HealthReportEntry>> _healthReportEntriesQueue;
    private readonly IReadOnlyDictionary<TimeSpan, List<HealthCheckRegistration>> _periodHealthChecksMap;
    private readonly IDictionary<TimeSpan, Timer> _healthCheckRegistrationTimers;
    private CancellationToken _isStopping;

    public DefaultHealthCheckService(
        IServiceScopeFactory scopeFactory,
        IOptions<HealthCheckServiceOptions> healthCheckServiceOptions,
        IOptions<HealthCheckPublisherOptions> healthCheckPublisherOptions,
        ILogger<DefaultHealthCheckService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _healthCheckServiceOptions = healthCheckServiceOptions ?? throw new ArgumentNullException(nameof(healthCheckServiceOptions));
        _healthCheckPublisherOptions = healthCheckPublisherOptions ?? throw new ArgumentNullException(nameof(healthCheckPublisherOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // We're specifically going out of our way to do this at startup time. We want to make sure you
        // get any kind of health-check related error as early as possible. Waiting until someone
        // actually tries to **run** health checks would be real baaaaad.
        ValidateRegistrations(_healthCheckServiceOptions.Value.Registrations);

        _healthReportEntriesQueue = new ConcurrentQueue<KeyValuePair<string, HealthReportEntry>>();

        // Group healthcheck registrations by period, to build a Dictionary<TimeSpan, List<HealthCheckRegistration>>
        _periodHealthChecksMap = (from r in _healthCheckServiceOptions.Value.Registrations
                                  where ByPredicate(r, _healthCheckPublisherOptions.Value.Predicate) // Apply publisher predicate
                                  where r.Period != Timeout.InfiniteTimeSpan // Skip HCs with no individual period
                                  group r by r.Period).ToDictionary(g => g.Key, g => g.ToList());

        // Aggregate Timers for HealthCheckRegistration having the same period
        _healthCheckRegistrationTimers = CreateTimers(_periodHealthChecksMap);
    }

    // Internal for unit testing
    internal IDictionary<TimeSpan, Timer> CreateTimers(IReadOnlyDictionary<TimeSpan, List<HealthCheckRegistration>> periodHealthChecksMap, CancellationToken cancellationToken = default)
    {
        return periodHealthChecksMap.Select(m => CreateTimer(m.Key, m.Value, cancellationToken)).ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    internal KeyValuePair<TimeSpan, Timer> CreateTimer(TimeSpan period, List<HealthCheckRegistration> registrations, CancellationToken cancellationToken = default)
    {
        return new KeyValuePair<TimeSpan, Timer>(
            period,
            NonCapturingTimer.Create(
            async (state) =>
            {
                var entries = await RunChecksAsync(registrations, cancellationToken).ConfigureAwait(false);
                foreach (var entry in entries)
                {
                    _healthReportEntriesQueue.Enqueue(entry);
                }
            },
            null,
            dueTime: _healthCheckPublisherOptions.Value.Delay,
            period: period));
    }

    internal void StopTimers()
    {
        foreach (var timerPeriod in _healthCheckRegistrationTimers.Keys)
        {
            StopTimer(timerPeriod);
        }
    }

    internal void StopTimer(TimeSpan period)
    {
        _healthCheckRegistrationTimers[period].Dispose();
    }

    internal CancellationToken IsStopping
    {
        get { return _isStopping; }
        set
        {
            if (value != default)
            {
                _isStopping = value;
            }
        }
    }

    public override async Task<HealthReport> CheckHealthAsync(Func<HealthCheckRegistration, bool>? predicate, CancellationToken cancellationToken = default)
    {
        // Save the cancellation token, so to propagate cancellation to the individual periodicity HCs
        IsStopping = cancellationToken;

        // TODO: if we decide for the current design, where HCs with default periodicity are initiated during CheckHealthAsync,
        // individual-periodicity HCs cannot use the predicate passed in this method (those are initiated in the cctor of DefaultHealthCheckService)

        var totalTime = ValueStopwatch.StartNew();
        Log.HealthCheckProcessingBegin(_logger);

        // Filter registrations by default publisher period and predicate
        var registrationsDefaultPeriod = _healthCheckServiceOptions.Value.Registrations.Where(r => r.Period == Timeout.InfiniteTimeSpan && ByPredicate(r, predicate)).ToList();

        // Run healthchecks with default/publisher period
        List<KeyValuePair<string, HealthReportEntry>>? healthReportEntriesValues = await RunChecksAsync(registrationsDefaultPeriod, cancellationToken).ConfigureAwait(false);

        // Collect health report entries from previously run periodic healthchecks within the default period
        while (_healthReportEntriesQueue.TryDequeue(out var entry))
        {
            healthReportEntriesValues.Add(entry);
        }

        var totalElapsedTime = totalTime.GetElapsedTime();
        var report = new HealthReport(new HealthReportEntryDictionary(healthReportEntriesValues, StringComparer.OrdinalIgnoreCase), totalElapsedTime);
        Log.HealthCheckProcessingEnd(_logger, report.Status, totalElapsedTime);

        return report;
    }

    private async Task<List<KeyValuePair<string, HealthReportEntry>>> RunChecksAsync(ICollection<HealthCheckRegistration> registrations, CancellationToken cancellationToken = default)
    {
        var tasks = new Task<HealthReportEntry>[registrations.Count];
        var index = 0;

        foreach (var registration in registrations)
        {
            tasks[index++] = Task.Run(() => RunCheckAsync(registration, cancellationToken), cancellationToken);
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);

        var entries = new List<KeyValuePair<string, HealthReportEntry>>();

        index = 0;
        foreach (var registration in registrations)
        {
            var healthReportEntry = tasks[index++].Result;
            entries.Add(new KeyValuePair<string, HealthReportEntry>(registration.Name, healthReportEntry));
        }

        return entries;
    }

    private async Task<HealthReportEntry> RunCheckAsync(HealthCheckRegistration registration, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var scope = _scopeFactory.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            var healthCheck = registration.Factory(scope.ServiceProvider);

            // If the health check does things like make Database queries using EF or backend HTTP calls,
            // it may be valuable to know that logs it generates are part of a health check. So we start a scope.
            using (_logger.BeginScope(new HealthCheckLogScope(registration.Name)))
            {
                var stopwatch = ValueStopwatch.StartNew();
                var context = new HealthCheckContext { Registration = registration };

                Log.HealthCheckBegin(_logger, registration.Name);

                HealthReportEntry entry;
                CancellationTokenSource? timeoutCancellationTokenSource = null;
                try
                {
                    HealthCheckResult result;

                    var checkCancellationToken = cancellationToken;
                    if (registration.Timeout > TimeSpan.Zero)
                    {
                        timeoutCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        timeoutCancellationTokenSource.CancelAfter(registration.Timeout);
                        checkCancellationToken = timeoutCancellationTokenSource.Token;
                    }

                    result = await healthCheck.CheckHealthAsync(context, checkCancellationToken).ConfigureAwait(false);

                    var duration = stopwatch.GetElapsedTime();

                    entry = new HealthReportEntry(
                        status: result.Status,
                        description: result.Description,
                        duration: duration,
                        exception: result.Exception,
                        data: result.Data,
                        tags: registration.Tags);

                    Log.HealthCheckEnd(_logger, registration, entry, duration);
                    Log.HealthCheckData(_logger, registration, entry);
                }
                catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    var duration = stopwatch.GetElapsedTime();
                    entry = new HealthReportEntry(
                        status: registration.FailureStatus,
                        description: "A timeout occurred while running check.",
                        duration: duration,
                        exception: ex,
                        data: null,
                        tags: registration.Tags);

                    Log.HealthCheckError(_logger, registration, ex, duration);
                }

                // Allow cancellation to propagate if it's not a timeout.
                catch (Exception ex) when (ex as OperationCanceledException == null)
                {
                    var duration = stopwatch.GetElapsedTime();
                    entry = new HealthReportEntry(
                        status: registration.FailureStatus,
                        description: ex.Message,
                        duration: duration,
                        exception: ex,
                        data: null,
                        tags: registration.Tags);

                    Log.HealthCheckError(_logger, registration, ex, duration);
                }

                finally
                {
                    timeoutCancellationTokenSource?.Dispose();
                }

                return entry;
            }
        }
    }

    private static void ValidateRegistrations(IEnumerable<HealthCheckRegistration> registrations)
    {
        // Scan the list for duplicate names to provide a better error if there are duplicates.

        StringBuilder? builder = null;
        var distinctRegistrations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var registration in registrations)
        {
            if (!distinctRegistrations.Add(registration.Name))
            {
                builder ??= new StringBuilder("Duplicate health checks were registered with the name(s): ");

                builder.Append(registration.Name).Append(", ");
            }
        }

        if (builder is not null)
        {
            throw new ArgumentException(builder.ToString(0, builder.Length - 2), nameof(registrations));
        }
    }

    private bool ByPredicate(HealthCheckRegistration registration, Func<HealthCheckRegistration, bool>? predicate)
    {
        // Default to the HealthCheckPublisher predicate
        if (predicate == null)
        {
            predicate = _healthCheckPublisherOptions.Value.Predicate;
        }

        if (predicate == null)
        {
            return true;
        }

        return predicate(registration);
    }

    internal static class EventIds
    {
        public const int HealthCheckProcessingBeginId = 100;
        public const int HealthCheckProcessingEndId = 101;
        public const int HealthCheckBeginId = 102;
        public const int HealthCheckEndId = 103;
        public const int HealthCheckErrorId = 104;
        public const int HealthCheckDataId = 105;

        // Hard code the event names to avoid breaking changes. Even if the methods are renamed, these hard-coded names shouldn't change.
        public const string HealthCheckProcessingBeginName = "HealthCheckProcessingBegin";
        public const string HealthCheckProcessingEndName = "HealthCheckProcessingEnd";
        public const string HealthCheckBeginName = "HealthCheckBegin";
        public const string HealthCheckEndName = "HealthCheckEnd";
        public const string HealthCheckErrorName = "HealthCheckError";
        public const string HealthCheckDataName = "HealthCheckData";

        public static readonly EventId HealthCheckData = new EventId(HealthCheckDataId, HealthCheckDataName);
    }

    private static partial class Log
    {
        [LoggerMessage(EventIds.HealthCheckProcessingBeginId, LogLevel.Debug, "Running health checks", EventName = EventIds.HealthCheckProcessingBeginName)]
        public static partial void HealthCheckProcessingBegin(ILogger logger);

        public static void HealthCheckProcessingEnd(ILogger logger, HealthStatus status, TimeSpan duration) =>
            HealthCheckProcessingEnd(logger, status, duration.TotalMilliseconds);

        [LoggerMessage(EventIds.HealthCheckProcessingEndId, LogLevel.Debug, "Health check processing with combined status {HealthStatus} completed after {ElapsedMilliseconds}ms", EventName = EventIds.HealthCheckProcessingEndName)]
        private static partial void HealthCheckProcessingEnd(ILogger logger, HealthStatus HealthStatus, double ElapsedMilliseconds);

        [LoggerMessage(EventIds.HealthCheckBeginId, LogLevel.Debug, "Running health check {HealthCheckName}", EventName = EventIds.HealthCheckBeginName)]
        public static partial void HealthCheckBegin(ILogger logger, string HealthCheckName);

        // These are separate so they can have different log levels
        private const string HealthCheckEndText = "Health check {HealthCheckName} with status {HealthStatus} completed after {ElapsedMilliseconds}ms with message '{HealthCheckDescription}'";

#pragma warning disable SYSLIB1006
        [LoggerMessage(EventIds.HealthCheckEndId, LogLevel.Debug, HealthCheckEndText, EventName = EventIds.HealthCheckEndName)]
        private static partial void HealthCheckEndHealthy(ILogger logger, string HealthCheckName, HealthStatus HealthStatus, double ElapsedMilliseconds, string? HealthCheckDescription);

        [LoggerMessage(EventIds.HealthCheckEndId, LogLevel.Warning, HealthCheckEndText, EventName = EventIds.HealthCheckEndName)]
        private static partial void HealthCheckEndDegraded(ILogger logger, string HealthCheckName, HealthStatus HealthStatus, double ElapsedMilliseconds, string? HealthCheckDescription, Exception? exception);

        [LoggerMessage(EventIds.HealthCheckEndId, LogLevel.Error, HealthCheckEndText, EventName = EventIds.HealthCheckEndName)]
        private static partial void HealthCheckEndUnhealthy(ILogger logger, string HealthCheckName, HealthStatus HealthStatus, double ElapsedMilliseconds, string? HealthCheckDescription, Exception? exception);
#pragma warning restore SYSLIB1006

        public static void HealthCheckEnd(ILogger logger, HealthCheckRegistration registration, HealthReportEntry entry, TimeSpan duration)
        {
            switch (entry.Status)
            {
                case HealthStatus.Healthy:
                    HealthCheckEndHealthy(logger, registration.Name, entry.Status, duration.TotalMilliseconds, entry.Description);
                    break;

                case HealthStatus.Degraded:
                    HealthCheckEndDegraded(logger, registration.Name, entry.Status, duration.TotalMilliseconds, entry.Description, entry.Exception);
                    break;

                case HealthStatus.Unhealthy:
                    HealthCheckEndUnhealthy(logger, registration.Name, entry.Status, duration.TotalMilliseconds, entry.Description, entry.Exception);
                    break;
            }
        }

        [LoggerMessage(EventIds.HealthCheckErrorId, LogLevel.Error, "Health check {HealthCheckName} threw an unhandled exception after {ElapsedMilliseconds}ms", EventName = EventIds.HealthCheckErrorName)]
        private static partial void HealthCheckError(ILogger logger, string HealthCheckName, double ElapsedMilliseconds, Exception exception);

        public static void HealthCheckError(ILogger logger, HealthCheckRegistration registration, Exception exception, TimeSpan duration) =>
            HealthCheckError(logger, registration.Name, duration.TotalMilliseconds, exception);

        public static void HealthCheckData(ILogger logger, HealthCheckRegistration registration, HealthReportEntry entry)
        {
            if (entry.Data.Count > 0 && logger.IsEnabled(LogLevel.Debug))
            {
                logger.Log(
                    LogLevel.Debug,
                    EventIds.HealthCheckData,
                    new HealthCheckDataLogValue(registration.Name, entry.Data),
                    null,
                    (state, ex) => state.ToString());
            }
        }
    }

    internal sealed class HealthCheckDataLogValue : IReadOnlyList<KeyValuePair<string, object>>
    {
        private readonly string _name;
        private readonly List<KeyValuePair<string, object>> _values;

        private string? _formatted;

        public HealthCheckDataLogValue(string name, IReadOnlyDictionary<string, object> values)
        {
            _name = name;
            _values = values.ToList();

            // We add the name as a kvp so that you can filter by health check name in the logs.
            // This is the same parameter name used in the other logs.
            _values.Add(new KeyValuePair<string, object>("HealthCheckName", name));
        }

        public KeyValuePair<string, object> this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                {
                    throw new IndexOutOfRangeException(nameof(index));
                }

                return _values[index];
            }
        }

        public int Count => _values.Count;

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        public override string ToString()
        {
            if (_formatted == null)
            {
                var builder = new StringBuilder();
                builder.AppendLine(FormattableString.Invariant($"Health check data for {_name}:"));

                var values = _values;
                for (var i = 0; i < values.Count; i++)
                {
                    var kvp = values[i];
                    builder.Append("    ");
                    builder.Append(kvp.Key);
                    builder.Append(": ");

                    builder.AppendLine(kvp.Value?.ToString());
                }

                _formatted = builder.ToString();
            }

            return _formatted;
        }
    }
}
