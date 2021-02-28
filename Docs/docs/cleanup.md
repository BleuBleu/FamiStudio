# Cleaning-up Your Project

## Clean-up Dialog

The clean-up dialog dialog is accessed from the toolbar. It can perform various cleaning operations, such as deleting unused elements and merging identical ones.

## Cleaning-up Song

![](images/CleanupSong.png#center)

Here are the operations that can be performed on songs:

* **Merge identical patterns** : Patterns that are identical on the same channel will be assumed to be the same and will all be replaced by instances of a single one. To be identical, patterns must have the exact same notes, effects, etc.

* **Delete empty patterns** : Patterns with zero notes or effect values will be deleted. Only applies to the selected songs. 

## Cleaning-up Projects

![](images/CleanupProject.png#center)

Here are the operations that can be performed on the project:

* **Merge identical instruments** : Instruments that have the same settings and envelopes will be combined into ones and every notes using the intruments to be deleted will use the new one. 

* **Delete unused instruments** : Instruments without a single note played will be deleted.

* **Unassign unused DPCM instrument keys** : Unassign samples to keys of the DPCM instrument that are not used by any song in the project.

* **Delete unassigned samples** : Samples that are not assigned to any key of the DPCM instrument will be deleted.

* **Permanently apply all DPCM sample processing** : Remove all the source data of any DPCM sample and replaces it with its processed DMC data. This will also reset all the processing parameters. Only do this when you know you are completely done adjusting your samples, to reduce the file size (it you were using WAV files as source). This will not affect the sound of the song, but will severely limit your ability to go back and adjust things like volume, etc. in a lossless way.

* **Delete unused arpeggios** : Arpeggios that are not used by a single note will be deleted.
