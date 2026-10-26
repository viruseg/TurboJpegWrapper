# Test fixtures

All fixtures were produced with libjpeg-turbo 3.2.0: `cjpeg` for the cjpeg fixtures and the TurboJPEG API for the CMYK/YCCK and `lossless-p12-wide.jpg` fixtures. Fixtures are 35×23 unless noted; the CMYK/YCCK fixtures are 4×1 or 16×1, and `lossless-p12-wide.jpg` is 2048×2.

## Source images

- `turbojpeg-fixture.ppm`: 35×23 PPM with horizontal bands of 4 rows: red, green, blue, white, black, and then 3 rows of gray.
- `in8.ppm`: 35×23 8-bit PPM. Rows 0-14 are 3-row bands: red, green, blue, white, black. Rows 15-22 are 3 rows of 32, 3 rows of 128, and 2 rows of 64.
- `in16.ppm`: the same 35×23 layout as `in8.ppm` with 16-bit samples: 65535/0 in the color bands and 8192, 32768, 16384 in the gray rows.
- `gray16.pgm`: 35×23 16-bit PGM with 4-row bands of 0, 8192, 16384, 32768, 49151, and a final 3-row 65535 band.

## cjpeg fixtures

Existing 8-bit and 12-bit fixtures:

```text
cjpeg -quality 95 -sample 1x1 -outfile baseline-444.jpg turbojpeg-fixture.ppm
cjpeg -quality 90 -sample 2x2 -outfile baseline-420.jpg turbojpeg-fixture.ppm
cjpeg -grayscale -quality 95 -outfile grayscale.jpg turbojpeg-fixture.ppm
cjpeg -progressive -quality 90 -outfile progressive.jpg turbojpeg-fixture.ppm
cjpeg -precision 12 -quality 90 -outfile precision12.jpg turbojpeg-fixture.ppm
cjpeg -lossless 1 -outfile lossless.jpg turbojpeg-fixture.ppm
```

8-bit arithmetic, unknown-subsampling, and warning fixtures:

```text
cjpeg -quality 90 -sample 2x2 -outfile s420.jpg in8.ppm
cjpeg -arithmetic -quality 90 -sample 2x2 -outfile arithmetic.jpg in8.ppm
cjpeg -quality 90 -sample 3x1,1x1,1x1 -outfile s-unknown.jpg in8.ppm
```

Lossless RGB precision fixtures:

```text
cjpeg -lossless 1 -precision 2 -outfile lossless-p2.jpg in16.ppm
cjpeg -lossless 1 -precision 4 -outfile lossless-p4.jpg in16.ppm
cjpeg -lossless 1 -precision 7 -outfile lossless-p7.jpg in16.ppm
cjpeg -lossless 1 -precision 8 -outfile lossless-p8.jpg in16.ppm
cjpeg -lossless 1 -precision 9 -outfile lossless-p9.jpg in16.ppm
cjpeg -lossless 1 -precision 10 -outfile lossless-p10.jpg in16.ppm
cjpeg -lossless 1 -precision 11 -outfile lossless-p11.jpg in16.ppm
cjpeg -lossless 1 -precision 12 -outfile lossless-p12.jpg in16.ppm
cjpeg -lossless 1 -precision 13 -outfile lossless-p13.jpg in16.ppm
cjpeg -lossless 1 -precision 14 -outfile lossless-p14.jpg in16.ppm
cjpeg -lossless 1 -precision 15 -outfile lossless-p15.jpg in16.ppm
cjpeg -lossless 1 -precision 16 -outfile lossless-p16.jpg in16.ppm
```

Lossless GRAY and 12-bit lossy GRAY fixtures:

```text
cjpeg -lossless 1 -precision 2 -outfile gray-lossless-p2.jpg gray16.pgm
cjpeg -lossless 1 -precision 4 -outfile gray-lossless-p4.jpg gray16.pgm
cjpeg -lossless 1 -precision 7 -outfile gray-lossless-p7.jpg gray16.pgm
cjpeg -lossless 1 -precision 8 -outfile gray-lossless-p8.jpg gray16.pgm
cjpeg -lossless 1 -precision 9 -outfile gray-lossless-p9.jpg gray16.pgm
cjpeg -lossless 1 -precision 12 -outfile gray-lossless-p12.jpg gray16.pgm
cjpeg -lossless 1 -precision 16 -outfile gray-lossless-p16.jpg gray16.pgm
cjpeg -grayscale -precision 12 -quality 90 -outfile gray-lossy-p12.jpg gray16.pgm
```

