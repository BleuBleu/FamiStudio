#include "gifdec.h"
#include <stdlib.h>
#include <string.h>

#ifdef LINUX
#define __stdcall
#endif

gd_GIF* __stdcall GifOpen(const char* file, int swap)
{
    return gd_open_gif(file, swap);
}

int __stdcall GifGetWidth(gd_GIF* gif)
{
    return gif->width;
}

int __stdcall GifGetHeight(gd_GIF* gif)
{
    return gif->height;
}

int __stdcall GifAdvanceFrame(gd_GIF* gif)
{
    int ret = gd_get_frame(gif);

    if (ret == 0)
    {
        gd_rewind(gif);
        ret = gd_get_frame(gif);
    }

    if (ret == -1)
        return -1;
}

void __stdcall GifRenderFrame(gd_GIF* gif, unsigned char* buffer, int stride, int channels)
{
    unsigned char* color = buffer;

    // Create temp buffer if stride isnt a perfect match or if we need to add alpha.
    if (channels != 3 || stride != gif->width * 3)
    {
        color = malloc(stride * gif->height);
    }

    gd_render_frame(gif, color);

    if (color != buffer)
    {
        unsigned char* src = color;

        if (channels == 3)
        {
            for (int y = 0; y < gif->height; y++)
            {
                memcpy(&buffer[stride * y], src, gif->width * 3);
                src += gif->width * 3;
            }
        }
        else
        {
            unsigned char* dst = buffer;

            for (int y = 0; y < gif->height * gif->width; y++)
            {
                dst[0] = src[2];
                dst[1] = src[1];
                dst[2] = src[0];
                dst[3] = 255;

                src += 3;
                dst += 4;
            }
        }

        free(color);
    }
}

int __stdcall GifGetFrameDelay(gd_GIF* gif)
{
    return gif->gce.delay * 10;
}

void __stdcall GifRewind(gd_GIF* gif)
{
    return gd_rewind(gif);
}

void __stdcall GifClose(gd_GIF* gif)
{
    gd_close_gif(gif);
}
