using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace SecureApiVAPT.Attributes;

public class NoXssAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value == null || value is not string input)
            return true;

        return !ContainsXss(input);
    }

    private static bool ContainsXss(string input)
    {
        if (string.IsNullOrEmpty(input)) return false;

        var xssPatterns = new[]
        {
            @"<script[^>]*>.*?</script>",
            @"javascript:",
            @"on\w+\s*=",
            @"<iframe[^>]*>",
            @"<object[^>]*>",
            @"<embed[^>]*>",
            @"<link[^>]*>",
            @"<meta[^>]*>",
            @"vbscript:",
            @"data:",
            @"&#x?[0-9a-f]+;",
            @"&#[0-9]+;",
            @"<.*?>"
        };

        return xssPatterns.Any(pattern => Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase));
    }

    public override string FormatErrorMessage(string name)
    {
        return $"The field {name} contains potentially dangerous content.";
    }
}
