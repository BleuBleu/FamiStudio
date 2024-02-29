/****************************************************************************

  emu2149.c -- YM2149/AY-3-8910 emulator by Mitsutaka Okazaki 2001-2022

  2001 04-28 : Version 1.00beta -- 1st Beta Release.
  2001 08-14 : Version 1.10
  2001 10-03 : Version 1.11     -- Added PSG_set_quality().
  2002 03-02 : Version 1.12     -- Removed PSG_init & PSG_close.
  2002 10-13 : Version 1.14     -- Fixed the envelope unit.
  2003 09-19 : Version 1.15     -- Added PSG_setMask and PSG_toggleMask
  2004 01-11 : Version 1.16     -- Fixed the envelope problem where the envelope 
                                   frequency register is written before key-on.
  2015 12-13 : Version 1.17     -- Changed own integer types to C99 stdint.h types.
  2016 09-06 : Version 1.20     -- Support per-channel output.
  2021 09-29 : Version 1.30     -- Fix some envelope generator problems (issue #2).
  2022-11-25 : Version 1.40     -- Fix the bug with the changing period register
                                   faster than frequency (issue #3).
                                -- Fix the confusing PSG_setVolumeMode type argument.
                                -- Rename PSG_set_rate to PSG_setRate.
                                -- Rename PSG_set_quality to PSG_setQuality.
                                -- Restore PSG_setClock that was removed on v1.20 for
                                   compatibility.
                                -- Add PSG_setClockDivider function.
                                -- Fix noise period at zero frequency.
  2022-11-26 : Version 1.41     -- Fix a problem with DC offset when muting a channel
                                   (issue #4).
                                -- Disable frequency limiter if the internal rate 
                                   converter is not active (i.e. quality == 0).
  
  Further modifications done by alexmush:
  
  2024-02-29 :      --  Fixed the noise generator 
                        (now accurate to real YM2149 and YM6630 hardware)
                    --  Shifted volume table & adjusted regular volume assignment for it
                        (now YM envelopes sound like intended)
                    --  Envelopes now *fully* reset on write to 0xD
                                

  References:
    psg.vhd        -- 2000 written by Kazuhiro Tsujikawa.
    s_fme7.c       -- 1999,2000 written by Mamiya (NEZplug).
    ay8910.c       -- 1998-2001 Author unknown (MAME).
    MSX-Datapack   -- 1991 ASCII Corp.
    AY-3-8910 data sheet
    YM2149 data sheet
    
*****************************************************************************/
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include "emu2149.h"

static uint32_t voltbl[2][32] = {
  /* 
    YM2149 - 32 steps
    source: emu2149 (shifted forwards by 1 by alexmush)
  */
  {0x00, 0x00, 0x01, 0x01, 0x02, 0x02, 0x03, 0x03, 0x04, 0x05, 0x06, 0x07,
   0x09, 0x0B, 0x0D, 0x0F,
   0x12, 0x16, 0x1A, 0x1F, 0x25, 0x2D, 0x35, 0x3F, 0x4C, 0x5A, 0x6A, 0x7F,
   0x97, 0xB4, 0xD6, 0xFF},
  /* 
    AY-3-8910 - 16 steps
    source: emu2149
  */
  {0x00, 0x00, 0x03, 0x03, 0x04, 0x04, 0x06, 0x06, 0x09, 0x09, 0x0D, 0x0D,
   0x12, 0x12, 0x1D, 0x1D,
   0x22, 0x22, 0x37, 0x37, 0x4D, 0x4D, 0x62, 0x62, 0x82, 0x82, 0xA6, 0xA6,
   0xD0, 0xD0, 0xFF, 0xFF}
};

static const uint8_t regmsk[16] = {
    0xff, 0x0f, 0xff, 0x0f, 0xff, 0x0f, 0x1f, 0x3f, 
    0x1f, 0x1f, 0x1f, 0xff, 0xff, 0x0f, 0xff, 0xff
};

#define GETA_BITS 24

static void
internal_refresh (PSG * psg)
{
  uint32_t f_master = psg->clk;

  if (psg->clk_div)
    f_master /= 2;

  if (psg->quality)
  {
    psg->base_incr = 1 << GETA_BITS;
    psg->realstep = f_master;
    psg->psgstep = psg->rate * 8;
    psg->psgtime = 0;
  }
  else
  {
    psg->base_incr = (uint32_t)((double)f_master * (1 << GETA_BITS) / 8 / psg->rate);
    psg->freq_limit = 0;
  }
}

