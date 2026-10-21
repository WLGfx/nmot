value = noise(vec3)
byte = (value).normal 0 - 255
byte = (flags)

bit usage
0 - X-
1 - X+
2 - Y-
3 - Y+
4 - Z-
5 - Z+
6 - ...
7 - ...

1 byte = 0 - 255
2 bytes = 0 - 65535
4 bytes = 0 - 21

Voxel data = 8 voxels per byte @ LOD / 8 on x axis

The noise value could be cut down again between 0 - 15 (4 bits)
This leaves 12 bits, 6 for faces, 6 free

Can a GPU read bit values... Lets ask one...

Yes they can with a usampler instead of sampler.

However, this will be done using a compute shader using the noise data.
((Qpalzm@12))