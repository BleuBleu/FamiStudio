using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Android.Media;
using Android.Opengl;
using Android.Views;
using Java.Nio;

namespace FamiStudio
{
    public class VideoEncoderAndroidBase : IVideoEncoder
    {
        protected const int EGL_RECORDABLE_ANDROID = 0x3142;

        protected Surface surface;

        protected EGLDisplay eglDisplay = EGL14.EglNoDisplay;
        protected EGLContext eglContext = EGL14.EglNoContext;
        protected EGLSurface eglSurface = EGL14.EglNoSurface;

        protected EGLDisplay prevEglDisplay;
        protected EGLContext prevEglContext;
        protected EGLSurface prevEglSurfaceRead;
        protected EGLSurface prevEglSurfaceDraw;

        private int surfaceResX;
        private int surfaceResY;

        public virtual bool BeginEncoding(int resX, int resY, int rateNumer, int rateDenom, int videoBitRate, int audioBitRate, bool stereo, string audioFile, string outputFile)
        {
            surfaceResX = resX;
            surfaceResY = resY;
            ElgInitialize();
            return true;
        }

        public virtual bool AddFrame(OffscreenGraphics graphics)
        {
            EglSwapBuffers();
            return true;
        }

        public virtual void EndEncoding(bool abort)
        {
            ElgShutdown();
        }

        private bool ElgInitialize()
        {
            prevEglContext = EGL14.EglGetCurrentContext();
            prevEglDisplay = EGL14.EglGetCurrentDisplay();
            prevEglSurfaceRead = EGL14.EglGetCurrentSurface(EGL14.EglRead);
            prevEglSurfaceDraw = EGL14.EglGetCurrentSurface(EGL14.EglDraw);

            eglDisplay = EGL14.EglGetDisplay(EGL14.EglDefaultDisplay);
            if (eglDisplay == EGL14.EglNoDisplay)
                return false;

            int[] version = new int[2];
            if (!EGL14.EglInitialize(eglDisplay, version, 0, version, 1))
                return false;

            // Configure EGL for recording and OpenGL ES 2.0.
            int[] attribList =
            {
                EGL14.EglRedSize, 8,
                EGL14.EglGreenSize, 8,
                EGL14.EglBlueSize, 8,
                EGL14.EglAlphaSize, 8,
                EGL14.EglDepthSize, 16,
                EGL14.EglRenderableType, EGL14.EglOpenglEsBit,
                EGL_RECORDABLE_ANDROID, 1,
                EGL14.EglNone
            };
            EGLConfig[] configs = new EGLConfig[1];
            int[] numConfigs = new int[1];
            EGL14.EglChooseConfig(eglDisplay, attribList, 0, configs, 0, configs.Length, numConfigs, 0);
            CheckEglError();

            // Configure context for OpenGL ES 2.0.
            int[] attrib_list =
            {
                EGL14.EglContextClientVersion, 2,
                EGL14.EglNone
            };
            eglContext = EGL14.EglCreateContext(eglDisplay, configs[0], EGL14.EglNoContext, attrib_list, 0);
            CheckEglError();

            if (surface != null)
            {
                int[] surfaceAttribs = { EGL14.EglNone };
                eglSurface = EGL14.EglCreateWindowSurface(eglDisplay, configs[0], surface, surfaceAttribs, 0);
            }
            else
            {
                int[] surfaceAttribs = 
                {
                    EGL14.EglWidth,  surfaceResX,
                    EGL14.EglHeight, surfaceResY,
                    EGL14.EglTextureFormat, EGL14.EglTextureRgba,
                    EGL14.EglTextureTarget, EGL14.EglTexture2d,
                    EGL14.EglNone
                };
                eglSurface = EGL14.EglCreatePbufferSurface(eglDisplay, configs[0], surfaceAttribs, 0);
            }
            CheckEglError();

            EGL14.EglMakeCurrent(eglDisplay, eglSurface, eglSurface, eglContext);
            CheckEglError();

            return true;
        }

        private void ElgShutdown()
        {
            if (eglDisplay != EGL14.EglNoDisplay)
            {
                EGL14.EglMakeCurrent(eglDisplay, EGL14.EglNoSurface, EGL14.EglNoSurface, EGL14.EglNoContext);
                EGL14.EglDestroySurface(eglDisplay, eglSurface);
                EGL14.EglDestroyContext(eglDisplay, eglContext);
                EGL14.EglReleaseThread();
                EGL14.EglTerminate(eglDisplay);
            }

            surface?.Release();

            eglDisplay = EGL14.EglNoDisplay;
            eglContext = EGL14.EglNoContext;
            eglSurface = EGL14.EglNoSurface;

            surface = null;

            EGL14.EglMakeCurrent(prevEglDisplay, prevEglSurfaceDraw, prevEglSurfaceRead, prevEglContext);
        }

        private void EglSwapBuffers()
        {
            EGL14.EglSwapBuffers(eglDisplay, eglSurface);
            CheckEglError();
        }

        protected void CheckEglError()
        {
            Debug.Assert(EGL14.EglGetError() == EGL14.EglSuccess);
        }
    }

