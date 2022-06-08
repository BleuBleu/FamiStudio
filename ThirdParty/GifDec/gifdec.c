#include "gifdec.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include <sys/types.h>
#include <sys/stat.h>
#include <fcntl.h>
#ifdef _WIN32
#include <io.h>
#else
#include <unistd.h>
#endif

#define MIN(A, B) ((A) < (B) ? (A) : (B))
#define MAX(A, B) ((A) > (B) ? (A) : (B))

typedef struct Entry {
    uint16_t length;
    uint16_t prefix;
    uint8_t  suffix;
} Entry;

typedef struct Table {
    int bulk;
    int nentries;
    Entry *entries;
} Table;

static void
swap_bytes(uint8_t* a, uint8_t* b)
{
    uint8_t tmp = *a;
    *a = *b;
    *b = tmp;
}

static uint16_t
read_num(const uint8_t** data)
{
    uint16_t num = (*data)[0] + (((uint16_t)(*data)[1]) << 8);
    *data += 2;
    return num;
}

static void
read_data(const uint8_t** data, void* p, int size)
{
    memcpy(p, *data, size);
    *data += size;
}

gd_GIF *
gd_open_gif(const uint8_t* buffer, int swap)
{
    uint8_t sigver[3];
    uint16_t width, height, depth;
    uint8_t fdsz, bgidx, aspect;
    int i;
    uint8_t *bgcolor;
    int gct_sz;
    gd_GIF *gif;
    const uint8_t* data = buffer;

    if (buffer == NULL) return NULL;

    /* Header */
    read_data(&data, sigver, 3);
    if (memcmp(sigver, "GIF", 3) != 0) {
        fprintf(stderr, "invalid signature\n");
        goto fail;
    }
    /* Version */
    read_data(&data, sigver, 3);
    if (memcmp(sigver, "89a", 3) != 0) {
        fprintf(stderr, "invalid version\n");
        goto fail;
    }
    /* Width x Height */
    width  = read_num(&data);
    height = read_num(&data);
    /* FDSZ */
    read_data(&data, &fdsz, 1);
    /* Color Space's Depth */
    depth = ((fdsz >> 4) & 7) + 1;
    /* Ignore Sort Flag. */
    /* GCT Size */
    gct_sz = fdsz & 0x80 ? 1 << ((fdsz & 0x07) + 1) : 0;
    /* Background Color Index */
    read_data(&data, &bgidx, 1);
    /* Aspect Ratio */
    read_data(&data, &aspect, 1);
    /* Create gd_GIF Structure. */
    gif = calloc(1, sizeof(*gif));
    if (!gif) goto fail;
    gif->width  = width;
    gif->height = height;
    gif->depth  = depth;
    gif->swap = swap;
    /* Read GCT */
    gif->gct.size = gct_sz;
    read_data(&data, gif->gct.colors, 3 * gif->gct.size);
    if (swap)
    {
        for (i = 0; i < gif->gct.size; i++)
            swap_bytes(&gif->gct.colors[i * 3 + 0], &gif->gct.colors[i * 3 + 2]);
    }
    gif->palette = &gif->gct;
    gif->bgindex = bgidx;
    gif->frame = calloc(4, width * height);
    if (!gif->frame) {
        free(gif);
        goto fail;
    }
    gif->canvas = &gif->frame[width * height];
    if (gif->bgindex)
        memset(gif->frame, gif->bgindex, gif->width * gif->height);
    bgcolor = &gif->palette->colors[gif->bgindex*3];
    if (bgcolor[0] || bgcolor[1] || bgcolor [2])
        for (i = 0; i < gif->width * gif->height; i++)
            memcpy(&gif->canvas[i*3], bgcolor, 3);
    gif->data = data;
    gif->anim_start = data;
    goto ok;
fail:
    return 0;
ok:
    return gif;
}

static void
discard_sub_blocks(gd_GIF *gif)
{
    uint8_t size;

    do {
        read_data(&gif->data, &size, 1);
        gif->data += size;
    } while (size);
}

