namespace TurboJpeg.Wrapper.Decoder.Benchmark;

internal static class JpegFile
{
    internal const string EnvironmentVariable = "TURBOJPEG_BENCHMARK_JPEG";

    internal static string PromptPath()
    {
        while (true)
        {
            Console.Write("Path to a JPEG file: ");
            string? input = Console.ReadLine();

            if (input is null)
            {
                throw new InvalidOperationException("A JPEG file path is required.");
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
