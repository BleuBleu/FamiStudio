GIF decoder
===========

This is a small C library that can be used to read GIF files.

Features
--------

  * support for all standard GIF features
  * support for Netscape Application Extension (looping information)
  * other extensions may be easily supported via user hooks
  * small and portable: less than 500 lines of C99
  * public domain


Limitations
-----------

  * no support for GIF files that don't have a global color table
  * no direct support for the plain text extension (rarely used)


Documentation
-------------

0. Essential GIF concepts

GIF  animations  are  stored  in  files as  a  series  of  palette-based
compressed frames.

In order to display the animation, a  program must lay the frames on top
of a  fixed-size canvas,  one after  the other. Each  frame has  a size,
position and  duration. Each  frame can  have its own  palette or  use a
global palette defined in the beginning of the file.

In order to  properly use extension hooks, it's  necessary to understand
how GIF files store variable-sized data. A GIF block of variable size is
a sequence  of sub-blocks. The first  byte in a sub-block  indicates the
number of data bytes to follow. The  end of the block is indicated by an
empty sub-block: one  byte of value 0x00. For instance,  a data block of
600 bytes is stored as 4 sub-blocks:

  255, <255 data bytes>, 255, <255 data bytes>, 90, <90 data bytes>, 0

1. Opening and closing a GIF file

The function `gd_open_gif()` tries to open a GIF file for reading.

    gd_GIF *gd_open_gif(const char *fname);

If this function fails, it returns NULL.

If `gd_open_gif()` succeeds, it returns  a GIF handler (`gd_GIF *`). The
GIF handler  can be passed to  the other gifdec functions  to decode GIF
metadata and frames.

To close  the GIF file  and free memory after  it has been  decoded, the
function `gd_close_gif()` must be called.

    void gd_close_gif(gd_GIF *gif);

2. Reading GIF attributes

Once a GIF file has been successfully opened, some basic information can
be read directly from the GIF handler:

    gd_GIF *gif = gd_open_gif("animation.gif");
    printf("canvas size: %ux%u\n", gif->width, gif->height);
    printf("number of colors: %d\n", gif->palette->size);

3. Reading frames

The function `gd_get_frame()` decodes one frame from the GIF file.

    int gd_get_frame(gd_GIF *gif);

This function returns 0 if there are no more frames to read.

The decoded frame  is stored in `gif->frame`, which is  a buffer of size
`gif->width * gif->height`, in bytes. Each byte value is an index to the
palette at `gif->palette`.

Since GIF files often only store  the rectangular region of a frame that
changed  from the  previous frame,  this function  will only  update the
bytes in `gif->frame`  that are in that region. For  GIF files that only
use  the global  palette, the  whole state  of the  canvas is  stored in
`gif->frame`  at all  times,  in the  form of  an  indexed color  image.
However, when  local palettes are  used, it's  not enough to  keep color
indices from previous frames. The color RGB values themselves need to be
stored.

For this  reason, in order  to get the whole  state of the  canvas after
a  new  frame  has  been  read, it's  necessary  to  call  the  function
`gd_render_frame()`, which writes all pixels to a given buffer.

    void gd_render_frame(gd_GIF *gif, uint8_t *buffer);

The buffer  size must  be at  least `gif->width *  gif->height *  3`, in
bytes. The function `gd_render_frame()` writes  the 24-bit RGB values of
all canvas pixels in it.

4. Frame duration

GIF animations  are not  required to  have a  constant frame  rate. Each
frame can  have a different duration,  which is stored right  before the
frame in a Graphic Control Extension  (GCE) block. This type of block is
read  by gifdec  into a  `gd_GCE` struct  that is  a member  of the  GIF
handler. Specifically,  the unsigned integer `gif->gce.delay`  holds the
current frame duration,  in hundreths of a second. That  means that, for
instance, if  `gif->gce.delay` is `50`,  then the current frame  must be
displayed for half a second.

5. Looping

Most GIF  animations are supposed  to loop automatically, going  back to
the first frame  after the last one is displayed.  GIF files may contain
looping instruction in the form of a non-negative number. If this number
is  zero,  the  animation  must loop  forever.  Otherwise,  this  number
indicates how many times the animation  must be played. When `gifdec` is
decoding a GIF file, this number is stored in `gif->loop_count`.

