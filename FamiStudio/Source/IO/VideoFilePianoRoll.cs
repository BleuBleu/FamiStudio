using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace FamiStudio
{
    class VideoFilePianoRoll : VideoFileBase
    {
        const int ChannelIconPosY = 26;
        const int SegmentTransitionNumFrames = 16;
        const int ThinNoteThreshold = 288;
        const int VeryThinNoteThreshold = 192;

        private void ComputeChannelsScroll(VideoFrameMetadata[] frames, long channelMask, int numVisibleNotes)
        {
            var numFrames = frames.Length;
            var numChannels = frames[0].channelData.Length;

            for (int c = 0; c < numChannels; c++)
            {
                if ((channelMask & (1L << c)) == 0)
                    continue;

                // Go through all the frames and split them in segments. 
                // A segment is a section of the song where all the notes fit in the view.
                var segments = new List<ScrollSegment>();
                var currentSegment = (ScrollSegment)null;
                var minOverallNote = int.MaxValue;
                var maxOverallNote = int.MinValue;

                for (int f = 0; f < numFrames; f++)
                {
                    var frame = frames[f];
                    var note  = frame.channelData[c].note;

                    if (note.IsMusical)
                    {
                        if (currentSegment == null)
                        {
                            currentSegment = new ScrollSegment();
                            segments.Add(currentSegment);
                        }

                        // If its the start of a new pattern and we've been not moving for ~10 sec, let's start a new segment.
                        bool forceNewSegment = frame.playNote == 0 && (f - currentSegment.startFrame) > 600;

                        var minNoteValue = note.Value - 1;
                        var maxNoteValue = note.Value + 1;

                        // Only consider slides if they arent too large.
                        if (note.IsSlideNote && Math.Abs(note.SlideNoteTarget - note.Value) < numVisibleNotes / 2)
                        {
                            minNoteValue = Math.Min(note.Value, note.SlideNoteTarget) - 1;
                            maxNoteValue = Math.Max(note.Value, note.SlideNoteTarget) + 1;
                        }

                        // Only consider arpeggios if they are not too big.
                        if (note.IsArpeggio && note.Arpeggio.GetChordMinMaxOffset(out var minArp, out var maxArp) && maxArp - minArp < numVisibleNotes / 2)
                        {
                            minNoteValue = note.Value + minArp;
                            maxNoteValue = note.Value + maxArp;
                        }

                        minOverallNote = Math.Min(minOverallNote, minNoteValue);
                        maxOverallNote = Math.Max(maxOverallNote, maxNoteValue);

                        var newMinNote = Math.Min(currentSegment.minNote, minNoteValue);
                        var newMaxNote = Math.Max(currentSegment.maxNote, maxNoteValue);

                        // If we cant fit the next note in the view, start a new segment.
                        if (forceNewSegment || newMaxNote - newMinNote + 1 > numVisibleNotes)
                        {
                            currentSegment.endFrame = f;
                            currentSegment = new ScrollSegment();
                            currentSegment.startFrame = f;
                            segments.Add(currentSegment);

                            currentSegment.minNote = minNoteValue;
                            currentSegment.maxNote = maxNoteValue;
                        }
                        else
                        {
                            currentSegment.minNote = newMinNote;
                            currentSegment.maxNote = newMaxNote;
                        }
                    }
                }

                // Not a single notes in this channel...
                if (currentSegment == null)
                {
                    currentSegment = new ScrollSegment();
                    currentSegment.minNote = Note.FromFriendlyName("C4");
                    currentSegment.maxNote = currentSegment.minNote;
                    segments.Add(currentSegment);
                }

                currentSegment.endFrame = numFrames;

                // Remove very small segments, these make the camera move too fast, looks bad.
                var shortestAllowedSegment = SegmentTransitionNumFrames * 2;

                bool removed = false;
                do
                {
                    var sortedSegment = new List<ScrollSegment>(segments);

                    sortedSegment.Sort((s1, s2) => s1.NumFrames.CompareTo(s2.NumFrames));

                    if (sortedSegment[0].NumFrames >= shortestAllowedSegment)
                        break;

                    for (int s = 0; s < sortedSegment.Count; s++)
                    {
                        var seg = sortedSegment[s];

                        if (seg.NumFrames >= shortestAllowedSegment)
                            break;

                        var thisSegmentIndex = segments.IndexOf(seg);

                        // Segment is too short, see if we can merge with previous/next one.
                        var mergeSegmentIndex  = -1;
                        var mergeSegmentLength = -1;
                        if (thisSegmentIndex > 0)
                        {
                            mergeSegmentIndex  = thisSegmentIndex - 1;
                            mergeSegmentLength = segments[thisSegmentIndex - 1].NumFrames;
                        }
                        if (thisSegmentIndex != segments.Count - 1 && segments[thisSegmentIndex + 1].NumFrames > mergeSegmentLength)
                        {
                            mergeSegmentIndex = thisSegmentIndex + 1;
                            mergeSegmentLength = segments[thisSegmentIndex + 1].NumFrames;
                        }
                        if (mergeSegmentIndex >= 0)
                        {
                            // Merge.
                            var mergeSeg = segments[mergeSegmentIndex];
                            mergeSeg.startFrame = Math.Min(mergeSeg.startFrame, seg.startFrame);
                            mergeSeg.endFrame   = Math.Max(mergeSeg.endFrame, seg.endFrame);
                            segments.RemoveAt(thisSegmentIndex);
                            removed = true;
                            break;
                        }
                    }
                }
                while (removed);

                // Build the actually scrolling data. 
                var minScroll = (float)Math.Ceiling(Note.MusicalNoteMin + numVisibleNotes * 0.5f);
                var maxScroll = (float)Math.Floor  (Note.MusicalNoteMax - numVisibleNotes * 0.5f);

                Debug.Assert(maxScroll >= minScroll);

                foreach (var segment in segments)
                {
                    segment.scroll = Utils.Clamp(segment.minNote + (segment.maxNote - segment.minNote) * 0.5f, minScroll, maxScroll);
                }

                for (var s = 0; s < segments.Count; s++)
                {
                    var segment0 = segments[s + 0];
                    var segment1 = s == segments.Count - 1 ? null : segments[s + 1];

                    for (int f = segment0.startFrame; f < segment0.endFrame - (segment1 == null ? 0 : SegmentTransitionNumFrames); f++)
                    {
                        frames[f].channelData[c].scroll = segment0.scroll;
                    }

                    if (segment1 != null)
                    {
                        // Smooth transition to next segment.
                        for (int f = segment0.endFrame - SegmentTransitionNumFrames, a = 0; f < segment0.endFrame; f++, a++)
                        {
                            var lerp = a / (float)SegmentTransitionNumFrames;
                            frames[f].channelData[c].scroll = Utils.Lerp(segment0.scroll, segment1.scroll, Utils.SmootherStep(lerp));
                        }
                    }
                }
            }
        }

        private void SmoothFamitrackerScrolling(VideoFrameMetadata[] frames)
        {
            var numFrames = frames.Length;

            for (int f = 0; f < numFrames; )
            {
                var thisFrame = frames[f];

                // Keep moving forward until we see that we have advanced by 1 row.
                int nf = f + 1;
                for (; nf < numFrames; nf++)
                {
                    var nextFrame = frames[nf];
                    if (nextFrame.playPattern != thisFrame.playPattern ||
                        nextFrame.playNote    != thisFrame.playNote)
                    {
                        break;
                    }
                }

                var numFramesSameNote = nf - f;

                // Smooth out movement linearly.
                for (int i = 0; i < numFramesSameNote; i++)
                {
                    frames[f + i].playNote += i / (float)numFramesSameNote;
                }

                f = nf;
            }
        }

        private void SmoothFamiStudioScrolling(VideoFrameMetadata[] frames, Song song)
        {
            var patternIndices = new int[frames.Length];
            var absoluteNoteIndices = new float[frames.Length];

            // Keep copy of original pattern/notes.
            for (int i = 0; i < frames.Length; i++)
            {
                patternIndices[i] = frames[i].playPattern;
                absoluteNoteIndices[i] = song.GetPatternStartAbsoluteNoteIndex(frames[i].playPattern, (int)frames[i].playNote);
            }

            // Do moving average to smooth the movement.
            for (int i = 0; i < frames.Length; i++)
            {
                var averageSize = (Utils.Max(song.GetPatternGroove(patternIndices[i])) + 1) / 2;

                averageSize = Math.Min(averageSize, i);
                averageSize = Math.Min(averageSize, absoluteNoteIndices.Length - i - 1);

                var sum = 0.0f;
                var cnt = 0;
                for (int j = i - averageSize; j <= i + averageSize; j++)
                {
                    if (j >= 0 && j < absoluteNoteIndices.Length)
                    {
                        sum += absoluteNoteIndices[j];
                        cnt++;
                    }
                }
                sum /= cnt;

                frames[i].playPattern = song.PatternIndexFromAbsoluteNoteIndex((int)sum);
                frames[i].playNote = sum - song.GetPatternStartAbsoluteNoteIndex(frames[i].playPattern);
            }
        }

        public unsafe bool Save(Project originalProject, int songId, int loopCount, int window, string filename, int resX, int resY, bool halfFrameRate, long channelMask, int audioDelay, int audioBitRate, int videoBitRate, float pianoRollZoom, bool stereo, float[] pan, bool[] emuTriggers)
        {
            if (!InitializeEncoder(originalProject, songId, loopCount, filename, resX, resY, halfFrameRate, window, channelMask, audioDelay, audioBitRate, videoBitRate, stereo, pan, emuTriggers))
                return false;

            var channelResXFloat = videoResX / (float)channelStates.Length;
            var channelResX = videoResY;
            var channelResY = (int)channelResXFloat;
            var longestChannelName = 0.0f;

            foreach (var state in channelStates)
            {
                state.graphics = OffscreenGraphics.Create(channelResX, channelResY, false);
                state.bitmap = videoGraphics.CreateBitmapFromOffscreenGraphics(state.graphics);

                // Measure the longest text.
                longestChannelName = Math.Max(longestChannelName, state.graphics.MeasureString(state.channelText, fonts.FontVeryLarge));
            }

            // Tweak some cosmetic stuff that depends on resolution.
            var smallChannelText = longestChannelName + 32 + ChannelIconTextSpacing > channelResY * 0.8f;
            var font = smallChannelText ? fonts.FontMedium : fonts.FontVeryLarge;
            var textOffsetY = smallChannelText ? 1 : 4;
            var pianoRollScaleX = Utils.Clamp(resY / 1080.0f, 0.6f, 0.9f);
            var pianoRollScaleY = channelResY < VeryThinNoteThreshold ? 0.5f : (channelResY < ThinNoteThreshold ? 0.667f : 1.0f);
            var channelLineWidth = channelResY < ThinNoteThreshold ? 3 : 5;
            var gradientSizeY = 256 * (videoResY / 1080.0f);

            LoadChannelIcons(!smallChannelText);

            // Setup piano roll and images.
            var pianoRoll = new PianoRoll();
            pianoRoll.OverrideGraphics(videoGraphics, fonts);
            pianoRoll.Move(0, 0, channelResX, channelResY);
            pianoRoll.StartVideoRecording(channelStates[0].graphics, song, pianoRollZoom, pianoRollScaleX, pianoRollScaleY, out var noteSizeY);

            // Build the scrolling data.
            var numVisibleNotes = (int)Math.Floor(channelResY / (float)noteSizeY);
            ComputeChannelsScroll(metadata, channelMask, numVisibleNotes);

            if (song.UsesFamiTrackerTempo)
                SmoothFamitrackerScrolling(metadata);
            else
                SmoothFamiStudioScrolling(metadata, song);
            
            return LaunchEncoderLoop((f) =>
            {
                var frame = metadata[f];

                // Render the piano rolls for each channels.
                foreach (var s in channelStates)
                {
                    var volume = frame.channelData[s.songChannelIndex].volume;
                    var note   = frame.channelData[s.songChannelIndex].note;

                    var color = Color.Transparent;

                    if (note.IsMusical)
                    {
                        color = Color.FromArgb(128 + volume * 127 / 15, note.Instrument != null ? note.Instrument.Color : Theme.LightGreyColor1);
                    }

                    s.graphics.BeginDrawFrame(new Rectangle(0, 0, channelResX, channelResY), Color.Black);
                    pianoRoll.RenderVideoFrame(s.graphics, s.channel.Index, frame.playPattern, frame.playNote, frame.channelData[s.songChannelIndex].scroll, note.Value, color);
                    s.graphics.EndDrawFrame();
                }

                // Render the full screen overlay.
                videoGraphics.BeginDrawFrame(new Rectangle(0, 0, videoResX, videoResY), Color.Black);
                videoGraphics.PushClipRegion(0, 0, videoResX, videoResY);

                var c = videoGraphics.BackgroundCommandList;
                var o = videoGraphics.DefaultCommandList;

                // Composite the channel renders.
                foreach (var s in channelStates)
                {
                    int channelPosX0 = (int)Math.Round(s.videoChannelIndex * channelResXFloat);
                    c.DrawBitmap(s.bitmap, channelPosX0, 0, s.bitmap.Size.Height, s.bitmap.Size.Width, 1.0f, 0, 0, 1, 1, true);
                }

                videoGraphics.PopClipRegion();

                // Gradient
                o.FillRectangleGradient(0, 0, videoResX, gradientSizeY, Color.Black, Color.Transparent, true, gradientSizeY);

                // Channel names + oscilloscope
                foreach (var s in channelStates)
                {
                    int channelPosX0 = (int)Math.Round((s.videoChannelIndex + 0) * channelResXFloat);
                    int channelPosX1 = (int)Math.Round((s.videoChannelIndex + 1) * channelResXFloat);

                    var channelNameSizeX = (int)videoGraphics.MeasureString(s.channelText, font);
                    var channelIconPosX = channelPosX0 + channelResY / 2 - (channelNameSizeX + s.icon.Size.Width + ChannelIconTextSpacing) / 2;

                    o.FillAndDrawRectangle(channelIconPosX, ChannelIconPosY, channelIconPosX + s.icon.Size.Width - 1, ChannelIconPosY + s.icon.Size.Height - 1, Theme.DarkGreyColor2, Theme.LightGreyColor1);
                    o.DrawBitmap(s.icon, channelIconPosX, ChannelIconPosY, 1, Theme.LightGreyColor1);
                    o.DrawText(s.channelText, font, channelIconPosX + s.icon.Size.Width + ChannelIconTextSpacing, ChannelIconPosY + textOffsetY, Theme.LightGreyColor1);

                    if (s.videoChannelIndex > 0)
                        o.DrawLine(channelPosX0, 0, channelPosX0, videoResY, Theme.BlackColor, channelLineWidth);

                    var oscMinY = (int)(ChannelIconPosY + s.icon.Size.Height + 10);
                    var oscMaxY = (int)(oscMinY + 100.0f * (resY / 1080.0f));

                    var oscilloscope = UpdateOscilloscope(s, f);

                    o.PushTransform(channelPosX0 + 10, (oscMinY + oscMaxY) / 2, channelPosX1 - channelPosX0 - 20, (oscMinY - oscMaxY) / 2);
                    o.DrawGeometry(oscilloscope, Theme.LightGreyColor1, 1, true, false);
                    o.PopTransform();

                    videoGraphics.ConditionalFlushCommandLists();
                }
            }, () =>
            {
                // Cleanup.
                pianoRoll.EndVideoRecording();

                foreach (var c in channelStates)
                {
                    c.bitmap.Dispose();
                    c.graphics.Dispose();
                }
            });
        }
    }

    class ScrollSegment
    {
        public int startFrame;
        public int endFrame;
        public int minNote = int.MaxValue;
        public int maxNote = int.MinValue;
        public int NumFrames => endFrame - startFrame;
        public float scroll;
    }
}
