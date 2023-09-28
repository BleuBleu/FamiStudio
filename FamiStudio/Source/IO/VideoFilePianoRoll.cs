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

            var numCols = separateChannels ? Math.Min(channelStates.Length, Utils.DivideAndRoundUp(channelStates.Length, settings.PianoRollNumRows)) : 1;
            var numRows = separateChannels ? Utils.DivideAndRoundUp(channelStates.Length, numCols) : 1;
            var channelSizeXFloat = videoResX / (float)numCols;
            var channelSizeYFloat = videoResY / (float)numRows;
            var channelSizeX = (int)channelSizeXFloat;
            var channelSizeY = (int)channelSizeYFloat;

            var cosPerspectiveAngle  = MathF.Cos(Utils.DegreesToRadians(settings.PianoRollPerspective));
            var numPerspectivePixels = (int)((1.0f - cosPerspectiveAngle) * channelSizeY);
            var renderSizeX = channelSizeX + numPerspectivePixels * 2;
            var renderSizeY = (int)(channelSizeY / cosPerspectiveAngle);

            var pianoRollGraphics = OffscreenGraphics.Create(renderSizeY, renderSizeX, false); // Piano roll is 90 degrees rotated.
            var pianoRollTexture  = pianoRollGraphics.GetTexture();

            // Render 3D perspective with 2x SSAA to reduce aliasing in the distance.
            var perspective2xGraphics = settings.PianoRollPerspective > 0 ? OffscreenGraphics.Create(channelSizeX * 2, channelSizeY * 2, false) : null;
            var perspective2xTexture  = settings.PianoRollPerspective > 0 ? perspective2xGraphics.GetTexture() : null;
            var perspective1xGraphics = settings.PianoRollPerspective > 0 ? OffscreenGraphics.Create(channelSizeX * 1, channelSizeY * 1, false) : null;
            var perspective1xTexture  = settings.PianoRollPerspective > 0 ? perspective1xGraphics.GetTexture() : null;

            var longestChannelName = 0.0f;
            foreach (var state in channelStates)
                longestChannelName = Math.Max(longestChannelName, videoGraphics.MeasureString(state.channelText, fonts.FontVeryLarge));

            // Tweak some cosmetic stuff that depends on resolution.
            var smallChannelText = true; // MATTT longestChannelName + 32 + ChannelIconTextSpacing > renderSizeY * 0.8f;
            var font = smallChannelText ? fonts.FontMedium : fonts.FontVeryLarge;
            var textOffsetY = smallChannelText ? 1 : 4;
            //var pianoRollScaleX = Utils.Clamp(settings.ResY / 1080.0f, 0.6f, 0.9f);
            //var pianoRollScaleY = renderSizeY < VeryThinNoteThreshold ? 0.5f : (renderSizeY < ThinNoteThreshold ? 0.667f : 1.0f);
            var channelLineWidth = 3; // renderSizeY < ThinNoteThreshold ? 3 : 5; // MATTT : This is wrong.
            var gradientSizeY = 256 * (videoResY / 1080.0f) / numRows;

            LoadChannelIcons(!smallChannelText);

            var channelHeaderSizeXFloat = separateChannels ? channelSizeX : videoResX / (float)channelStates.Length;

            //var oscMinY = (int)(ChannelIconPosY + channelStates[0].icon.Size.Height + 10);
            //var oscMaxY = (int)(oscMinY + 100.0f * (videoResY / 1080.0f));
            //registerPosY = oscMaxY + 4;

            var highlightedKeys = new ValueTuple<int, Color>[channelStates.Length];

            // Setup piano roll and images.
            var noteSizeY = 0;
            var pianoRoll = new PianoRoll();
            pianoRoll.OverrideGraphics(videoGraphics, fonts);
            pianoRoll.Move(0, 0, renderSizeY, renderSizeX); // Piano roll is 90 degrees rotated.
            pianoRoll.StartVideoRecording(song, settings.PianoRollZoom, 1.0f, 1.0f, out noteSizeY); // MATTT Figure out scale X/Y

            //if (separateChannels)
            //{
            //    pianoRoll.StartVideoRecording(song, 0, settings.PianoRollZoom, pianoRollScaleX, pianoRollScaleY, out noteSizeY);
            //}
            //else
            //{
            //}

            // Build the scrolling data.
            // MATTT Figure out how that's going to work.
            var numVisibleNotes = (int)Math.Floor(renderSizeY / (float)noteSizeY);
            ComputeChannelsScroll(metadata, settings.ChannelMask, numVisibleNotes);

            if (song.UsesFamiTrackerTempo)
                SmoothFamitrackerScrolling(metadata);
            else
                SmoothFamiStudioScrolling(metadata, song);

            var RenderPianoRollAndComposite = (VideoFrameMetadata frame, int idx, (int, Color)[] keys) =>
            {
                pianoRollGraphics.BeginDrawFrame(new Rectangle(0, 0, renderSizeY, renderSizeX), true, Theme.DarkGreyColor2);
                pianoRoll.RenderVideoFrame(pianoRollGraphics, channelStates[idx].songChannelIndex, separateChannels ? 0 : settings.ChannelMask, frame.playPattern, frame.playNote, frame.channelData[idx].scroll, keys);
                pianoRollGraphics.EndDrawFrame(true);

                if (settings.PianoRollPerspective > 0)
                {
                    perspective2xGraphics.BeginDrawFrame(new Rectangle(0, 0, channelSizeX * 2, channelSizeY * 2), true, Theme.DarkGreyColor2);
                    perspective2xGraphics.DefaultCommandList.DrawBitmap(pianoRollTexture, -numPerspectivePixels * 2, 0, renderSizeX * 2, channelSizeY * 2, 1.0f, 0, 0, 1, 1, BitmapFlags.Perspective2x | BitmapFlags.Rotated90);
                    perspective2xGraphics.EndDrawFrame();

                    perspective1xGraphics.BeginDrawFrame(new Rectangle(0, 0, channelSizeX, channelSizeY), true, Theme.DarkGreyColor2);
                    perspective1xGraphics.DefaultCommandList.DrawBitmap(perspective2xTexture, 0, 0, channelSizeX, channelSizeY, 1.0f, 0, 1, 1, 0);
                    perspective1xGraphics.EndDrawFrame();
                }

                var channelPosX = (int)MathF.Round((idx % numCols) * channelSizeXFloat);
                var channelPosY = (int)MathF.Round((idx / numCols) * channelSizeYFloat);

                videoGraphics.BeginDrawFrame(new Rectangle(0, 0, videoResX, videoResY), idx == 0, Color.Black); // MATTT : Correct position

                if (settings.PianoRollPerspective > 0)
                {
                    videoGraphics.DrawBlur(perspective1xTexture.Id, channelPosX, channelPosY, channelSizeX, channelSizeY);
                }
                else
                {
                    videoGraphics.PushClipRegion(channelPosX, channelPosY, channelSizeX, channelSizeY);
                    videoGraphics.DefaultCommandList.DrawBitmap(pianoRollTexture, channelPosX, channelPosY, channelSizeX, channelSizeY, 1, 0, 0, 1, 1, BitmapFlags.Rotated90);
                    videoGraphics.PopClipRegion();
                }

                videoGraphics.EndDrawFrame(false);
            };

            return LaunchEncoderLoop((f) =>
            {
                var frame = metadata[f];

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
                    for (var i = 0; i < channelStates.Length; i++)
                        RenderPianoRollAndComposite(frame, i, new[] { highlightedKeys[i] });
                }
                else
                {
                    RenderPianoRollAndComposite(frame, 0, highlightedKeys);
                }

                var c = videoGraphics.DefaultCommandList;
                var o = videoGraphics.OverlayCommandList;

                videoGraphics.BeginDrawFrame(new Rectangle(0, 0, videoResX, videoResY), false, Color.Black);
                //videoGraphics.PopClipRegion();

                // Gradients + grid lines.
                for (var i = 0; i < numRows; i++)
                {
                    var by = i * channelSizeY;
                    c.FillRectangleGradient(0, by, videoResX, by + gradientSizeY, Color.Black, Color.Transparent, true, gradientSizeY);

                    if (i > 0)
                        o.DrawLine(0, by, videoResX, by, Theme.BlackColor, channelLineWidth);

                    for (var j = 1; j < numCols; j++)
                    {
                        var bx = j * channelSizeX;
                        o.DrawLine(bx, 0, bx, videoResY, Theme.BlackColor, channelLineWidth);
                    }
                }

                // Channel names + oscilloscope
                for (int i = 0; i < channelStates.Length; i++)
                {
                    var s = channelStates[i];

                    var channelPosX = separateChannels ? (int)MathF.Round((i % numCols) * channelSizeXFloat) : i * channelHeaderSizeXFloat;
                    var channelPosY = separateChannels ? (int)MathF.Round((i / numCols) * channelSizeYFloat) : 0;
                    var channelNameSizeX = (int)videoGraphics.MeasureString(s.channelText, font);
                    var channelIconPosX = (int)channelHeaderSizeXFloat / 2 - (channelNameSizeX + s.icon.Size.Width + ChannelIconTextSpacing) / 2;
                    var oscilloscope = UpdateOscilloscope(s, f);

                    o.PushTranslation(channelPosX, channelPosY + ChannelIconPosY);
                    o.FillAndDrawRectangle(channelIconPosX, 0, channelIconPosX + s.icon.Size.Width - 1, s.icon.Size.Height - 1, Theme.DarkGreyColor2, Theme.LightGreyColor1);
                    o.DrawBitmap(s.icon, channelIconPosX, 0, 1, Theme.LightGreyColor1);
                    o.DrawText(s.channelText, font, channelIconPosX + s.icon.Size.Width + ChannelIconTextSpacing, textOffsetY, Theme.LightGreyColor1);
                    o.PushTransform(10, 50 /*(oscMinY + oscMaxY) / 2*/, channelHeaderSizeXFloat - 20, 50 /*(oscMinY - oscMaxY) / 2*/); // MATTT : Hardcoded values!!!
                    o.DrawNiceSmoothLine(oscilloscope, frame.channelData[i].color, settings.OscLineThickness);
                    o.PopTransform();
                    o.PopTransform();
                }
            }, () =>
            {
                // Cleanup.
                pianoRoll.EndVideoRecording();

                Utils.DisposeAndNullify(ref pianoRollGraphics);
                Utils.DisposeAndNullify(ref perspective2xGraphics);
                Utils.DisposeAndNullify(ref perspective1xGraphics);
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
