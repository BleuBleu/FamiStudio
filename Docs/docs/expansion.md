# Using Expansion Audio

## Overview

Expansion audio refers to extra hardware that were present on some Famicom cartridges (or add-on in the case of the Famicom Disk System). These chips added a few extra audio channels on top of the standard 5 channels.

List of expansions supported:

* **Konami VRC6**: Adds 2 square channels and a Sawtooth channel. The square channels are better quality than the stock square as they give more control over the duty cycle and don't suffer for the phase reset bug.

* **Konami VRC7**: Adds 6 FM synthesis channels. It was based off the Yamaha YM2413 OPLL. Qualitatively, it sounds like an old SoundBlaster card from the 90's. It has 15 preset patches and a user-defined one. Since only 1 user-defined patch can be defined, one has to be careful not to use multiple custom patches at the same time from different channels.

* **Famicom Disk System**: Adds an extra channel that uses a wavetable and a modulation unit to create a wide range of sound.

* **Nintendo MMC5**: Adds 2 extra square channels. They are extremely similar to the regular square channels of the Famicom, and also suffer from the same phase reset issue. Unlike the stock square channels, the reset bug cannot be worked around.

* **Namco 163**: Adds up to 8 wavetable channels. It has 128 bytes of RAM to store all the currently used waveforms. Waveforms can be 4, 8, 16 and 32 in size. Smaller waveforms will sound worse, but you can have more of them at any given time. FamiStudio will work at its best with a sample size of 16. Also, it should be noted that for technical reasons, the more channels you add, the worse the audio quality will be. It is recommended that you use 4 channels or less, which is what most games using the expansion did.

* **Sunsoft 5B**: Add 3 extra square channels with shared noise & envelope capabilities. These channels are fixed at a duty cycle of 50%. It was based off the Yamaha YM2149F. While this expansion was extremely powerful, it was only ever used in one game (Gimmick!) and this game did not make use of any advanced features of the chip. For this reason most emulators only support a small subset of features.