static void
read_plain_text_ext(gd_GIF *gif)
{
    if (gif->plain_text) {
        uint16_t tx, ty, tw, th;
        uint8_t cw, ch, fg, bg;
        const uint8_t* sub_block;
        gif->data++; /* block size = 12 */
        tx = read_num(&gif->data);
        ty = read_num(&gif->data);
        tw = read_num(&gif->data);
        th = read_num(&gif->data);
        read_data(&gif->data, &cw, 1);
        read_data(&gif->data, &ch, 1);
        read_data(&gif->data, &fg, 1);
        read_data(&gif->data, &bg, 1);
        sub_block = gif->data;
        gif->plain_text(gif, tx, ty, tw, th, cw, ch, fg, bg);
        gif->data = sub_block;
    } else {
        /* Discard plain text metadata. */
        gif->data += 13;
    }
    /* Discard plain text sub-blocks. */
    discard_sub_blocks(gif);
}

static void
read_graphic_control_ext(gd_GIF *gif)
{
    uint8_t rdit;

    /* Discard block size (always 0x04). */
    gif->data++; 
    read_data(&gif->data, &rdit, 1);
    gif->gce.disposal = (rdit >> 2) & 3;
    gif->gce.input = rdit & 2;
    gif->gce.transparency = rdit & 1;
    gif->gce.delay = read_num(&gif->data);
    read_data(&gif->data, &gif->gce.tindex, 1);
    /* Skip block terminator. */
    gif->data++;
}

static void
read_comment_ext(gd_GIF *gif)
{
    if (gif->comment) {
        const uint8_t* sub_block = gif->data;
        gif->comment(gif);
        gif->data = sub_block;
    }
    /* Discard comment sub-blocks. */
    discard_sub_blocks(gif);
}

static void
read_application_ext(gd_GIF *gif)
{
    char app_id[8];
    char app_auth_code[3];

    /* Discard block size (always 0x0B). */
    gif->data++;
    /* Application Identifier. */
    read_data(&gif->data, app_id, 8);
    /* Application Authentication Code. */
    read_data(&gif->data, app_auth_code, 3);
    if (!strncmp(app_id, "NETSCAPE", sizeof(app_id))) {
        /* Discard block size (0x03) and constant byte (0x01). */
        gif->data += 2;
        gif->loop_count = read_num(&gif->data);
        /* Skip block terminator. */
        gif->data++;
    } else if (gif->application) {
        const uint8_t* sub_block = gif->data;
        gif->application(gif, app_id, app_auth_code);
        gif->data = sub_block;
        discard_sub_blocks(gif);
    } else {
        discard_sub_blocks(gif);
    }
}

static void
read_ext(gd_GIF *gif)
{
    uint8_t label;

    read_data(&gif->data, &label, 1);
    switch (label) {
    case 0x01:
        read_plain_text_ext(gif);
        break;
    case 0xF9:
        read_graphic_control_ext(gif);
        break;
    case 0xFE:
        read_comment_ext(gif);
        break;
    case 0xFF:
        read_application_ext(gif);
        break;
    default:
        fprintf(stderr, "unknown extension: %02X\n", label);
    }
}

static Table *
new_table(int key_size)
{
    int key;
    int init_bulk = MAX(1 << (key_size + 1), 0x100);
    Table *table = malloc(sizeof(*table) + sizeof(Entry) * init_bulk);
    if (table) {
        table->bulk = init_bulk;
        table->nentries = (1 << key_size) + 2;
        table->entries = (Entry *) &table[1];
        for (key = 0; key < (1 << key_size); key++)
            table->entries[key] = (Entry) {1, 0xFFF, key};
    }
    return table;
}

/* Add table entry. Return value:
 *  0 on success
 *  +1 if key size must be incremented after this addition
 *  -1 if could not realloc table */
static int
add_entry(Table **tablep, uint16_t length, uint16_t prefix, uint8_t suffix)
{
    Table *table = *tablep;
    if (table->nentries == table->bulk) {
        table->bulk *= 2;
        table = realloc(table, sizeof(*table) + sizeof(Entry) * table->bulk);
        if (!table) return -1;
        table->entries = (Entry *) &table[1];
        *tablep = table;
    }
    table->entries[table->nentries] = (Entry) {length, prefix, suffix};
    table->nentries++;
    if ((table->nentries & (table->nentries - 1)) == 0)
        return 1;
    return 0;
}

