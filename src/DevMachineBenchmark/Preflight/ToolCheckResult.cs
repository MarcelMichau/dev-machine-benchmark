namespace DevMachineBenchmark.Preflight;

public sealed record ToolCheckResult(
    string ToolName,
    bool IsInstalled,
    string? Version,
    bool IsRequired);
