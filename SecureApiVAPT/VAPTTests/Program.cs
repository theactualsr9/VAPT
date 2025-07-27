using Microsoft.Extensions.Logging;

namespace VAPTTests;

class Program
{
    static async Task Main(string[] args)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });

        var logger = loggerFactory.CreateLogger<Program>();
        
        logger.LogInformation("🎯 VAPT Security Testing Tool");
        logger.LogInformation("Target: http://localhost:5000");
        logger.LogInformation("Starting tests...");

        var runner = new VAPTRunner("http://localhost:5000", loggerFactory.CreateLogger<VAPTRunner>(), loggerFactory);
        await runner.RunVAPTTests();

        logger.LogInformation("✅ VAPT testing completed successfully");
    }
}
