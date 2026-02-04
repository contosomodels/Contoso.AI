using Contoso.AI;
using System.Runtime.Versioning;

namespace Contoso.AI.ConsoleTest;

[SupportedOSPlatform("windows10.0.19041.0")]
class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("=== Politeness Analyzer Console Test ===\n");

        var testCases = new[]
        {
            new TestCase("Thank you so much for your help!", PolitenessLevel.Polite),
            new TestCase("I appreciate your patience on this matter. If you could provide those details when you get a chance, that would be helpful.", PolitenessLevel.SomewhatPolite),
            new TestCase("The meeting has been rescheduled to 3 PM tomorrow. Please update your calendar accordingly.", PolitenessLevel.Neutral),
            new TestCase("You clearly have no idea what you're talking about. Maybe you should educate yourself before wasting everyone's time with such ignorant questions.", PolitenessLevel.Impolite)
        };

        int passed = 0;
        int failed = 0;

        try
        {
            Console.WriteLine("Initializing analyzer...");
            var readyResult = await PolitenessAnalyzer.EnsureReadyAsync();
            
            if (readyResult.Status != AIFeatureReadyResultState.Success)
            {
                Console.WriteLine($"? Failed to initialize analyzer: {readyResult.ExtendedError?.Message ?? "Unknown error"}");
                return 1;
            }

            var analyzer = await PolitenessAnalyzer.CreateAsync();
            Console.WriteLine("? Analyzer initialized successfully\n");

            for (int i = 0; i < testCases.Length; i++)
            {
                var testCase = testCases[i];
                Console.WriteLine($"Test {i + 1}: \"{testCase.Text}\"");
                Console.WriteLine($"Expected: {testCase.ExpectedLevel}");

                var result = await analyzer.AnalyzeAsync(testCase.Text);
                Console.WriteLine($"Actual: {result.Level}");
                Console.WriteLine($"Description: {result.Description}");
                Console.WriteLine($"Inference Time: {result.InferenceTimeMs}ms");

                if (result.Level == testCase.ExpectedLevel)
                {
                    Console.WriteLine("? PASSED");
                    passed++;
                }
                else
                {
                    Console.WriteLine("? FAILED");
                    failed++;
                }

                Console.WriteLine();
            }

            analyzer.Dispose();

            Console.WriteLine("=== Test Summary ===");
            Console.WriteLine($"Total: {testCases.Length}");
            Console.WriteLine($"Passed: {passed}");
            Console.WriteLine($"Failed: {failed}");

            if (failed == 0)
            {
                Console.WriteLine("\n? All tests passed!");
                return 0;
            }
            else
            {
                Console.WriteLine($"\n? {failed} test(s) failed!");
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n? Unhandled exception: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    private record TestCase(string Text, PolitenessLevel ExpectedLevel);
}
