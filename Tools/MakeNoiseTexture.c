#include <stdio.h>
#include <math.h>

unsigned char rand (int x, int y) {
	float r = sinf (x * 11.9898f + y * 78.233f) * 137.5453f;
	float i;
	return (unsigned char)(modff (r, &i) * 256.0f);
}

int main (void) {
	FILE* file = fopen ("NOISE.RGBA.64x64.RAW.data", "wb");
	unsigned char RGBA [4];
	RGBA [2] = RGBA [3] = 255;
	for (int yCurrent = 63; yCurrent >= 0; --yCurrent) {
		int yPrevious = (yCurrent - 17) & 63;
		for (int xCurrent = 0; xCurrent <= 63; ++xCurrent) {
			int xPrevious = (xCurrent - 37) & 63;
			RGBA [0] = rand (xPrevious, yPrevious);
			RGBA [1] = rand (xCurrent, yCurrent);
			fwrite (RGBA, sizeof (RGBA), 1, file);
		}
	}
	fclose (file);
}
