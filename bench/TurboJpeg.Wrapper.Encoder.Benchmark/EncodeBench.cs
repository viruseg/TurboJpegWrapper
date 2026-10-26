using BenchmarkDotNet.Attributes;

namespace TurboJpegWrapper.Encoder.Benchmark;

[MemoryDiagnoser]
public class EncodeBench
{
    private byte[] _bmp = [];

    [GlobalSetup]
    public void GlobalSetup()
    {
        string? path = Environment.GetEnvironmentVariable(BmpFile.EnvironmentVariable);

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException(
                $"Set {BmpFile.EnvironmentVariable} to the path of the BMP file to benchmark.");
        }

        _bmp = File.ReadAllBytes(path);
    }

    [Benchmark(Baseline = true)]
    public byte[] EncodeTurboJpeg()
    {
        return TurboJpeg.EncodeToJpeg(_bmp);
    }

    [Benchmark]
    public byte[] EncodeGdi()
    {
        ImgConverterGdi.ConvertToJpeg(_bmp, out var result);
        return result;
    }

    [Benchmark]
    public byte[] EncodeImageSharp()
    {
        ImgConverterImageSharp.ConvertToJpeg(_bmp, out var result);
        return result;
    }
}