static uint16_t
get_key(gd_GIF *gif, int key_size, uint8_t *sub_len, uint8_t *shift, uint8_t *byte)
{
    int bits_read;
    int rpad;
    int frag_size;
    uint16_t key;

    key = 0;
    for (bits_read = 0; bits_read < key_size; bits_read += frag_size) {
        rpad = (*shift + bits_read) % 8;
        if (rpad == 0) {
            /* Update byte. */
            if (*sub_len == 0) {
                read_data(&gif->data, sub_len, 1); /* Must be nonzero! */
                if (*sub_len == 0)
                    return 0x1000;
            }
            read_data(&gif->data, byte, 1);
            (*sub_len)--;
        }
        frag_size = MIN(key_size - bits_read, 8 - rpad);
        key |= ((uint16_t) ((*byte) >> rpad)) << bits_read;
    }
    /* Clear extra bits to the left. */
    key &= (1 << key_size) - 1;
    *shift = (*shift + key_size) % 8;
    return key;
}

/* Compute output index of y-th input line, in frame of height h. */
static int
interlaced_line_index(int h, int y)
{
    int p; /* number of lines in current pass */

    p = (h - 1) / 8 + 1;
    if (y < p) /* pass 1 */
        return y * 8;
    y -= p;
    p = (h - 5) / 8 + 1;
    if (y < p) /* pass 2 */
        return y * 8 + 4;
    y -= p;
    p = (h - 3) / 4 + 1;
    if (y < p) /* pass 3 */
        return y * 4 + 2;
    y -= p;
    /* pass 4 */
    return y * 2 + 1;
}

/* Decompress image pixels.
 * Return 0 on success or -1 on out-of-memory (w.r.t. LZW code table). */
static int
read_image_data(gd_GIF *gif, int interlace)
{
    uint8_t sub_len, shift, byte;
    int init_key_size, key_size, table_is_full;
    int frm_off, frm_size, str_len, i, p, x, y;
    uint16_t key, clear, stop;
    int ret;
    Table *table;
    Entry entry;
    const uint8_t* start, *end;

    read_data(&gif->data, &byte, 1);
    key_size = (int) byte;
    if (key_size < 2 || key_size > 8)
        return -1;
    
    start = gif->data;
    discard_sub_blocks(gif);
    end = gif->data;
    gif->data = start;
    clear = 1 << key_size;
    stop = clear + 1;
    table = new_table(key_size);
    key_size++;
    init_key_size = key_size;
    sub_len = shift = 0;
    key = get_key(gif, key_size, &sub_len, &shift, &byte); /* clear code */
    frm_off = 0;
    ret = 0;
    frm_size = gif->fw*gif->fh;
    while (frm_off < frm_size) {
        if (key == clear) {
            key_size = init_key_size;
            table->nentries = (1 << (key_size - 1)) + 2;
            table_is_full = 0;
        } else if (!table_is_full) {
            ret = add_entry(&table, str_len + 1, key, entry.suffix);
            if (ret == -1) {
                free(table);
                return -1;
            }
            if (table->nentries == 0x1000) {
                ret = 0;
                table_is_full = 1;
            }
        }
        key = get_key(gif, key_size, &sub_len, &shift, &byte);
        if (key == clear) continue;
        if (key == stop || key == 0x1000) break;
        if (ret == 1) key_size++;
        entry = table->entries[key];
        str_len = entry.length;
        for (i = 0; i < str_len; i++) {
            p = frm_off + entry.length - 1;
            x = p % gif->fw;
            y = p / gif->fw;
            if (interlace)
                y = interlaced_line_index((int) gif->fh, y);
            gif->frame[(gif->fy + y) * gif->width + gif->fx + x] = entry.suffix;
            if (entry.prefix == 0xFFF)
                break;
            else
                entry = table->entries[entry.prefix];
        }
        frm_off += str_len;
        if (key < table->nentries - 1 && !table_is_full)
            table->entries[table->nentries - 1].suffix = entry.suffix;
    }
    free(table);
    if (key == stop)
        read_data(&gif->data, &sub_len, 1); /* Must be zero! */
    gif->data = end;
    return 0;
}