The function `gd_rewind()` must be called to go back to the start of the
GIF file without closing and reopening it.

    void gd_rewind(gd_GIF *gif);

6. Putting it all together

A simplified skeleton of a GIF viewer may look like this:

    gd_GIF *gif = gd_open_gif("some_animation.gif");
    char *buffer = malloc(gif->width * gif->height * 3);
    for (unsigned looped = 1;; looped++) {
        while (gd_get_frame(gif)) {
            gd_render_frame(gif, buffer);
            /* insert code to render buffer to screen
                and wait for delay time to pass here  */
        }
        if (looped == gif->loop_count)
            break;
        gd_rewind(gif);
    }
    free(buffer);
    gd_close_gif(gif);

7. Transparent Background

GIFs can mark a certain color in the palette as the "Background Color".
Pixels having this  color are usually treated as  transparent pixels by
applications.

The function `gd_is_bgcolor()`  can be used to check whether  a pixel in
the canvas currently has background color.

    int gd_is_bgcolor(gd_GIF *gif, uint8_t color[3]);

Here's an example of how to use it:

    gd_render_frame(gif, buffer);
    color = buffer;
    for (y = 0; y < gif->height; y++) {
        for (x = 0; x < gif->width; x++) {
            if (gd_is_bgcolor(gif, color))
                transparent_pixel(x, y);
            else
                opaque_pixel(x, y, color);
            color += 3;
        }
    }

8. Reading streamed metadata with extension hooks

Some  metadata blocks  may occur  any number  of times  in GIF  files in
between frames.  By default, gifdec  ignore these blocks.  However, it's
possible to  setup callback functions  to handle each type  of extension
block, by changing some GIF handler members.

Whenever a Comment Extension block is found, `gif->comment()` is called.

    void (*comment)(struct gd_GIF *gif);

As defined in  the GIF specification, "[t]he  Comment Extension contains
textual information which is not part  of the actual graphics in the GIF
Data Stream." Encoders  are recommended to only include  "text using the
7-bit ASCII character set" in GIF comments.

The actual comment is stored as  a variable-sized block and must be read
from  the file  (using the  file descriptor  `gif->fd`) by  the callback
function. Here's an example, printing the comment to stdout:

    void
    comment(gd_GIF *gif)
    {
        uint8_t sub_len, byte, i;
        do {
            read(gif->fd, &sub_len, 1);
            for (i = 0; i < sub_len; i++) {
                read(gif->fd, &byte, 1);
                printf("%c", byte);
            }
        } while (sub_len);
        printf("\n");
    }
    
    /* ... */
    
    /* Somewhere on the main path of execution. */
    gif->comment = comment;


Whenever a Plain  Text Extension block is  found, `gif->plain_text()` is
called.

    void (*plain_text)(
        struct gd_GIF *gif, uint16_t tx, uint16_t ty,
        uint16_t tw, uint16_t th, uint8_t cw, uint8_t ch,
        uint8_t fg, uint8_t bg
    );

According to the GIF specification, "[t]he Plain Text Extension contains
textual  data and  the parameters  necessary to  render that  data as  a
graphic  [...]". This  is  a  rarely used  extension  that requires  the
decoder to actually render text on the canvas. In order to support this,
one  must  read the  relevant  specification  and implement  a  suitable
callback function to setup as `gif->plain_text`.

The actual  plain text is stored  as a variable-sized block  and must be
read from the file by the callback function.


Whenever   an   unknown   Application    Extension   block   is   found,
`gif->application()` is called.

    void (*application)(struct gd_GIF *gif, char id[8], char auth[3]);

Application  Extensions  are  used  to  extend  GIF  with  extraofficial
features.  Currently,  gifdec  only  supports  the  so-called  "Netscape
Application  Extension",  which  is  commonly used  to  specify  looping
behavior. Other Application Extensions may be supported via this hook.

The application  data is stored  as a  variable-sized block and  must be
read from the file by the callback function.


Example
-------

The file "example.c" is  a demo GIF player based on  gifdec and SDL2. It
can be tested like this:

    $ cc `pkg-config --cflags --libs sdl2` -o gifplay gifdec.c example.c
    $ ./gifplay animation.gif

That should display the animation. Press SPACE to pause and Q to quit.

Copying
-------

All of the source code and documentation for gifdec is released into the
public domain and provided without warranty of any kind.