void 
PSG_setClock(PSG *psg, uint32_t clock)
{
  if (psg->clk != clock) {
    psg->clk = clock;
    internal_refresh(psg);
  }
}

void 
PSG_setClockDivider(PSG *psg, uint8_t enable)
{
  if (psg->clk_div != enable) {
    psg->clk_div = enable;  
    internal_refresh (psg);  }
}

void
PSG_setRate (PSG * psg, uint32_t rate)
{
  uint32_t r = rate ? rate : 44100;
  if (psg->rate != r) {
    psg->rate = r;
    internal_refresh (psg);
  }
}

void
PSG_setQuality (PSG * psg, uint8_t q)
{
  if (psg->quality != q) {
    psg->quality = q;
    internal_refresh(psg);
  }
}

PSG *
PSG_new (uint32_t clock, uint32_t rate)
{
  PSG *psg;

  psg = (PSG *) calloc (1, sizeof (PSG));
  if (psg == NULL)
    return NULL;

  PSG_setVolumeMode(psg, 0);
  psg->clk = clock;
  psg->clk_div = 0;
  psg->rate = rate ? rate : 44100;
  psg->quality = 0;
  internal_refresh(psg);
  PSG_setMask(psg, 0x00);
  return psg;
}

void
PSG_setVolumeMode (PSG * psg, int type)
{
  switch (type)
  {
  case 1:
    psg->voltbl = voltbl[0]; /* YM2149 */
    break;
  case 2:
    psg->voltbl = voltbl[1]; /* AY-3-8910 */
    break;
  default:
    psg->voltbl = voltbl[0]; /* fallback: YM2149 */
    break;
  }
}

uint32_t
PSG_setMask (PSG *psg, uint32_t mask)
{
  uint32_t ret = 0;
  if(psg)
  {
    ret = psg->mask;
    psg->mask = mask;
  }  
  return ret;
}

uint32_t
PSG_toggleMask (PSG *psg, uint32_t mask)
{
  uint32_t ret = 0;
  if(psg)
  {
    ret = psg->mask;
    psg->mask ^= mask;
  }
  return ret;
}

void
PSG_reset (PSG * psg)
{
  int i;

  psg->base_count = 0;

  for (i = 0; i < 3; i++)
  {
    psg->count[i] = 0;
    psg->freq[i] = 0;
    psg->edge[i] = 0;
    psg->volume[i] = 0;
    psg->ch_out[i] = 0;
  }

  psg->mask = 0;

  for (i = 0; i < 16; i++)
    psg->reg[i] = 0;
  psg->adr = 0;

  psg->noise_scaler = 0;
  psg->noise_seed = 1;
  psg->noise_count = 0;
  psg->noise_freq = 0;

  psg->env_ptr = 0;
  psg->env_freq = 0;
  psg->env_count = 0;
  psg->env_pause = 1;

  psg->out = 0;

}

void
PSG_delete (PSG * psg)
{
  free (psg);
}

uint8_t
PSG_readIO (PSG * psg)
{
  return (uint8_t) (psg->reg[psg->adr]);
}

uint8_t
PSG_readReg (PSG * psg, uint32_t reg)
{
  return (uint8_t) (psg->reg[reg & 0x1f]);

}

void
PSG_writeIO (PSG * psg, uint32_t adr, uint32_t val)
{
  if (adr & 1)
    PSG_writeReg (psg, psg->adr, val);
  else
    psg->adr = val & 0x1f;
}

