using BenchmarkDotNet.Running;
using TurboJpegWrapper.Encoder.Benchmark;

string bmpPath;
try
{
    bmpPath = BmpFile.PromptPath();
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

Environment.SetEnvironmentVariable(BmpFile.EnvironmentVariable, bmpPath);

BenchmarkRunner.Run<EncodeBench>();
return 0;