    // Based off https://bigflake.com/mediacodec/. Thanks!!!
    public class VideoEncoderAndroid : VideoEncoderAndroidBase
    {
        const long SecondsToMicroSeconds = 1000000;
        const long SecondsToNanoSeconds  = 1000000000;

        private readonly string VideoMimeType = "video/avc";       // H.264 Advanced Video Coding
        private readonly string AudioMimeType = "audio/mp4a-latm"; // AAC

        private MediaCodec videoEncoder;
        private MediaCodec audioEncoder;
        private MediaMuxer muxer;
        private int videoTrackIndex;
        private int audioTrackIndex;
        private int frameIndex;
        private int frameRateNumer;
        private int frameRateDenom;
        private int numAudioChannels;
        private bool muxerStarted;
        private bool abortAudioEncoding;
        private ManualResetEvent muxerStartEvent = new ManualResetEvent(false);
        private Task audioEncodingTask;

        private int audioDataIdx;
        private byte[] audioData;

        private MediaCodec.BufferInfo videoBufferInfo;
        private MediaCodec.BufferInfo audioBufferInfo;

        public VideoEncoderAndroid()
        {
        }

        // https://github.com/lanhq147/SampleMediaFrame/blob/e2f20ff9eef73318e5a9b4de15458c5c2eb0fd46/app/src/main/java/com/google/android/exoplayer2/video/av/HWRecorder.java

        public override bool BeginEncoding(int resX, int resY, int rateNumer, int rateDenom, int videoBitRate, int audioBitRate, bool stereo, string audioFile, string outputFile)
        {
            videoBufferInfo = new MediaCodec.BufferInfo();
            audioBufferInfo = new MediaCodec.BufferInfo();

            frameRateNumer = rateNumer;
            frameRateDenom = rateDenom;
            numAudioChannels = stereo ? 2 : 1;

            MediaFormat videoFormat = MediaFormat.CreateVideoFormat(VideoMimeType, resX, resY);
            videoFormat.SetInteger(MediaFormat.KeyColorFormat, (int)MediaCodecCapabilities.Formatsurface);
            videoFormat.SetInteger(MediaFormat.KeyBitRate, videoBitRate * 1000);
            videoFormat.SetFloat(MediaFormat.KeyFrameRate, rateNumer / (float)rateDenom);
            videoFormat.SetInteger(MediaFormat.KeyIFrameInterval, 4);
            videoFormat.SetInteger(MediaFormat.KeyProfile, (int)MediaCodecProfileType.Avcprofilehigh);
            videoFormat.SetInteger(MediaFormat.KeyLevel, (int)MediaCodecProfileLevel.Avclevel31);
            
            videoEncoder = MediaCodec.CreateEncoderByType(VideoMimeType);
            videoEncoder.Configure(videoFormat, null, null, MediaCodecConfigFlags.Encode);
            surface = videoEncoder.CreateInputSurface();
            videoEncoder.Start();
            
            MediaFormat audioFormat = MediaFormat.CreateAudioFormat(AudioMimeType, 44100, numAudioChannels);
            audioFormat.SetInteger(MediaFormat.KeyAacProfile, (int)MediaCodecProfileType.Aacobjectlc);
            audioFormat.SetInteger(MediaFormat.KeyBitRate, audioBitRate * 1000);

            audioEncoder = MediaCodec.CreateEncoderByType(AudioMimeType);
            audioEncoder.Configure(audioFormat, null, null, MediaCodecConfigFlags.Encode);
            audioEncoder.Start();
            
            try
            {
                muxer = new MediaMuxer(outputFile, MuxerOutputType.Mpeg4);
            }
            catch
            {
                return false;
            }

            videoTrackIndex = -1;
            audioTrackIndex = -1;
            muxerStarted = false;

            if (!base.BeginEncoding(resX, resY, rateNumer, rateDenom, videoBitRate, audioBitRate, stereo, audioFile, outputFile))
            {
                return false;

            }

            audioData = File.ReadAllBytes(audioFile);

            if (audioData == null)
                return false;

            DrainEncoder(videoEncoder, videoBufferInfo, videoTrackIndex, false);
            DrainEncoder(audioEncoder, audioBufferInfo, audioTrackIndex, false);

            audioEncodingTask = Task.Factory.StartNew(AudioEncodeThread, TaskCreationOptions.LongRunning);

            return true;
        }