static inline void
update_output (PSG * psg)
{

  int i, noise;
  uint8_t incr;

  psg->base_count += psg->base_incr;
  incr = (psg->base_count >> GETA_BITS);
  psg->base_count &= (1 << GETA_BITS) - 1;

  /* Envelope */
  psg->env_count += incr;


  if (psg->env_count >= psg->env_freq)
  {
    if (!psg->env_pause)
    {
      if(psg->env_face)
        psg->env_ptr = (psg->env_ptr + 1) & 0x3f ; 
      else
        psg->env_ptr = (psg->env_ptr + 0x3f) & 0x3f;
    }

    if (psg->env_ptr & 0x20) /* if carry or borrow */
    {
      if (psg->env_continue)
      {
        if (psg->env_alternate^psg->env_hold) psg->env_face ^= 1;
        if (psg->env_hold) psg->env_pause = 1;
        psg->env_ptr = psg->env_face ? 0 : 0x1f;       
      }
      else
      {
        psg->env_pause = 1;
        psg->env_ptr = 0;
      }
    }

    if (psg->env_freq >= incr) 
      psg->env_count -= psg->env_freq;
    else
      psg->env_count = 0;
  }

  /* Noise */
  psg->noise_count += incr;
  if (psg->noise_count >= psg->noise_freq)
  {
    psg->noise_scaler ^= 1;
    if (psg->noise_scaler) 
    { 
      if ((psg->noise_seed ^ (psg->noise_seed >> 3)) & 1)
        psg->noise_seed |= 1<<17;
      psg->noise_seed >>= 1;
    }

    if (psg->noise_freq >= incr)
      psg->noise_count -= psg->noise_freq;
    else
      psg->noise_count = 0;
  }
  noise = ~psg->noise_seed & 1;

  /* Tone */
  for (i = 0; i < 3; i++)
  {
    psg->count[i] += incr;
    if (psg->count[i] >= psg->freq[i])
    {
      psg->edge[i] = !psg->edge[i];

      if (psg->freq[i] >= incr) 
        psg->count[i] -= psg->freq[i];
      else
      {
        psg->count[i] = 0;
      }
      if (psg->edge[i])
        psg->trigger_mask |= (1 << i);
    }

    if (0 < psg->freq_limit && psg->freq[i] <= psg->freq_limit) {
      /* Mute the channel if the pitch is higher than the Nyquist frequency at the current sample rate, 
       * to prevent aliased or broken tones from being generated. Of course, this logic doesn't exist 
       * on the actual chip, but practically all tones higher than the Nyquist frequency are usually 
       * removed by a low-pass circuit somewhere, so we here halt the output. */
      continue;
    }

    
    if (psg->mask & PSG_MASK_CH(i)) 
    {
      psg->ch_out[i] = 0;
      continue;
    }

    if ((psg->tmask[i]||psg->edge[i]) && (psg->nmask[i]||noise))
    {
      if (!(psg->volume[i] & 32)) 
        psg->ch_out[i] = (psg->voltbl[psg->volume[i] & 31] << 4);
      else 
        psg->ch_out[i] = (psg->voltbl[psg->env_ptr] << 4);
    }
    else 
    {
      psg->ch_out[i] = 0;
    }
  }
}

static inline int16_t 
mix_output(PSG *psg) 
{
  return (int16_t)(psg->ch_out[0] + psg->ch_out[1] + psg->ch_out[2]);
}

int16_t
PSG_calc (PSG * psg)
{
  psg->trigger_mask = 0;

  if (!psg->quality)
  {
    update_output(psg);
    psg->out = mix_output(psg);
  }

  /* Simple rate converter */
  else
  {
    /* Simple rate converter (See README for detail). */
    while (psg->realstep > psg->psgtime)
    {
      psg->psgtime += psg->psgstep;
      update_output(psg);
      psg->out += mix_output(psg);
      psg->out >>= 1;
    }
    psg->psgtime -= psg->realstep;
  }

  return psg->out;
}

void
PSG_writeReg (PSG * psg, uint32_t reg, uint32_t val)
{
  int c;

  if (reg > 15) return;

  val &= regmsk[reg];

  psg->reg[reg] = (uint8_t) val;
  switch (reg)
  {
  case 0:
  case 2:
  case 4:
  case 1:
  case 3:
  case 5:
    c = reg >> 1;
    psg->freq[c] = ((psg->reg[c * 2 + 1] & 15) << 8) + psg->reg[c * 2];
    break;

  case 6:
    psg->noise_freq = val & 31;
    break;

  case 7:
    psg->tmask[0] = (val & 1);
    psg->tmask[1] = (val & 2);
    psg->tmask[2] = (val & 4);
    psg->nmask[0] = (val & 8);
    psg->nmask[1] = (val & 16);
    psg->nmask[2] = (val & 32);
    break;

  case 8:
  case 9:
  case 10:
    psg->volume[reg - 8] = (val << 1) | 1;
    break;
 
  case 11:
  case 12:
    psg->env_freq = (psg->reg[12] << 8) + psg->reg[11];
    break;

  case 13:
    psg->env_continue = (val >> 3) & 1;
    psg->env_attack = (val >> 2) & 1;
    psg->env_alternate = (val >> 1) & 1;
    psg->env_hold = val & 1;
    psg->env_face = psg->env_attack;
    psg->env_pause = 0;
    psg->env_count = 0;
    psg->env_ptr = psg->env_face ? 0 : 0x1f;
    break;

  case 14:
  case 15:
  default:
    break;
  }

  return;
}