# Using Expansion Audio

## Overview

Expansion audio refers to extra hardware that were present on some Famicom cartridges (or add-on in the case of the Famicom Disk System). These chips added a few extra audio channels on top of the standard 5 channels.

List of expansions supported:

* **Konami VRC6**: Adds 2 square channels and a Sawtooth channel. The square channels are better quality than the stock square as they give more control over the duty cycle and don't suffer for the phase reset bug. 

* **Konami VRC7**: Adds 6 FM synthesis channels. It was based off the Yamaha YM2413 OPLL. Qualitatively, it sounds like an old SoundBlaster card from the 90s. It has 15 preset patches and a user-defined one. Since only 1 user-defined patch can be defined, one has to be careful not to use multiple custom patches at the same time from different channels.

* **Famicom Disk System**: Adds an extra channel that uses a wavetable and a modulation unit to create a wide range of sound. 

* **Nintendo MMC5**: Adds 2 extra square channels. They are extremely similar to the regular square channels of the Famicom, and also suffer from the same phase reset issue. Unlike the stock square channels, the reset bug cannot be worked around.

* **Namco 163**: Adds up to 8 wavetable channels. It has 128 bytes of RAM to store all the currently used waveforms. Waveforms can be 4, 8, 16 and 32 in size. Smaller waveforms will sound worse, but you can have more of them at any given time. FamiStudio will work at its best with a sample size of 16. Also, it should be noted that for technical reasons, the more channels you add, the worse the audio quality will be. It is recommanded that you use 4 channels or less, which is what most games using the expansion did. 

* **Sunsoft S5B**: Add 3 extra square channels. These channels are fixed at a duty cycle or 50%. It was based off the Yamaha YM2149F. While this expansion was extremely powerful, it was only ever used in one game (Gimmick!) and this game did not make use of any advanced features of the chip. For this reason, FamiStudio (and most emulator) only support a small subset of features.

## Enabling expansion audio

The audio expansion(s) used is set in the project properties, which you can access by double-clicking on the project name (first row in the Project Explorer).

![](images/EditProject.png#center)

Enabling any kind of expansion audio will automatically disable PAL support since no audio expansion were ever available in PAL territories. 

Note that while using multiple audio expansion(s) is supported in FamiStudio, no NES game has ever shipped with multiple expansions. This probably would not have been possible for multiple reasons (cost, competing compagnies, hardware conflicts, etc.). So be aware that you are bending the rules of reality when using multiple expanions.

## Expansion instruments

Almost all expansion audio requires using special instruments on the expansion channels, the only exception being MMC5 because of its similarity with the regular square channels.

When you have an expansion enabled and try to create an instrument, you will be asked what time of instrument you want to create.

![](images/CreateExpInstrument.png#center)

Certain expansion instrument have parameters that are very different from standard instrument. Some of these parameters are accessed by expanding the instrument by clicking on the little triangle on the left of its name.

### VRC6

VRC6 have a single extra parameter: 

* **Saw Master Volume** : The saw channel tends to be very loud and its volume ranges from 0 to 63 (altough FamiStudio always work in the 0 to 15 range). 
    * **Full** : Maps the 0-15 range to the full 0-63 range, very loud.
    * **Half** : Maps the 0-15 range to the full 0-31 range, probably the best compromise.
    * **Quarter** : Use the 0-15 values as is, quiet.

![](images/VRC6.png#center)


### VRC7

VRC7 instruments are by far the ones with the most parameters.

![](images/VRC7.png#center)

Roughly speaking, the chip generates a carier frequency, which is then modified by a modulator. To configure this properly, you should be familiar with the Attack-Decay-Sustain-Release (ADSR) way of generating sound.

* **Patch** : Allows you to select one of the built-in patch. Changing any parameter value will revert back to the "Custom" patch as it is the only one that can be configured.
* **Carier** / **Modulator** : 
    * **Tremolo** : Toggle use of tremolo. The rate of the tremolo is fixed and cannot be changed.
    * **Vibrato** : Toggle use of vibrato. The rate of the vibrato is fixed and cannot be changed.
    * **Sustained** : Toggle use of sustain in the envelope.
    * **Wave Rectified** : If enabled, clips the waveform to zero.
    * **Key Scaling** : Toggle use of the KeyScaling Level parameter.
    * **KeyScaling Level** : Affects the speed of the ADSR envelope speed.
    * **FreqMultiplier** : Multiplier on the frequency.
    * **Attack** : Speed of the attack.
    * **Decay** : Speed of the decay.
    * **Sustain** : Attenuation after decay.
    * **Release** : Speed of release.
* **Modulator Level** : Output level of the modulator.
* **Feedback** : Feedback applied to the modulator.

### Famicom Disk System

The FDS instruments have 2 extra envelopes and a few extra parameter.

![](images/FDS.png#center)

The extra envelopes are:

* **Waveform** : The waveform of the instrument. It has a fixed length of 64 and can be fully customized. You can also choose from one of the presets.
* **Modulation Table** : The modulation table is used to modulate the waveform. It has a fixed length of 32 and can be fully customized. You can also choose from one of the presets. It has no effect if the modulation speed or depth is zero. Note that the way the modulation table is edited is very different from FamiTracker. All the internal complexity is hidden.

The parameters are:

* **Master Volume** : At full volume the FDS can be significantly louder than the regular channels. The chip also supported 40%, 50% and 66% which makes the sound a lot more balanced. 
* **Wave Preset** : A few preset that you can use to set the waveform envelope.
* **Mod Preset** : A few preset that you can use to set the modulation envelope.
* **Mod Speed** : The speed of the modulation.
* **Mod Depth** : The depth of the modulation.
* **Mod Delay** : A delay, in frames (1/60th of a second) before enabling modulation.

When changing the values of the sliders with huge values (such as the Mod Speed), you can hold Shift while dragging to fine tune the exact value.

### Namco 163

The N163 instruments have an extra envelope and a few extra parameter. Note that FamiStudio's current implementation of N163 only supports a single waveform per instrument, unlike FamiTracker which has many.

![](images/N163.png#center)

Extra envelope:

* **Waveform** : The waveform of the instrument. It will of a fixed length, as speficied by the "Wave Size" parameter.

The parameters are:

* **Wave Preset** : A few preset that you can use to set the waveform envelope.
* **Wave Size** : The N163 chip had only 128 of RAM to store all the waveforms used at any given time. For example, if you use waveforms of size 32, you can only use 4 unique instrument at any given time. Using more will result in channels using wrong instruments.
* **Wave Position** : The position, in the 128 bytes of memory, of the waveform. You must manually make sure that different waveforms dont overlap.


