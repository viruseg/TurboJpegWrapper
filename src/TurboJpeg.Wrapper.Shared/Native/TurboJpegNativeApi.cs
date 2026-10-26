using System.Runtime.InteropServices;

namespace TurboJpegWrapper.Internal.Native;

[StructLayout(LayoutKind.Sequential)]
internal struct TjScalingFactor
{
    internal int Numerator;
    internal int Denominator;
}

[StructLayout(LayoutKind.Sequential)]
internal struct TjRegion
{
    internal int X;
    internal int Y;
    internal int W;
    internal int H;
}

internal static unsafe partial class TurboJpegNativeApi
{
    internal const string LibraryName = "turbojpeg";
    internal const int ApiVersion = 3_002_000;

    internal const int InitCompress = 0;
    internal const int InitDecompress = 1;

    internal const int ParamStopOnWarning = 0;
    internal const int ParamBottomUp = 1;
    internal const int ParamNoRealloc = 2;
    internal const int ParamQuality = 3;
    internal const int ParamSubsamp = 4;
    internal const int ParamJpegWidth = 5;
    internal const int ParamJpegHeight = 6;
    internal const int ParamPrecision = 7;
    internal const int ParamColorspace = 8;
    internal const int ParamFastUpsample = 9;
    internal const int ParamFastDct = 10;
    internal const int ParamOptimize = 11;
    internal const int ParamProgressive = 12;
    internal const int ParamScanLimit = 13;
    internal const int ParamArithmetic = 14;
    internal const int ParamLossless = 15;
    internal const int ParamLosslessPsv = 16;
    internal const int ParamLosslessPt = 17;
    internal const int ParamRestartBlocks = 18;
    internal const int ParamRestartRows = 19;
    internal const int ParamXDensity = 20;
    internal const int ParamYDensity = 21;
    internal const int ParamDensityUnits = 22;
    internal const int ParamMaxMemory = 23;
    internal const int ParamMaxPixels = 24;
    internal const int ParamSaveMarkers = 25;

    internal const int PixelFormatBgr = 1;
    internal const int PixelFormatBgrx = 3;
    internal const int PixelFormatGray = 6;
    internal const int PixelFormatBgra = 8;

    internal const int ErrorWarning = 0;
    internal const int ErrorFatal = 1;

    [LibraryImport(LibraryName, EntryPoint = "tj3InitVersion")]
    internal static partial nint InitVersion(int initType, int apiVersion);

    [LibraryImport(LibraryName, EntryPoint = "tj3Destroy")]
    internal static partial void Destroy(nint handle);

    [LibraryImport(LibraryName, EntryPoint = "tj3DecompressHeader")]
    internal static partial int DecompressHeader(nint handle, byte* jpegBuf, nuint jpegSize);

    [LibraryImport(LibraryName, EntryPoint = "tj3Decompress8")]
    internal static partial int Decompress8(
        nint handle,
        byte* jpegBuf,
        nuint jpegSize,
        byte* dstBuf,
        int pitch,
        int pixelFormat);

    [LibraryImport(LibraryName, EntryPoint = "tj3Decompress12")]
    internal static partial int Decompress12(
        nint handle,
        byte* jpegBuf,
        nuint jpegSize,
        short* dstBuf,
        int pitch,
        int pixelFormat);

    [LibraryImport(LibraryName, EntryPoint = "tj3Decompress16")]
    internal static partial int Decompress16(
        nint handle,
        byte* jpegBuf,
        nuint jpegSize,
        ushort* dstBuf,
        int pitch,
        int pixelFormat);

    [LibraryImport(LibraryName, EntryPoint = "tj3Get")]
    internal static partial int Get(nint handle, int param);

    [LibraryImport(LibraryName, EntryPoint = "tj3Set")]
    internal static partial int Set(nint handle, int param, int value);

    [LibraryImport(LibraryName, EntryPoint = "tj3GetErrorStr")]
    internal static partial nint GetErrorStr(nint handle);

    [LibraryImport(LibraryName, EntryPoint = "tj3GetErrorCode")]
    internal static partial int GetErrorCode(nint handle);

    [LibraryImport(LibraryName, EntryPoint = "tj3GetScalingFactors")]
    internal static partial TjScalingFactor* GetScalingFactors(out int count);

    [LibraryImport(LibraryName, EntryPoint = "tj3SetScalingFactor")]
    internal static partial int SetScalingFactor(nint handle, TjScalingFactor factor);

    [LibraryImport(LibraryName, EntryPoint = "tj3SetCroppingRegion")]
    internal static partial int SetCroppingRegion(nint handle, TjRegion region);

    [LibraryImport(LibraryName, EntryPoint = "tj3Compress8")]
    internal static partial int Compress8(
        nint handle, byte* srcBuf, int width, int pitch, int height, int pixelFormat,
        byte** jpegBuf, nuint* jpegSize);

    [LibraryImport(LibraryName, EntryPoint = "tj3JPEGBufSize")]
    internal static partial nuint JPEGBufSize(int width, int height, int jpegSubsamp);
}
