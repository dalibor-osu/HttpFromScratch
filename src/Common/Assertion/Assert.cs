using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace HttpFromScratch.Common.Assertion;

public static class Assert
{
    [Conditional("DEBUG")]
    public static void That([DoesNotReturnIf(false)] bool condition, string? message = null,
        [CallerArgumentExpression(nameof(condition))] string? conditionName = null)
    {
        if (condition)
        {
            return;
        }

        string finalMessage = string.IsNullOrEmpty(message) ? $"Assertion failed: {conditionName}" : $"{message} ({conditionName})";
        throw new AssertionException(finalMessage);
    }
}