        private void AudioEncodeThread()
        {
            var done = false;

            try
            {
                while (!done && !abortAudioEncoding)
                {
                    done = !WriteAudio();
                    DrainEncoder(audioEncoder, audioBufferInfo, audioTrackIndex, false);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
            }
        }

        private bool WriteAudio()
        {
            int index = audioEncoder.DequeueInputBuffer(-1);
            if (index >= 0)
            {
                ByteBuffer buffer = audioEncoder.GetInputBuffer(index);

                var len = Utils.Clamp(audioData.Length - audioDataIdx, 0, buffer.Remaining());
                buffer.Clear();
                buffer.Put(audioData, audioDataIdx, len);

                long presentationTime = (audioDataIdx * SecondsToMicroSeconds) / (44100 * sizeof(short) * numAudioChannels);
                audioDataIdx += len;

                var done = audioDataIdx == audioData.Length;
                audioEncoder.QueueInputBuffer(index, 0, len, presentationTime, done ? MediaCodecBufferFlags.EndOfStream : MediaCodecBufferFlags.None);
            }

            return audioDataIdx < audioData.Length;
        }

        public override bool AddFrame(OffscreenGraphics graphics)
        {
            Debug.WriteLine($"Sending frame {frameIndex} to encoder");

            long presentationTime = ComputePresentationTimeNsec(frameIndex++);
            EGLExt.EglPresentationTimeANDROID(eglDisplay, eglSurface, presentationTime);
            CheckEglError();

            if (!base.AddFrame(graphics))
            {
                return false;
            }

            DrainEncoder(videoEncoder, videoBufferInfo, videoTrackIndex, false);
            
            return true;
        }

        public override void EndEncoding(bool abort)
        {
            Debug.WriteLine("Releasing encoder objects");

            abortAudioEncoding = abort;

            if (audioEncodingTask != null)
            {
                audioEncodingTask.Wait();
                audioEncodingTask = null;
            }

            if (!abortAudioEncoding)
            {
                DrainEncoder(videoEncoder, videoBufferInfo, videoTrackIndex, false);
                DrainEncoder(audioEncoder, audioBufferInfo, audioTrackIndex, false);
            }

            if (videoEncoder != null)
            {
                videoEncoder.Stop();
                videoEncoder.Release();
                videoEncoder = null;
            }

            if (audioEncoder != null)
            {
                audioEncoder.Stop();
                audioEncoder.Release();
                audioEncoder = null;
            }

            base.EndEncoding(abort);

            if (muxer != null)
            {
                muxer.Stop();
                muxer.Release();
                muxer = null;
            }
        }

        private void DrainEncoder(MediaCodec encoder, MediaCodec.BufferInfo bufferInfo, int trackIndex, bool endOfStream)
        {
            Debug.WriteLine($"DrainEncoder {endOfStream})");

            const int TIMEOUT_USEC = 10000;

            if (endOfStream)
            {
                Debug.WriteLine("Sending EOS to encoder");
                encoder.SignalEndOfInputStream();
            }

            while (true)
            {
                int encoderStatus = encoder.DequeueOutputBuffer(bufferInfo, TIMEOUT_USEC);
                if (encoderStatus == (int)MediaCodecInfoState.TryAgainLater)
                {
                    // no output available yet
                    if (!endOfStream)
                    {
                        break;      // out of while
                    }
                    else
                    {
                        Debug.WriteLine("No output available, spinning to await EOS");
                    }
                }
                else if (encoderStatus == (int)MediaCodecInfoState.OutputFormatChanged)
                {
                    Debug.Assert(!muxerStarted);
                    MediaFormat newFormat = encoder.OutputFormat;
                    Debug.WriteLine($"Encoder output format changed: {newFormat}");

                    var isVideo = encoder == videoEncoder;

                    if (isVideo)
                    {
                        videoTrackIndex = muxer.AddTrack(newFormat);
                        trackIndex = videoTrackIndex;
                    }
                    else
                    {
                        audioTrackIndex = muxer.AddTrack(newFormat);
                        trackIndex = audioTrackIndex;
                    }

                    // now that we have the Magic Goodies, start the muxer
                    if (videoTrackIndex >= 0 && audioTrackIndex >= 0)
                    {
                        muxer.Start();
                        muxerStarted = true;
                        muxerStartEvent.Set();
                    }
                }
                else if (encoderStatus < 0)
                {
                    Debug.WriteLine($"Unexpected result from encoder.dequeueOutputBuffer: {encoderStatus}");
                }
                else
                {
                    ByteBuffer encodedData = encoder.GetOutputBuffer(encoderStatus);
                    Debug.Assert(encodedData != null);

                    if ((bufferInfo.Flags & MediaCodecBufferFlags.CodecConfig) != 0)
                    {
                        Debug.WriteLine("Ignoring BUFFER_FLAG_CODEC_CONFIG");
                        bufferInfo.Size = 0;
                    }

                    if (bufferInfo.Size != 0)
                    {
                        muxerStartEvent.WaitOne();

                        encodedData.Position(bufferInfo.Offset);
                        encodedData.Limit(bufferInfo.Offset + bufferInfo.Size);

                        muxer.WriteSampleData(trackIndex, encodedData, bufferInfo);
                        Debug.WriteLine($"Sent {bufferInfo.Size} bytes to muxer");
                    }

                    encoder.ReleaseOutputBuffer(encoderStatus, false);

                    if ((bufferInfo.Flags & MediaCodecBufferFlags.EndOfStream) != 0)
                    {
                        if (!endOfStream)
                        {
                            Debug.WriteLine("Reached end of stream unexpectedly");
                        }
                        else
                        {
                            Debug.WriteLine("End of stream reached");
                        }
                        break; 
                    }
                }
            }
        }

        private long ComputePresentationTimeNsec(int frameIndex)
        {
            return frameIndex * SecondsToNanoSeconds * frameRateDenom / frameRateNumer;
        }
    }
}