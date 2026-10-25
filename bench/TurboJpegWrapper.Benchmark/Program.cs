using BenchmarkDotNet.Running;
using TurboJpegWrapper.Benchmark;

string jpegPath;
try
{
    jpegPath = JpegFile.PromptPath();
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

Environment.SetEnvironmentVariable(JpegFile.EnvironmentVariable, jpegPath);

BenchmarkRunner.Run<DecodeBench>();
return 0;
