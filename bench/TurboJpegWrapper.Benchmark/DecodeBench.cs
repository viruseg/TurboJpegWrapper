using BenchmarkDotNet.Attributes;

namespace TurboJpegWrapper.Benchmark;

[MemoryDiagnoser]
public class DecodeBench
{
    private byte[] _jpeg = [];

    [GlobalSetup]
    public void GlobalSetup()
    {
        string? path = Environment.GetEnvironmentVariable(JpegFile.EnvironmentVariable);

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException(
                $"Set {JpegFile.EnvironmentVariable} to the path of the JPEG file to benchmark.");
        }

        _jpeg = File.ReadAllBytes(path);
    }

    [Benchmark(Baseline = true)]
    public byte[] DecodeTurboJpeg()
    {
        return TurboJpeg.DecodeToBmp(_jpeg);
    }

    [Benchmark]
    public byte[] DecodeGdi()
    {
        ImgConverterGdi.ConvertToBmp(_jpeg, out var result);
        return result;
    }

    [Benchmark]
    public byte[] DecodeImageSharp()
    {
        ImgConverterImageSharp.ConvertToBmp(_jpeg, out var result);
        return result;
    }
}
