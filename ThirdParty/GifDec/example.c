/* gifdec example -- simple GIF player using SDL2
 * compiling:
 *   cc `pkg-config --cflags --libs sdl2` -o example gifdec.c example.c
 * executing:
 *   ./example animation.gif
 * */

#include <stdio.h>

#include <SDL.h>

#include "gifdec.h"

int
main(int argc, char *argv[])
{
    SDL_Window *window;
    SDL_Renderer *renderer;
    SDL_Surface *surface;
    SDL_Texture *texture;
    SDL_Event event;
    gd_GIF *gif;
    char title[32] = {0};
    Uint8 *color, *frame;
    void *addr;
    int i, j;
    Uint32 pixel;
    int ret, paused, quit;
    Uint32 t0, t1, delay, delta;

    if (argc != 2) {
        fprintf(stderr, "usage:\n  %s gif-file\n", argv[0]);
        return 1;
    }
    gif = gd_open_gif(argv[1]);
    if (!gif) {
        fprintf(stderr, "Could not open %s\n", argv[1]);
        return 1;
    }
    frame = malloc(gif->width * gif->height * 3);
    if (!frame) {
        fprintf(stderr, "Could not allocate frame\n");
        return 1;
    }
    if (SDL_Init(SDL_INIT_VIDEO|SDL_INIT_TIMER) != 0) {
        SDL_Log("Unable to initialize SDL: %s", SDL_GetError());
        return 1;
    }
    if (SDL_CreateWindowAndRenderer(gif->width, gif->height, SDL_WINDOW_RESIZABLE, &window, &renderer)) {
        SDL_Log("Couldn't create window and renderer: %s", SDL_GetError());
        return 1;
    }
    snprintf(title, sizeof(title) - 1, "GIF %dx%d %d colors",
             gif->width, gif->height, gif->palette->size);
    SDL_SetWindowTitle(window, title);
    color = &gif->gct.colors[gif->bgindex * 3];
    SDL_SetRenderDrawColor(renderer, color[0], color[1], color[2], 0x00);
    SDL_RenderClear(renderer);
    SDL_RenderPresent(renderer);
    surface = SDL_CreateRGBSurface(0, gif->width, gif->height, 32, 0, 0, 0, 0);
    if (!surface) {
        SDL_Log("SDL_CreateRGBSurface() failed: %s", SDL_GetError());
        return 1;
    }
    paused = 0;
    quit = 0;
    while (1) {
        while (SDL_PollEvent(&event) && !quit) {
            if (event.type == SDL_QUIT)
                quit = 1;
            if (event.type == SDL_KEYDOWN) {
                if (event.key.keysym.sym == SDLK_q)
                    quit = 1;
                else if (event.key.keysym.sym == SDLK_SPACE)
                    paused = !paused;
            }
        }
        if (quit) break;
        if (paused) {
            SDL_Delay(10);
            continue;
        }
        t0 = SDL_GetTicks();
        ret = gd_get_frame(gif);
        if (ret == -1)
            break;
        SDL_LockSurface(surface);
        gd_render_frame(gif, frame);
        color = frame;
        for (i = 0; i < gif->height; i++) {
            for (j = 0; j < gif->width; j++) {
                if (!gd_is_bgcolor(gif, color))
                    pixel = SDL_MapRGB(surface->format, color[0], color[1], color[2]);
                else if (((i >> 2) + (j >> 2)) & 1)
                    pixel = SDL_MapRGB(surface->format, 0x7F, 0x7F, 0x7F);
                else
                    pixel = SDL_MapRGB(surface->format, 0x00, 0x00, 0x00);
                addr = surface->pixels + (i * surface->pitch + j * sizeof(pixel));
                memcpy(addr, &pixel, sizeof(pixel));
                color += 3;
            }
        }
        SDL_UnlockSurface(surface);
        texture = SDL_CreateTextureFromSurface(renderer, surface);
        SDL_RenderCopy(renderer, texture, NULL, NULL);
        SDL_RenderPresent(renderer);
        SDL_DestroyTexture(texture);
        t1 = SDL_GetTicks();
        delta = t1 - t0;
        delay = gif->gce.delay * 10;
        delay = delay > delta ? delay - delta : 1;
        SDL_Delay(delay);
        if (ret == 0)
            gd_rewind(gif);
    }
    SDL_FreeSurface(surface);
    SDL_DestroyRenderer(renderer);
    SDL_DestroyWindow(window);
    SDL_Quit();
    free(frame);
    gd_close_gif(gif);
    return 0;
}
