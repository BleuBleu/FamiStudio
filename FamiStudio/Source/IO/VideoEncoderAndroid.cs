using Android.Media;
using Android.Opengl;
using Android.Views;
using Java.Nio;
using System;

using Debug = System.Diagnostics.Debug;

namespace FamiStudio
{
    // Based off https://bigflake.com/mediacodec/. Thanks!!!
    class VideoEncoderAndroid
    {
        private string MimeType = "video/avc";    // H.264 Advanced Video Coding

        private Surface mSurface;
        private MediaCodec encoder;
        private MediaMuxer muxer;
        private int trackIndex;
        private int frameIndex;
        private int frameRateNumer;
        private int frameRateDenom;
        private bool muxerStarted;

        private MediaCodec.BufferInfo bufferInfo;

        private const int EGL_RECORDABLE_ANDROID = 0x3142;

        private EGLDisplay eglDisplay = EGL14.EglNoDisplay;
        private EGLContext eglContext = EGL14.EglNoContext;
        private EGLSurface eglSurface = EGL14.EglNoSurface;

        private EGLDisplay prevEglDisplay;
        private EGLContext prevEglContext;
        private EGLSurface prevEglSurfaceRead;
        private EGLSurface prevEglSurfaceDraw;

        private VideoEncoderAndroid()
        {
        }

        public static VideoEncoderAndroid CreateInstance()
        {
            // DROIDTODO : Check support!
            return new VideoEncoderAndroid();
        }

        public bool BeginEncoding(int resX, int resY, int rateNumer, int rateDenom, int videoBitRate, int audioBitRate, string audioFile, string outputFile)
        {
            bufferInfo = new MediaCodec.BufferInfo();

            frameRateNumer = rateNumer;
            frameRateDenom = rateDenom;

            MediaFormat format = MediaFormat.CreateVideoFormat(MimeType, resX, resY);

            format.SetInteger(MediaFormat.KeyColorFormat, (int)MediaCodecCapabilities.Formatsurface);
            format.SetInteger(MediaFormat.KeyBitRate, videoBitRate * 1000);
            format.SetFloat(MediaFormat.KeyFrameRate, rateNumer / (float)rateDenom);
            format.SetInteger(MediaFormat.KeyIFrameInterval, 4);
            format.SetInteger(MediaFormat.KeyProfile, (int)MediaCodecProfileType.Avcprofilehigh);
            format.SetInteger(MediaFormat.KeyLevel, (int)MediaCodecProfileLevel.Avclevel31);
            Debug.WriteLine($"Media format : {format}");

            encoder = MediaCodec.CreateEncoderByType(MimeType);
            encoder.Configure(format, null, null, MediaCodecConfigFlags.Encode);
            mSurface = encoder.CreateInputSurface();
            encoder.Start();
            
            try
            {
                muxer = new MediaMuxer(outputFile, MuxerOutputType.Mpeg4);
            }
            catch (Exception e)
            {
                return false;
            }

            trackIndex = -1;
            muxerStarted = false;

            if (!ElgInitialize())
                return false;

            DrainEncoder(false);

            return true;
        }

        public void AddFrame(byte[] image)
        {
            Console.WriteLine($"Sending frame {frameIndex} to encoder");

            EGLExt.EglPresentationTimeANDROID(eglDisplay, eglSurface, ComputePresentationTimeNsec(frameIndex++));
            CheckEglError();

            EGL14.EglSwapBuffers(eglDisplay, eglSurface);
            CheckEglError();

            DrainEncoder(false);
        }

        public void EndEncoding()
        {
            Debug.WriteLine("Releasing encoder objects");

            DrainEncoder(false);

            if (encoder != null)
            {
                encoder.Stop();
                encoder.Release();
                encoder = null;
            }
            
            ElgShutdown();

            if (muxer != null)
            {
                muxer.Stop();
                muxer.Release();
                muxer = null;
            }
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
                EGL14.EglContextClientVersion, 1,
                EGL14.EglNone
            };
            eglContext = EGL14.EglCreateContext(eglDisplay, configs[0], EGL14.EglNoContext, attrib_list, 0);
            CheckEglError();

