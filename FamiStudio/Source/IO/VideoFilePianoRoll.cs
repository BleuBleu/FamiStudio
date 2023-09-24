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

                foreach (var segment in segments)
                {
                    segment.scroll = segment.minNote + (segment.maxNote - segment.minNote) * 0.5f;
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

        public unsafe bool Save(VideoExportSettings settings)
        {
            if (!InitializeEncoder(settings))
                return false;

            // MATTT : Clean those up. Its a mess between separate and unified.
            var separateChannels = settings.VideoMode == VideoMode.PianoRollSeparateChannels;
            var channelResXFloat = videoResX / (float)channelStates.Length;
            var channelResX = videoResY;
            var channelResY = (int)channelResXFloat;
            var longestChannelName = 0.0f;

            var cosPerspectiveAngle = MathF.Cos(Utils.DegreesToRadians(settings.PianoRollPerspective));
            var numPerspectivePixels = (int)((1.0f - cosPerspectiveAngle) * videoResY);

            var unifiedPianoRollGraphics   = (OffscreenGraphics)null;
            var unifiedPerspectiveGraphics = (OffscreenGraphics)null;
            var unifiedPianoRollTexture    = (Bitmap)null;
            var unifiedPerspectiveTexture  = (Bitmap)null;

            if (!separateChannels)
            {
                channelResX = (int)(videoResY / cosPerspectiveAngle);
                channelResY = videoResX + numPerspectivePixels * 2;

                unifiedPianoRollGraphics = OffscreenGraphics.Create(channelResX, channelResY, false);
                unifiedPianoRollTexture = unifiedPianoRollGraphics.GetTexture();
                unifiedPerspectiveGraphics = OffscreenGraphics.Create(videoResX, videoResY, false);
                unifiedPerspectiveTexture = unifiedPerspectiveGraphics.GetTexture();
            }

            foreach (var state in channelStates)
            {
                if (separateChannels)
                {
                    state.graphics = OffscreenGraphics.Create(channelResX, channelResY, false);
                    state.bitmap = state.graphics.GetTexture();
                }
                else
                {
                    state.graphics = videoGraphics;
                }

                // Measure the longest text.
                longestChannelName = Math.Max(longestChannelName, videoGraphics.MeasureString(state.channelText, fonts.FontVeryLarge));
            }

            // Tweak some cosmetic stuff that depends on resolution.
            var smallChannelText = longestChannelName + 32 + ChannelIconTextSpacing > channelResY * 0.8f;
            var font = smallChannelText ? fonts.FontMedium : fonts.FontVeryLarge;
            var textOffsetY = smallChannelText ? 1 : 4;
            var pianoRollScaleX = Utils.Clamp(settings.ResY / 1080.0f, 0.6f, 0.9f);
            var pianoRollScaleY = channelResY < VeryThinNoteThreshold ? 0.5f : (channelResY < ThinNoteThreshold ? 0.667f : 1.0f);
            var channelLineWidth = channelResY < ThinNoteThreshold ? 3 : 5;
            var gradientSizeY = 256 * (videoResY / 1080.0f);
            
            LoadChannelIcons(!smallChannelText);

            var oscMinY = (int)(ChannelIconPosY + channelStates[0].icon.Size.Height + 10);
            var oscMaxY = (int)(oscMinY + 100.0f * (videoResY / 1080.0f));
            registerPosY = oscMaxY + 4;

            // Setup piano roll and images.
            var noteSizeY = 0;
            var pianoRoll = new PianoRoll();
            pianoRoll.OverrideGraphics(videoGraphics, fonts);
            pianoRoll.Move(0, 0, channelResX, channelResY);

            if (separateChannels)
            {
                pianoRoll.StartVideoRecording(song, 0, settings.PianoRollZoom, pianoRollScaleX, pianoRollScaleY, out noteSizeY);
            }
            else
            {
                pianoRoll.StartVideoRecording(song, settings.ChannelMask, settings.PianoRollZoom, 1.0f, 2.0f, out noteSizeY); // MATTT Scale X/Y
            }

            // Build the scrolling data.
            var numVisibleNotes = (int)Math.Floor(channelResY / (float)noteSizeY);
            ComputeChannelsScroll(metadata, settings.ChannelMask, numVisibleNotes);

            if (song.UsesFamiTrackerTempo)
                SmoothFamitrackerScrolling(metadata);
            else
                SmoothFamiStudioScrolling(metadata, song);
            
            return LaunchEncoderLoop((f) =>
            {
                var frame = metadata[f];

                var highlightedKeys = new ValueTuple<int, Color>[channelStates.Length];

                for (int i = 0; i < channelStates.Length; i++)
                { 
                    var s = channelStates[i];
                    var volume = frame.channelData[s.songChannelIndex].volume;
                    var note = frame.channelData[s.songChannelIndex].note;
                    var color = Color.Transparent;

                    if (note.IsMusical)
                    {
                        if (s.channel.Type == ChannelType.Dpcm && Settings.DpcmColorMode == Settings.ColorModeSample && note.Instrument != null)
                        {
                            var mapping = note.Instrument.GetDPCMMapping(note.Value);
                            if (mapping != null && mapping.Sample != null)
                                color = mapping.Sample.Color;
                        }
                        else
                        {
                            color = Color.FromArgb(128 + volume * 127 / 15, note.Instrument != null ? note.Instrument.Color : Theme.LightGreyColor1);
                        }
                    }

                    highlightedKeys[i] = (note.Value, color);
                }

                if (separateChannels)
                {
                    // Render the piano rolls for each channels.
                    for (int i = 0; i < channelStates.Length; i++)
                    {
                        var s = channelStates[i];

                        s.graphics.BeginDrawFrame(new Rectangle(0, 0, channelResX, channelResY), true, Theme.DarkGreyColor2);
                        pianoRoll.RenderVideoFrame(s.graphics, s.channel.Index, frame.playPattern, frame.playNote, frame.channelData[s.songChannelIndex].scroll, new[] { highlightedKeys[i] });
                        s.graphics.EndDrawFrame(true);
                    }
                }
                else
                {
                    unifiedPianoRollGraphics.BeginDrawFrame(new Rectangle(0, 0, channelResX, channelResY), true, Theme.DarkGreyColor2);
                    pianoRoll.RenderVideoFrame(unifiedPianoRollGraphics, 0 /*s.channel.Index*/, frame.playPattern, frame.playNote, frame.channelData[0 /*s.songChannelIndex*/].scroll, highlightedKeys);
                    unifiedPianoRollGraphics.EndDrawFrame(true);

                    unifiedPerspectiveGraphics.BeginDrawFrame(new Rectangle(0, 0, videoResX, videoResY), true, Theme.DarkGreyColor2);
                    //unifiedPerspectiveGraphics.DefaultCommandList.DrawBitmap(unifiedPianoRollTexture, -numTiltPixels, videoResY - channelResX, channelResY, channelResX, 1.0f, 0, 0, 1, 1, BitmapFlags.Rotated90); // MATTT
                    unifiedPerspectiveGraphics.DefaultCommandList.DrawBitmap(unifiedPianoRollTexture, -numPerspectivePixels, 0, channelResY, videoResY, 1.0f, 0, 0, 1, 1, BitmapFlags.Perspective | BitmapFlags.Rotated90);
                    unifiedPerspectiveGraphics.EndDrawFrame();
                }

                // Render the full screen overlay.
                videoGraphics.BeginDrawFrame(new Rectangle(0, 0, videoResX, videoResY), false, Color.Black);
                videoGraphics.PushClipRegion(0, 0, videoResX, videoResY);

                var c = videoGraphics.BackgroundCommandList;
                var o = videoGraphics.DefaultCommandList;

                if (separateChannels)
                {
                    // Composite the channel renders.
                    foreach (var s in channelStates)
                    {
                        int channelPosX0 = (int)Math.Round(s.videoChannelIndex * channelResXFloat);
                        c.DrawBitmap(s.bitmap, channelPosX0, 0, s.bitmap.Size.Height, s.bitmap.Size.Width, 1.0f, 0, 0, 1, 1, BitmapFlags.Rotated90);
                    }
                }
                else
                {
                    videoGraphics.DrawBlur(unifiedPerspectiveTexture.Id);
                }

                videoGraphics.PopClipRegion();

                // Gradient
                o.FillRectangleGradient(0, 0, videoResX, gradientSizeY, Color.Black, Color.Transparent, true, gradientSizeY);

                // Channel names + oscilloscope
                for (int i = 0; i < channelStates.Length; i++)
                {
                    var s = channelStates[i];

                    var channelPosX0 = (int)Math.Round((s.videoChannelIndex + 0) * channelResXFloat);
                    var channelPosX1 = (int)Math.Round((s.videoChannelIndex + 1) * channelResXFloat);
                    var channelNameSizeX = (int)videoGraphics.MeasureString(s.channelText, font);
                    var channelIconPosX = channelPosX0 + (int)channelResXFloat / 2 - (channelNameSizeX + s.icon.Size.Width + ChannelIconTextSpacing) / 2;

                    o.FillAndDrawRectangle(channelIconPosX, ChannelIconPosY, channelIconPosX + s.icon.Size.Width - 1, ChannelIconPosY + s.icon.Size.Height - 1, Theme.DarkGreyColor2, Theme.LightGreyColor1);
                    o.DrawBitmap(s.icon, channelIconPosX, ChannelIconPosY, 1, Theme.LightGreyColor1);
                    o.DrawText(s.channelText, font, channelIconPosX + s.icon.Size.Width + ChannelIconTextSpacing, ChannelIconPosY + textOffsetY, Theme.LightGreyColor1);

                    if (s.videoChannelIndex > 0 && separateChannels)
                        o.DrawLine(channelPosX0, 0, channelPosX0, videoResY, Theme.BlackColor, channelLineWidth);

                    var oscilloscope = UpdateOscilloscope(s, f);

                    o.PushTransform(channelPosX0 + 10, (oscMinY + oscMaxY) / 2, channelPosX1 - channelPosX0 - 20, (oscMinY - oscMaxY) / 2);
                    o.DrawNiceSmoothLine(oscilloscope, frame.channelData[i].color, settings.OscLineThickness);
                    o.PopTransform();
                }
            }, () =>
            {
                // Cleanup.
                pianoRoll.EndVideoRecording();

                if (settings.VideoMode == VideoMode.PianoRollSeparateChannels)
                {
                    foreach (var c in channelStates)
                    {
                        c.bitmap.Dispose();
                        c.graphics.Dispose();
                    }
                }

                Utils.DisposeAndNullify(ref unifiedPianoRollGraphics);
                Utils.DisposeAndNullify(ref unifiedPerspectiveGraphics);
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