* **EPSM**: EPSM is not an audio expansion that was ever used, which never even existed in the lifetime of the NES, but rather the pet project of [Perkka](https://github.com/Perkka2/EPSM) who designed a small circuit board that can be plugged in the expansion port of the NES and controlled by it to produce sound. The expansion is based off the Yamaha YMF288 chip and adds 3 extra square channels with shared noise capabilities, 6 FM synthesis channels and 6 rhythm channels. The square are fixed at a duty cycle or 50%. Those are essentially the same as Sunsoft S5B. The FM channels are 4-OP channels that can be configured independently. The rhythm channels are 6 pre-defined drum samples.

## Enabling expansion audio

The audio expansion(s) used is set in the project properties, which you can access by clicking on the gear next to the project name (first row in the Project Explorer).

![](images/EditProject.png#center)

Note that while using multiple audio expansion(s) is supported in FamiStudio, no NES game has ever shipped with multiple expansions. This probably would not have been possible for multiple reasons (cost, competing companies, hardware conflicts, etc.). So be aware that you are bending the rules of reality when using multiple expansions.

## Expansion instruments

Almost all expansion audio requires using special instruments on the expansion channels, the only exception being MMC5 because of its similarity with the regular square channels.

When you have an expansion enabled and try to create an instrument, you will be asked what kind of instrument you want to create.

![](images/CreateExpInstrument.png#center)

Certain expansion instrument have parameters that are very different from standard instrument. Some of these parameters are accessed by expanding the instrument by clicking on the little triangle on the left of its name.

# Expansion-Specific Information

This section will go into more detail for each audio expansion.

## VRC6

VRC6 have a single extra parameter:

* **Saw Master Volume** : The saw channel tends to be very loud and its volume ranges from 0 to 63 (although FamiStudio always work in the 0 to 15 range).
    * **Full** : Maps the 0-15 range to the full 0-63 range, very loud.
    * **Half** : Maps the 0-15 range to the full 0-31 range, probably the best compromise.
    * **Quarter** : Use the 0-15 values as is, quiet.

![](images/VRC6.png#center)

Note that VRC6 expansion can be used to export to the [Rainbow Mapper](https://github.com/BrokeStudio/rainbow-net) and used with the Sound Engine. For more information, please check out the [Sound Engine](soundengine.md) documentation.

## VRC7

VRC7 instruments are by far the ones with the most parameters of the old expansions.

![](images/VRC7.png#center)

Roughly speaking, the chip generates a carrier frequency, which is then modified by a modulator. To configure this properly, you should be familiar with the [Attack-Decay-Sustain-Release (ADSR)](https://en.wikipedia.org/wiki/Envelope_(music)#ADSR) way of generating sound.

* **Patch** : Allows you to select one of the built-in patch. Changing any parameter value will revert back to the "Custom" patch as it is the only one that can be configured.
* **Carrier** / **Modulator** :
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

## Famicom Disk System

FDS instrument features a single user-drawn waveform, of fixed length (64). The waveform can also be resampled from a WAV file, see the [resampling section](#resampling-wav-files-for-n163-and-fds) for more details.

The FDS instruments have 2 extra envelopes and a few extra parameter.

![](images/FDS.png#center)

The extra envelopes are:

* **Waveform** : The waveform of the instrument. It has a fixed length of 64 and can be fully customised. You can also choose from one of the presets.
* **Modulation Table** : The modulation table is used to modulate the waveform. It has a fixed length of 32 and can be fully customized. You can also choose from one of the presets. It has no effect if the modulation speed or depth is zero. Note that the way the modulation table is edited is very different from FamiTracker. All the internal complexity is hidden.

The parameters are:

* **Master Volume** : At full volume the FDS can be significantly louder than the regular channels. The chip also supported 40%, 50% and 66% which makes the sound a lot more balanced.
* **Wave Preset** : A few preset that you can use to set the waveform envelope.
* **Wave Count** : The number of waveforms the instrument will contain. The maximum amount is 16.
* **Mod Preset** : A few preset that you can use to set the modulation envelope.
* **Mod Speed** : The speed of the modulation.
* **Mod Depth** : The depth of the modulation.
* **Mod Delay** : A delay, in frames (1/60th of a second) before enabling modulation.
* **Auto-Mod** and **Numerator** / **Denominator** : Toggles Auto-Modulation and allow setting a numerator/denominator fraction. 
* **Resample Period**, **Offset** and **Normalize** : See the [resampling section](#resampling-wav-files-for-n163-and-fds)

When **Auto-Mod** is enabled, the instrument will automatically compute the modulation speed as a fraction of the note's pitch. For example setting Numerator = 1 and Denominator = 4 means the modulation speed will be 1/4 of the note's pitch. Since this process involves multiplications and divisions and the NES/Famicom is ill-equipped for this kind of task, it is highly recommend that you limit yourself to simple fractions such as 1/4 and avoid things like 13/27.

When changing the values of the sliders with huge values (such as the Mod Speed), you can hold Shift while dragging to fine tune the exact value.

## Namco 163

N163 instrument can have custom user-drawn waveforms. The size of the waveform is configurable, but limited by the amount of N163 RAM available, which decreases as more N163 channels are added. Waveforms can also be resampled from a WAV file, see the [resampling section](#resampling-wav-files-for-n163-and-fds) for more details.

Since the amount of RAM is limited, and there can be up to 8 channels playing at the same time, it is the user's responsibility to make sure waves do not overlap. For example, if 2 N163 channels are playing 2 different instruments at the same time, a sine wave and a triangle wave, they will have to occupy different RAM locations. Both cannot be a RAM position 0, for example. If using a wave size of 32, we could place one at offset 0, and the second at offset 32. 

The register viewer can be used to dianose wave overlap issues. Look for error message in the RAM diagram if you think there are issues.

![](images/N163Overlap.png#center)

FamiStudio also supports cycling through multiple waveform in time. All the waveforms are presented visually as a single massive waveform, and each individual waveform can be repeated for a number of frames by setting the value of the "Repeat" effect in the effect panel. Moreover, both Loop and Release points can be configured. The current maximum number of wave data is currently 1024 bytes per instrument. So a wave size of 32 will be limited to 32 individual waves, where as a wave size of 16 will be able to have twice that amount.

In the example below, 4 waveforms are setup and the number of repeats for each increases over time.

![](images/N163Repeat.png#center)

Here are the parameters available.

![](images/N163.png#center)

Extra envelope:

* **Waveform** : The waveform of the instrument. It will of a fixed length, as specified by the "Wave Size" parameter.

The parameters are:

* **Wave Preset** : A few preset that you can use to set the waveform envelope.
* **Wave Size** : The N163 chip had only 128 of RAM to store all the waveforms used at any given time. For example, if you use waveforms of size 32, you can only use 4 unique instrument at any given time. Using more will result in channels using wrong instruments.
* **Wave Auto-Position** : Let FamiStudio automatically position the waves to try to avoid overlap. When this is active, every time you press Play, FamiStudio will scan all songs and all notes and try to assign a position to all the N163 instruments that have this option enabled. Note that this can fail if using too many large waves. When the auto-assignment algorithm fails, you will get no error message and will need to use the register viewer to diagnose issues, as mentioned above. 
* **Wave Position** : The position, in the 128 bytes of memory, of the waveform. You must manually make sure that different waveforms don't overlap.
* **Wave Count** : The number of waveforms the instrument will contain. The maximum amount depends on the wave size.
* **Resample Period**, **Offset** and **Normalize** : See the [resampling section](#resampling-wav-files-for-n163-and-fds)

## Sunsoft 5B

The Sunsoft 5B expansion adds 3 square channels which can have noise and/or an envelope added to it. 

For each channel, each of those features can be enabled or not:

* **Tone** : If enabled, will produce a square wave with 50% duty cycle, other will output a flat value equal to the volume.
* **Noise** : If enabled, will add a noise which frequency is driven by the "Noise Frequency" envelope
* **Envelope** : If enabled, the volume will be entirely controlled by the envelope. You can choose various envelope shapes, some repeating and some non-repeating.

Tone and Noise are enabled in the Mixer envelope, where "N" means "Noise" and "T" means "Tone". Disabling both will output a flat value equal to the volume and will display a dash "-". 

![](images/S5BMixer.gif#center)

Envelope is set on the instrument itself and is considered enabled if it is set to anything other than "Off".

One important thing to understand about the noise and envelope features is that they are **shared across all 3 channels**. This mean, for example, that if an 2 instruments on different 5B channels both try to use different envelopes, they will fight against each other. The last channel will always dominate (channel 3 > channel 2 > channel 1). Also, even if 2 channels both use the same envelope with the same settings, the note attacks of either channels will reset the envelopes. 

For noise, the same rule applies when it comes to its frequency. If a channel has the noise feature enable it its mixer envelope, it will set the noise frequency for **all channels that uses noise**. The last channel to be updated will also have the last word.

Non repeating envelope can be used to simulate note attacks, while repeating envelopes can be used to make a crude bass instrument. Instruments can opt-in the **Auto-Pitch** feature which will set the frequency of the envelope relative to the frequency of the note being played. Since the hardware uses very few bits to encode the pitch, repeating envelopes will sound out of tune if you try to play high pitched notes. Instruments that dont opt-in the **Auto-Pitch** feature can control the pich manually on the instrument or with the **Env Period** effect track. 

![](images/S5B.png#center)

## EPSM

EPSM instruments are by far the ones with the most parameters.

![](images/EPSM.png#center)

Roughly speaking, the chip generates a carrier frequency, which is then modified by a modulator. To configure this properly, you should be familiar with the Attack-Decay-Sustain-Release (ADSR) way of generating sound.

* **Patch** : Allows you to select the FamiStudio default pre-defined patch, it will automatically change to custom when a parameter is changed.
* **Algorithm** : Configuration of how the different operators affect each-other.
* **Feedback** : Audio feedback to OP1.
* **Left** : Enable Left channel.
* **Right** : Enable Right channel.
* **AMS** : Amplitude modulation sensitivity.
* **PMS** : Period modulation sensitivity.
* **LF Oscillator EN** : Enable low frequency oscillator
* **LF Oscillator** : Low frequency oscillator rate
* **OPx** :
    * **Detune** : Detune is a parameter that gives cycle several gap slight in each slot as for frequency information made from F-Number.
    * **Frequency Ratio** : Multiplier on the frequency.
    * **Volume** : Operator volume.
    * **Key Scale** : Affects the speed of the ADSR envelope speed.
    * **Attack Rate** : Speed of the attack of note.
    * **Amplitude Modulation** : Enable amplitude modulation for operator.
    * **Decay Rate** : Speed of the decay to sustain level after attack.
    * **Sustain Rate** : Sustain rate is a speed that attenuates from sustain level
    * **Sustain Level** : Sustain level is the level (amount of attenuation) that changes from decay rate into sustain rate.
    * **Release Rate** : The release rate is Key off speed of the following attenuation.
    * **SSG Envelope EN** : Enable SSG Envelope.
    * **SSG Envelope** : Selection of SSG Envelope type.

EPSM squares work identically to the Sunsoft 5B square channels with the exception that repeating envelopes will stay in tune a couple of octave higher due to the much higher clock of EPSM.

# Resampling WAV files for N163 and FDS

Both expansions using wavetables (N163 and FDS) can import and resample short WAV files. To import a WAV file, simply **right-click** on the instrument and select the "Resample Wav File..." option. Only very short files can be loaded and they will be truncated if they are too long. 

Once a WAV file is loaded, the instrument will be set to the "Resample" profile which means its waveform is generated from the loaded WAV file. You can then adjust the "Offset" and "Period" to align the waveform to the boundaries of the wavetable, as you see fit. 

Optionally, the "Normalize" option will adjust the volume to use the full range by detecting the min/max volume in the input WAV file.

Any manual modification to the waveform will switch the instrument to the "Custom" profile and disable resampling entirely.

Pro-tip : You can hold **Ctrl** while using the sliders in the project explorer to scroll more accurately.

![](images/Resample.gif#center)
