using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace VAPTTests;

public class VAPTRunner
{
    private readonly string _baseUrl;
    private readonly ILogger<VAPTRunner> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly VAPTTests _vaptTests;
    private readonly List<TestResult> _testResults;

    public VAPTRunner(string baseUrl, ILogger<VAPTRunner> logger, ILoggerFactory loggerFactory)
    {
        _baseUrl = baseUrl;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _vaptTests = new VAPTTests(baseUrl, loggerFactory.CreateLogger<VAPTTests>());
        _testResults = new List<TestResult>();
    }

    public async Task RunVAPTTests()
    {
        _logger.LogInformation("🚀 Starting VAPT Security Testing Suite");
        _logger.LogInformation("Target API: {BaseUrl}", _baseUrl);
        _logger.LogInformation("Timestamp: {Timestamp}", DateTime.UtcNow);

        var startTime = DateTime.UtcNow;

        try
        {
            await _vaptTests.RunAllTests();
            _logger.LogInformation("✅ All VAPT tests completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ VAPT tests failed with exception");
        }

        var endTime = DateTime.UtcNow;
        var duration = endTime - startTime;

        _logger.LogInformation("📊 VAPT Testing Summary");
        _logger.LogInformation("Duration: {Duration}", duration);
        _logger.LogInformation("Completed at: {EndTime}", endTime);

        await GenerateVAPTReport();
    }

    private async Task GenerateVAPTReport()
    {
        var report = new VAPTReport
        {
            Timestamp = DateTime.UtcNow,
            TargetUrl = _baseUrl,
            TestResults = _testResults,
            Summary = new VAPTSummary
            {
                TotalTests = _testResults.Count,
                PassedTests = _testResults.Count(r => r.Status == TestStatus.Passed),
                FailedTests = _testResults.Count(r => r.Status == TestStatus.Failed),
                WarningTests = _testResults.Count(r => r.Status == TestStatus.Warning)
            }
        };

        var reportJson = JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        var reportPath = $"vapt_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
        await File.WriteAllTextAsync(reportPath, reportJson);

        _logger.LogInformation("📄 VAPT Report generated: {ReportPath}", reportPath);
        _logger.LogInformation("📈 Summary: {Passed}/{Total} tests passed", report.Summary.PassedTests, report.Summary.TotalTests);
    }
}

public class TestResult
{
    public string TestName { get; set; } = string.Empty;
    public TestStatus Status { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; }
    public TimeSpan Duration { get; set; }
}

public enum TestStatus
{
    Passed,
    Failed,
    Warning
}

public class VAPTReport
{
    public DateTime Timestamp { get; set; }
    public string TargetUrl { get; set; } = string.Empty;
    public List<TestResult> TestResults { get; set; } = new();
    public VAPTSummary Summary { get; set; } = new();
}

public class VAPTSummary
{
    public int TotalTests { get; set; }
    public int PassedTests { get; set; }
    public int FailedTests { get; set; }
    public int WarningTests { get; set; }
} 