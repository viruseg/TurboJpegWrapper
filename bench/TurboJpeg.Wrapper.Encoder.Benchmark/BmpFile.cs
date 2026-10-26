namespace TurboJpegWrapper.Encoder.Benchmark;

internal static class BmpFile
{
    internal const string EnvironmentVariable = "TURBOJPEG_BENCHMARK_BMP";

    internal static string PromptPath()
    {
        while (true)
        {
            Console.Write("Path to a BMP file: ");
            string? input = Console.ReadLine();

            if (input is null)
            {
                throw new InvalidOperationException("A BMP file path is required.");
            }

            string path = input.Trim().Trim('"');

            if (path.Length == 0)
            {
                Console.WriteLine("The path must not be empty.");
                continue;
            }

            if (!File.Exists(path))
            {
                Console.WriteLine($"File not found: {path}");
                continue;
            }

            return Path.GetFullPath(path);
        }
    }
}
