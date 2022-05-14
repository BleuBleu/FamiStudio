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

int __stdcall GifAdvanceFrame(gd_GIF* gif, unsigned char* buffer, int stride)
{
    int ret = gd_get_frame(gif);

    if (ret == 0)
    {
        gd_rewind(gif);
        ret = gd_get_frame(gif);
    }

    if (ret == -1)
        return -1;

    unsigned char* color = buffer;

    // Create temp buffer if stride isnt a perfect match.
    if (stride != gif->width * 3)
    {
        color = malloc(stride * gif->height);
    }

    gd_render_frame(gif, color);

    if (color != buffer)
    {
        unsigned char* row = color;

        for (int y = 0; y < gif->height; y++)
        {
            memcpy(&buffer[stride * y], row, gif->width * 3);
            row += gif->width * 3; 
        }

        free(color);
    }

    return ret;
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
