using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace FamiStudio
{
    class VideoFilePianoRoll : VideoFileBase
    {
        const int ChannelIconPosY = 24;
        const int SegmentTransitionNumFrames = 16;

        private void ComputeChannelsScroll(int channelIndex, VideoFrameMetadata[] frames, long channelMask, int numVisibleNotes, int[] channelTranspose)
        {
            var numFrames = frames.Length;
            var numChannels = frames[0].channelData.Length;

            // Go through all the frames and split them in segments. 
            // A segment is a section of the song where all the notes fit in the view.
            var segments = new List<ScrollSegment>();
            var currentSegment = (ScrollSegment)null;
            var minOverallNote = int.MaxValue;
            var maxOverallNote = int.MinValue;

            for (int f = 0; f < numFrames; f++)
            {
                var frame = frames[f];
                var minNoteValue = int.MaxValue;
                var maxNoteValue = int.MinValue;

                for (int c = 0; c < numChannels; c++)
                {
                    if ((channelMask & (1L << c)) == 0)
                        continue;

                    var note = frame.channelData[c].note;
                    var trans = channelTranspose[c];

                    if (note.IsMusical)
                    {
                        minNoteValue = Math.Min(note.Value + trans, minNoteValue);
                        maxNoteValue = Math.Max(note.Value + trans, maxNoteValue);

                        // Only consider slides if they arent too large.
                        if (note.IsSlideNote && Math.Abs(note.SlideNoteTarget - note.Value) < numVisibleNotes / 2)
                        {
                            minNoteValue = Math.Min(note.Value + trans, note.SlideNoteTarget + trans - 1);
                            maxNoteValue = Math.Max(note.Value + trans, note.SlideNoteTarget + trans + 1);
                        }

                        // Only consider arpeggios if they are not too big.
                        if (note.IsArpeggio && note.Arpeggio.GetChordMinMaxOffset(out var minArp, out var maxArp) && maxArp - minArp < numVisibleNotes / 2)
                        {
                            minNoteValue = Math.Min(note.Value + trans + minArp, minNoteValue);
                            maxNoteValue = Math.Max(note.Value + trans + maxArp, maxNoteValue);
                        }
                    }
                }

                if (minNoteValue == int.MaxValue)
                {
                    continue;
                }

                minOverallNote = Math.Min(minOverallNote, minNoteValue);
                maxOverallNote = Math.Max(maxOverallNote, maxNoteValue);

                if (currentSegment == null)
                {
                    currentSegment = new ScrollSegment();
                    segments.Add(currentSegment);
                }

                // If its the start of a new pattern and we've been not moving for ~10 sec, let's start a new segment.
                bool forceNewSegment = frame.playNote == 0 && (f - currentSegment.startFrame) > 600;

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
                    var mergeSegmentIndex = -1;
                    var mergeSegmentLength = -1;
                    if (thisSegmentIndex > 0)
                    {
                        mergeSegmentIndex = thisSegmentIndex - 1;
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
                        mergeSeg.endFrame = Math.Max(mergeSeg.endFrame, seg.endFrame);
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
                    frames[f].channelData[channelIndex].scroll = segment0.scroll;
                }

                if (segment1 != null)
                {
                    // Smooth transition to next segment.
                    for (int f = segment0.endFrame - SegmentTransitionNumFrames, a = 0; f < segment0.endFrame; f++, a++)
                    {
                        var lerp = a / (float)SegmentTransitionNumFrames;
                        frames[f].channelData[channelIndex].scroll = Utils.Lerp(segment0.scroll, segment1.scroll, Utils.SmootherStep(lerp));
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

        private float QuantizeNoteScaling(float scale)
        {
            return Utils.Clamp(MathF.Floor(scale * 4), 2, 8) / 4.0f; // [0.5, 2.0]
        }

        private void ComputeSongNoteMinMaxRange(long channelMask, int[] channelTranspose, out int minNote, out int maxNote)
        {
#if true
            var numFrames = metadata.Length;
            var numChannels = metadata[0].channelData.Length;
            var minNoteValue = int.MaxValue;
            var maxNoteValue = int.MinValue;

            for (var f = 0; f < numFrames; f++)
            {
                var frame = metadata[f];

                for (var c = 0; c < numChannels; c++)
                {
                    if ((channelMask & (1L << c)) == 0)
                        continue;

                    var note = frame.channelData[c].note;
                    var trans = channelTranspose[c];

                    if (note.IsMusical)
                    {
                        minNoteValue = Math.Min(note.Value + trans, minNoteValue);
                        maxNoteValue = Math.Max(note.Value + trans, maxNoteValue);

                        // Only consider slides if they arent too large.
                        if (note.IsSlideNote)
                        {
                            minNoteValue = Math.Min(minNoteValue, note.SlideNoteTarget + trans - 1);
                            maxNoteValue = Math.Max(maxNoteValue, note.SlideNoteTarget + trans + 1);
                        }

                        // Only consider arpeggios if they are not too big.
                        if (note.IsArpeggio && note.Arpeggio.GetChordMinMaxOffset(out var minArp, out var maxArp))
                        {
                            minNoteValue = Math.Min(note.Value + minArp + trans, minNoteValue);
                            maxNoteValue = Math.Max(note.Value + maxArp + trans, maxNoteValue);
                        }
                    }
                }
            }

            if (minNoteValue != int.MaxValue)
            {
                minNote = minNoteValue - 1;
                maxNote = maxNoteValue + 1;
            }
            else
            {
                minNote = Note.MusicalNoteC4;
                maxNote = Note.MusicalNoteC4;
            }

#else
            // Ignore the bottom/top 5%.
            const float fractionToIgnore = 0.05f;

            var numFrames = metadata.Length;
            var numChannels = metadata[0].channelData.Length;
            var histogram = new SortedDictionary<int, int>();
            var totalNoteCount = 0;

            for (int c = 0; c < numChannels; c++)
            {
                if ((channelMask & (1L << c)) == 0)
                    continue;

                for (int f = 0; f < numFrames; f++)
                {
                    var frame = metadata[f];
                    var note = frame.channelData[c].note;

                    if (note.IsMusical)
                    {
                        var key = note.Value + channelTranspose[c];
                        histogram.TryGetValue(key, out var cnt);
                        histogram[key] = cnt + 1;
                        totalNoteCount++;
                    }
                }
            }

            if (histogram.Count > 0)
            {
                var numNotesToIgnore = (int)(totalNoteCount * fractionToIgnore);
                var noteCount = 0;

                minNote = int.MaxValue;
                maxNote = int.MinValue;

                foreach (var kv in histogram)
                {
                    var numAboveHi = Math.Max(0, noteCount + kv.Value - totalNoteCount + numNotesToIgnore);
                    var numBelowLo = Math.Max(0, numNotesToIgnore - noteCount);
                    var numValid = Math.Max(0, kv.Value - numBelowLo - numAboveHi);

                    if (numValid > 0)
                    {
                        minNote = Math.Min(minNote, kv.Key);
                        maxNote = Math.Max(maxNote, kv.Key);
                    }

                    noteCount += kv.Value;
                }

                Debug.Assert(minNote != int.MaxValue);
                Debug.Assert(maxNote != int.MinValue);
            }
            else
            {
                minNote = Note.MusicalNoteC4;
                maxNote = Note.MusicalNoteC4;
            }
#endif
        }

        private void ComputeProjectionParams(float angle, int sizeX, int sizeY, out float u0, out float v0)
        {
            // This transform (rotate) and perspective-project the top-left corner 
            // of the image. Returns the 2D position in UV space.
            float tanHalfFov = MathF.Tan(Utils.DegreesToRadians(45.0f) * 0.5f);
            float aspectRatio = sizeX / (float)sizeY;

            var px = 1.0f;
            var py = 0.0f;
            Utils.RotatePoint2D(angle, ref px, ref py);
            
            var topWidth  = 1.0f / (1.0f + 2.0f * MathF.Sin(angle) / aspectRatio * tanHalfFov);
            var imgHeight = (((px * 2.0f - 1.0f) / (1.0f + (py * (2.0f / aspectRatio * tanHalfFov)))) * 0.5f + 0.5f);

            u0 = (1.0f - topWidth) * 0.5f;
            v0 = (1.0f - imgHeight);
        }

        public unsafe bool Save(VideoExportSettings settings)
        {
            if (!InitializeEncoder(settings))
                return false;

            var separateChannels = settings.VideoMode == VideoMode.PianoRollSeparateChannels;
            var perspective = settings.PianoRollPerspective > 0;

            var numCols = separateChannels ? Math.Min(channelStates.Length, Utils.DivideAndRoundUp(channelStates.Length, settings.PianoRollNumRows)) : 1;
            var numRows = separateChannels ? Utils.DivideAndRoundUp(channelStates.Length, numCols) : 1;
            var channelSizeXFloat = videoResX / (float)numCols;
            var channelSizeYFloat = videoResY / (float)numRows;
            var channelSizeX = (int)channelSizeXFloat;
            var channelSizeY = (int)channelSizeYFloat;

            var perspectiveAngle = Utils.DegreesToRadians(settings.PianoRollPerspective);
            ComputeProjectionParams(perspectiveAngle, channelSizeX, channelSizeY, out var u, out var v);

            var numPerspectivePixels = (int)(u * channelSizeX * 2);
            var renderSizeX = channelSizeX + numPerspectivePixels * 2;
            var renderSizeY = (int)(channelSizeY / (1.0f - v));

            var pianoRollGraphics = OffscreenGraphics.Create(renderSizeY, renderSizeX, false, perspective); // Piano roll is 90 degrees rotated.
            var pianoRollTexture  = pianoRollGraphics.GetTexture();

            // Render 3D perspective with 2x SSAA to reduce aliasing in the distance.
            var perspective2xGraphics = perspective ? OffscreenGraphics.Create(channelSizeX * 2, channelSizeY * 2, false, true) : null;
            var perspective2xTexture  = perspective ? perspective2xGraphics.GetTexture() : null;
            var perspective1xGraphics = perspective ? OffscreenGraphics.Create(channelSizeX * 1, channelSizeY * 1, false, true) : null;
            var perspective1xTexture  = perspective ? perspective1xGraphics.GetTexture() : null;

            var longestChannelName = 0.0f;
            foreach (var state in channelStates)
                longestChannelName = Math.Max(longestChannelName, videoGraphics.MeasureString(state.channelText, fonts.FontVeryLarge));

            // Tweak some cosmetic stuff that depends on resolution.
            var channelHeaderSizeXFloat = separateChannels ? channelSizeX : videoResX / (float)channelStates.Length;
            var smallChannelText = longestChannelName + 32 + ChannelIconTextSpacing > channelHeaderSizeXFloat * 0.8f;
            var font = smallChannelText ? fonts.FontMedium : fonts.FontVeryLarge;
            var textOffsetY = smallChannelText ? 1 : 4;
            var channelLineWidth = 3;
            var gradientSizeY = 256 * (videoResY / 1080.0f) / numRows;
            var blurScale = Utils.Clamp(MathF.Min(channelSizeX / 1080.0f, channelSizeY / 1080.0f) * 1.5f, 0.25f, 2.0f);

            LoadChannelIcons(!smallChannelText);

            // Avoid square aspect ratio for oscilloscope 3:1 is max.
            var oscSizeY = (int)Math.Min(channelHeaderSizeXFloat / 3, 100.0f * (channelSizeY / 1080.0f));
            var oscMinY = ChannelIconPosY * 2 + channelStates[0].icon.Size.Height;
            var oscMaxY = (oscMinY + oscSizeY);
            var oscPosY = (oscMinY + oscMaxY) / 2;
            var oscScaleY = (oscMaxY - oscMinY) / 2;
            var oscChannelPadX = separateChannels ? 0 : 5;

            registerPosY += oscMaxY;

            var highlightedKeys = new ValueTuple<int, Color>[channelStates.Length];

            // Compensate "a bit" for perspective.
            var pianoRollScaleX = channelSizeY / 1080.0f / (1.0f - (v * 0.5f)); 
            var pianoRollScaleY = 1.0f;

            if (settings.PianoRollNoteWidth == 0)
            {
                // Keep at least 2 octaves on screen by default.
                var numNotesToShow = 24;

                if (settings.VideoMode == VideoMode.PianoRollUnified)
                {
                    ComputeSongNoteMinMaxRange(settings.ChannelMask, settings.ChannelTranspose, out var minNote, out var maxNote);
                    numNotesToShow = (maxNote - minNote + 1);
                }

                pianoRollScaleY = QuantizeNoteScaling(channelSizeX / (float)numNotesToShow / PianoRoll.DefaultPianoKeyWidth);
            }
            else
            {
                pianoRollScaleY = settings.PianoRollNoteWidth;
            }

            var pianoRoll = new PianoRoll();
            pianoRoll.OverrideGraphics(videoGraphics, fonts);
            pianoRoll.Move(0, 0, renderSizeY, renderSizeX); // Piano roll is 90 degrees rotated.
            pianoRoll.StartVideoRecording(song, settings.PianoRollZoom, pianoRollScaleX, pianoRollScaleY, settings.ChannelTranspose);

            // Build the scrolling data.
            var noteSizeY = (int)(PianoRoll.DefaultPianoKeyWidth * pianoRollScaleY);
            var numVisibleNotes = (int)Math.Floor(channelSizeX / (float)noteSizeY);

            if (separateChannels)
            {
                for (var c = 0; c < metadata[0].channelData.Length; c++)
                    ComputeChannelsScroll(c, metadata, (1L << c) & settings.ChannelMask, numVisibleNotes, settings.ChannelTranspose);
            }
            else
            {
                ComputeChannelsScroll(channelStates[0].songChannelIndex, metadata, settings.ChannelMask, numVisibleNotes, settings.ChannelTranspose);
            }

            if (song.UsesFamiTrackerTempo)
                SmoothFamitrackerScrolling(metadata);
            else
                SmoothFamiStudioScrolling(metadata, song);

            var RenderPianoRollAndComposite = (VideoFrameMetadata frame, int idx, (int, Color)[] keys) =>
            {
                var s = channelStates[idx];

                pianoRollGraphics.BeginDrawFrame(new Rectangle(0, 0, renderSizeY, renderSizeX), true, Theme.DarkGreyColor2);
                pianoRoll.RenderVideoFrame(pianoRollGraphics, s.songChannelIndex, separateChannels ? 0 : settings.ChannelMask, frame.playPattern, frame.playNote, frame.channelData[s.songChannelIndex].scroll, keys);
                pianoRollGraphics.EndDrawFrame(true);

                if (settings.PianoRollPerspective > 0)
                {
                    perspective2xGraphics.BeginDrawFrame(new Rectangle(0, 0, channelSizeX * 2, channelSizeY * 2), false, Theme.DarkGreyColor2);
                    perspective2xGraphics.DefaultCommandList.DrawTexture(pianoRollTexture, -numPerspectivePixels * 2, 0, renderSizeX * 2, channelSizeY * 2, u, v, 1, 1, TextureFlags.Perspective | TextureFlags.Rotated90);
                    perspective2xGraphics.EndDrawFrame();

                    perspective1xGraphics.BeginDrawFrame(new Rectangle(0, 0, channelSizeX, channelSizeY), false, Theme.DarkGreyColor2);
                    perspective1xGraphics.DefaultCommandList.DrawTexture(perspective2xTexture, 0, 0, channelSizeX, channelSizeY, 0, 1, 1, 0);
                    perspective1xGraphics.EndDrawFrame();
                }

                var channelPosX = (int)MathF.Round((idx % numCols) * channelSizeXFloat);
                var channelPosY = (int)MathF.Round((idx / numCols) * channelSizeYFloat);

                videoGraphics.BeginDrawFrame(new Rectangle(0, 0, videoResX, videoResY), idx == 0, Theme.DarkGreyColor2);

                if (settings.PianoRollPerspective > 0)
                {
                    videoGraphics.DrawBlur(perspective1xTexture.Id, channelPosX, channelPosY, channelSizeX, channelSizeY, channelSizeY / 3, blurScale);
                }
                else
                {
                    videoGraphics.PushClipRegion(channelPosX, channelPosY, channelSizeX, channelSizeY);
                    videoGraphics.DefaultCommandList.DrawTexture(pianoRollTexture, channelPosX, channelPosY, channelSizeX, channelSizeY, 0, 0, 1, 1, TextureFlags.Rotated90);
                    videoGraphics.PopClipRegion();
                }

                videoGraphics.EndDrawFrame();
            };

            return LaunchEncoderLoop((f) =>
            {
                var frame = metadata[f];

                for (int i = 0; i < channelStates.Length; i++)
                {
                    var s = channelStates[i];
                    var volume = frame.channelData[s.songChannelIndex].volume;
                    var note = frame.channelData[s.songChannelIndex].note;
                    var color = Color.Invisible;

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

                    highlightedKeys[i] = (note.Value + settings.ChannelTranspose[s.songChannelIndex], color);
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

                // Gradients + grid lines.
                for (var i = 0; i < numRows; i++)
                {
                    var by = i * channelSizeY;
                    c.FillRectangleGradient(0, by, videoResX, by + gradientSizeY, Color.Black, Color.Invisible, true, gradientSizeY);

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
                    o.DrawTexture(s.icon, channelIconPosX, 0, Theme.LightGreyColor1);
                    o.DrawText(s.channelText, font, channelIconPosX + s.icon.Size.Width + ChannelIconTextSpacing, textOffsetY, Theme.LightGreyColor1);
                    o.PopTransform();

                    o.PushTransform(channelPosX + oscChannelPadX, channelPosY + oscPosY, channelHeaderSizeXFloat - oscChannelPadX * 2, -oscScaleY);
                    o.DrawNiceSmoothLine(oscilloscope, frame.channelData[i].color, settings.OscLineThickness);
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