`cjpeg -grayscale` is ignored for lossless output, so the lossless GRAY fixtures are generated from the 16-bit PGM source rather than from a color PPM.

## TurboJPEG API fixtures

`lossless-p12-wide.jpg` was generated with the TurboJPEG API, not `cjpeg`:

- `tj3Compress12`, 2048×2, `TJPF_BGR`, pitch 6144 samples.
- `TJPARAM_QUALITY=100`, `TJPARAM_SUBSAMP=TJSAMP_444`, `TJPARAM_PRECISION=12`, `TJPARAM_LOSSLESS=1`, `TJPARAM_NOREALLOC=1`.
- The source repeats the BGR triples `(0,0,4095)`, `(0,4095,0)`, `(4095,0,0)`, and `(4095,4095,4095)` across each row.

The CMYK and YCCK fixtures use `TJPARAM_QUALITY=100`, `TJPARAM_SUBSAMP=TJSAMP_444`, `TJPARAM_COLORSPACE` from the table, `TJPARAM_PRECISION` from the table, and `TJPARAM_NOREALLOC=1`. Set `TJPARAM_LOSSLESS=1` for the lossless rows and leave it unset for the lossy rows. The source pixel format is `TJPF_CMYK`. For 8-bit calls the pitch is bytes; for 12/16-bit calls it is samples per row.

| Fixture | API call | Size | Precision / coding | Colorspace | Source samples |
| --- | --- | --- | --- | --- | --- |
| `cmyk.jpg` | `tj3Compress8` | 4×1 | 8-bit lossy | `TJCS_CMYK` | Bytes `FF FF FF FF`, `FF FF FF 00`, `FF 00 00 FF`, `00 FF FF FF` |
| `ycck.jpg` | `tj3Compress8` | 4×1 | 8-bit lossy | `TJCS_YCCK` | The same four byte tuples as `cmyk.jpg` |
| `cmyk-p4.jpg` | `tj3Compress8` | 4×1 | 4-bit lossless | `TJCS_CMYK` | Bytes `0F 0F 0F 0F`, `0F 0F 0F 00`, `0F 00 00 0F`, `00 0F 0F 0F` |
| `cmyk12.jpg` | `tj3Compress12` | 4×1 | 12-bit lossy | `TJCS_CMYK` | Shorts `4095,4095,4095,4095`, `4095,4095,4095,0`, `4095,0,0,4095`, `0,4095,4095,4095` |
| `cmyk16.jpg` | `tj3Compress16` | 4×1 | 16-bit lossless | `TJCS_CMYK` | The same values as `cmyk12.jpg` widened to `ushort` (4095 becomes 65535; 0 stays 0) |
| `cmyk-wide.jpg` | `tj3Compress8` | 16×1 | 8-bit lossy | `TJCS_CMYK` | The four byte tuples from `cmyk.jpg`, repeated four times |

All CMYK/YCCK source samples are already inverted Adobe CMYK. In order, the four pixels represent white, black, red, and cyan under that convention. `cmyk-wide.jpg` repeats those four pixels four times.

`s420.jpg` is an intermediate used only to build the warning fixtures; it is not committed.

## Warning fixtures

The warning fixtures are byte-level mutations of cjpeg output:

```text
head -c 917 s420.jpg > truncated.jpg
head -c 2 s420.jpg > extraneous.jpg
printf 'JUNKJUNK' >> extraneous.jpg
tail -c +3 s420.jpg >> extraneous.jpg
head -c 446 precision12.jpg > truncated12.jpg
```

- `truncated.jpg`: the first 917 bytes of the 977-byte `s420.jpg`, removing the end of the entropy-coded data.
- `extraneous.jpg`: `s420.jpg` with the 8 ASCII bytes `JUNKJUNK` inserted immediately after the SOI marker.
- `truncated12.jpg`: the first 446 bytes of the 486-byte `precision12.jpg`, removing the end of the 12-bit entropy-coded data.
- `s-unknown.jpg` is not a warning fixture. It is a 3×1 subsampled 8-bit JPEG whose sampling scheme TurboJPEG reports as unknown, and it is used to test crop rejection.