/* Read image.
 * Return 0 on success or -1 on out-of-memory (w.r.t. LZW code table). */
static int
read_image(gd_GIF *gif)
{
    uint8_t fisrz;
    int interlace, i;

    /* Image Descriptor. */
    gif->fx = read_num(&gif->data);
    gif->fy = read_num(&gif->data);
    
    if (gif->fx >= gif->width || gif->fy >= gif->height)
        return -1;
    
    gif->fw = read_num(&gif->data);
    gif->fh = read_num(&gif->data);
    
    gif->fw = MIN(gif->fw, gif->width - gif->fx);
    gif->fh = MIN(gif->fh, gif->height - gif->fy);
    
    read_data(&gif->data, &fisrz, 1);
    interlace = fisrz & 0x40;
    /* Ignore Sort Flag. */
    /* Local Color Table? */
    if (fisrz & 0x80) {
        /* Read LCT */
        gif->lct.size = 1 << ((fisrz & 0x07) + 1);
        read_data(&gif->data, gif->lct.colors, 3 * gif->lct.size);
        if (gif->swap)
        { 
            for (i = 0; i < gif->lct.size; i++)
                swap_bytes(&gif->lct.colors[i * 3 + 0], &gif->lct.colors[i * 3 + 2]);
        }
        gif->palette = &gif->lct;
    } else
        gif->palette = &gif->gct;
    /* Image Data. */
    return read_image_data(gif, interlace);
}

static void
render_frame_rect(gd_GIF *gif, uint8_t *buffer)
{
    int i, j, k;
    uint8_t index, *color;
    i = gif->fy * gif->width + gif->fx;
    for (j = 0; j < gif->fh; j++) {
        for (k = 0; k < gif->fw; k++) {
            index = gif->frame[(gif->fy + j) * gif->width + gif->fx + k];
            color = &gif->palette->colors[index*3];
            if (!gif->gce.transparency || index != gif->gce.tindex)
                memcpy(&buffer[(i+k)*3], color, 3);
        }
        i += gif->width;
    }
}

static void
dispose(gd_GIF *gif)
{
    int i, j, k;
    uint8_t *bgcolor;
    switch (gif->gce.disposal) {
    case 2: /* Restore to background color. */
        bgcolor = &gif->palette->colors[gif->bgindex*3];
        i = gif->fy * gif->width + gif->fx;
        for (j = 0; j < gif->fh; j++) {
            for (k = 0; k < gif->fw; k++)
                memcpy(&gif->canvas[(i+k)*3], bgcolor, 3);
            i += gif->width;
        }
        break;
    case 3: /* Restore to previous, i.e., don't update canvas.*/
        break;
    default:
        /* Add frame non-transparent pixels to canvas. */
        render_frame_rect(gif, gif->canvas);
    }
}

/* Return 1 if got a frame; 0 if got GIF trailer; -1 if error. */
int
gd_get_frame(gd_GIF *gif)
{
    char sep;

    dispose(gif);
    read_data(&gif->data, &sep, 1);
    while (sep != ',') {
        if (sep == ';')
            return 0;
        if (sep == '!')
            read_ext(gif);
        else return -1;
        read_data(&gif->data, &sep, 1);
    }
    if (read_image(gif) == -1)
        return -1;
    return 1;
}

void
gd_render_frame(gd_GIF *gif, uint8_t *buffer)
{
    memcpy(buffer, gif->canvas, gif->width * gif->height * 3);
    render_frame_rect(gif, buffer);
}

int
gd_is_bgcolor(gd_GIF *gif, uint8_t color[3])
{
    return !memcmp(&gif->palette->colors[gif->bgindex*3], color, 3);
}

void
gd_rewind(gd_GIF *gif)
{
    gif->data = gif->anim_start;
}

void
gd_close_gif(gd_GIF *gif)
{
    gif->data = NULL;
    gif->anim_start = NULL;
    free(gif->frame);    
    free(gif);
}