            int[] surfaceAttribs = { EGL14.EglNone };
            eglSurface = EGL14.EglCreateWindowSurface(eglDisplay, configs[0], mSurface, surfaceAttribs, 0);
            CheckEglError();

            EGL14.EglMakeCurrent(eglDisplay, eglSurface, eglSurface, eglContext);
            CheckEglError();

            return true;
        }

        public void ElgShutdown()
        {
            if (eglDisplay != EGL14.EglNoDisplay)
            {
                EGL14.EglMakeCurrent(eglDisplay, EGL14.EglNoSurface, EGL14.EglNoSurface, EGL14.EglNoContext);
                EGL14.EglDestroySurface(eglDisplay, eglSurface);
                EGL14.EglDestroyContext(eglDisplay, eglContext);
                EGL14.EglReleaseThread();
                EGL14.EglTerminate(eglDisplay);
            }

            mSurface.Release();

            eglDisplay = EGL14.EglNoDisplay;
            eglContext = EGL14.EglNoContext;
            eglSurface = EGL14.EglNoSurface;

            mSurface = null;

            EGL14.EglMakeCurrent(prevEglDisplay, prevEglSurfaceDraw, prevEglSurfaceRead, prevEglContext);
        }

        private void CheckEglError()
        {
            Debug.Assert(EGL14.EglGetError() == EGL14.EglSuccess);
        }

        private void DrainEncoder(bool endOfStream)
        {
            Debug.WriteLine($"DrainEncoder {endOfStream})");

            const int TIMEOUT_USEC = 10000;

            if (endOfStream)
            {
                Debug.WriteLine("Sending EOS to encoder");
                encoder.SignalEndOfInputStream();
            }

            ByteBuffer[] encoderOutputBuffers = encoder.GetOutputBuffers();
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
                else if (encoderStatus == (int)MediaCodecInfoState.OutputBuffersChanged)
                {
                    encoderOutputBuffers = encoder.GetOutputBuffers();
                }
                else if (encoderStatus == (int)MediaCodecInfoState.OutputFormatChanged)
                {
                    if (muxerStarted)
                    {
                        throw new Exception();
                    }
                    MediaFormat newFormat = encoder.OutputFormat;
                    Debug.WriteLine("Encoder output format changed: {newFormat}");

                    // now that we have the Magic Goodies, start the muxer
                    trackIndex = muxer.AddTrack(newFormat);
                    muxer.Start();
                    muxerStarted = true;
                }
                else if (encoderStatus < 0)
                {
                    Debug.WriteLine($"Unexpected result from encoder.dequeueOutputBuffer: {encoderStatus}");
                }
                else
                {
                    ByteBuffer encodedData = encoderOutputBuffers[encoderStatus];
                    if (encodedData == null)
                    {
                        throw new Exception();
                    }

                    if ((bufferInfo.Flags & MediaCodecBufferFlags.CodecConfig) != 0)
                    {
                        Debug.WriteLine("Ignoring BUFFER_FLAG_CODEC_CONFIG");
                        bufferInfo.Size = 0;
                    }

                    if (bufferInfo.Size != 0)
                    {
                        if (!muxerStarted)
                        {
                            throw new Exception("muxer hasn't started");
                        }

                        encodedData.Position(bufferInfo.Offset);
                        encodedData.Limit(bufferInfo.Offset + bufferInfo.Size);

                        muxer.WriteSampleData(trackIndex, encodedData, bufferInfo);
                        Debug.WriteLine("Sent {mBufferInfo.Size} bytes to muxer");
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
            const long OneBillion = 1000000000;
            return frameIndex * OneBillion * frameRateDenom / frameRateNumer;
        }
    }
}