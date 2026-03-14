using DevMachineBenchmark.Benchmarks;

namespace DevMachineBenchmark.Preflight;

public static class PreflightValidator
{
    public static async Task<List<ToolCheckResult>> ValidateAsync()
    {
        var checks = new List<(string Name, string Command, string Args, bool Required)>
        {
            ("git", "git", "--version", true),
            ("dotnet", "dotnet", "--version", true),
            ("node", "node", "--version", false),
            ("npm", "npm", "--version", false),
            ("pnpm", "pnpm", "--version", false),
            ("docker", "docker", "--version", false),
        };

        var results = new List<ToolCheckResult>();

        foreach (var (name, command, args, required) in checks)
        {
            string? version = null;
            var installed = false;

            try
            {
                var result = await ProcessRunner.RunAsync(command, args, ".");
                if (result.ExitCode == 0)
                {
                    installed = true;
                    version = result.StandardOutput.Trim();
                }
            }
            catch
            {
                // Tool not found
            }

            results.Add(new ToolCheckResult(name, installed, version, required));
        }

        return results;
    }

    public static void PrintResults(List<ToolCheckResult> results)
    {
        Console.WriteLine();
        Console.WriteLine("Preflight Check Results:");
        Console.WriteLine(new string('-', 60));
        Console.WriteLine($"{"Tool",-12} {"Status",-12} {"Required",-10} Version");
        Console.WriteLine(new string('-', 60));

        foreach (var r in results)
        {
            var status = r.IsInstalled ? "OK" : "MISSING";
            var required = r.IsRequired ? "Yes" : "No";
            var version = r.Version ?? "-";
            Console.WriteLine($"{r.ToolName,-12} {status,-12} {required,-10} {version}");
        }

        Console.WriteLine(new string('-', 60));

        var missingRequired = results.Where(r => r.IsRequired && !r.IsInstalled).ToList();
        if (missingRequired.Count > 0)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: Required tools missing: {string.Join(", ", missingRequired.Select(r => r.ToolName))}");
            Console.ResetColor();
        }

        var missingOptional = results.Where(r => !r.IsRequired && !r.IsInstalled).ToList();
        if (missingOptional.Count > 0)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"WARNING: Optional tools missing (related benchmarks will be skipped): {string.Join(", ", missingOptional.Select(r => r.ToolName))}");
            Console.ResetColor();
        }

        Console.WriteLine();
    }

    public static bool HasRequiredTools(List<ToolCheckResult> results) =>
        results.Where(r => r.IsRequired).All(r => r.IsInstalled);

    public static bool HasTool(List<ToolCheckResult> results, string name) =>
        results.Any(r => r.ToolName == name && r.IsInstalled);
}
