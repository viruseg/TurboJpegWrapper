# Test fixtures

All fixtures were generated from a single 35×23 PPM image with the libjpeg-turbo `cjpeg.exe` (3.2.0).
The PPM contains horizontal bands of 4 rows: red, green, blue, white, black, and then 3 rows of gray.

The fixtures were produced with the libjpeg-turbo `cjpeg.exe` (3.2.0) using these exact commands:

```text
cjpeg -quality 95 -sample 1x1 -outfile baseline-444.jpg turbojpeg-fixture.ppm
cjpeg -quality 90 -sample 2x2 -outfile baseline-420.jpg turbojpeg-fixture.ppm
cjpeg -grayscale -quality 95 -outfile grayscale.jpg turbojpeg-fixture.ppm
cjpeg -progressive -quality 90 -outfile progressive.jpg turbojpeg-fixture.ppm
cjpeg -precision 12 -quality 90 -outfile precision12.jpg turbojpeg-fixture.ppm
cjpeg -lossless 1 -outfile lossless.jpg turbojpeg-fixture.ppm
```

`precision12.jpg` and `lossless.jpg` exist only to test rejection/acceptance paths.
