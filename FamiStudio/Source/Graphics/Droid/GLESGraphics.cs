using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Android.Opengl;
using Java.Nio;

namespace FamiStudio
{
    public class Graphics : GraphicsBase
    {
        private int polyProgram;
        private int polyScaleBiasUniform;
        private int polyDashScaleUniform;

        private int lineProgram;
        private int lineScaleBiasUniform;
        private int lineDashScaleUniform;

        private int lineSmoothProgram;
        private int lineSmoothScaleBiasUniform;
        private int lineSmoothWindowSizeUniform;

        private int bmpProgram;
        private int bmpScaleBiasUniform;
        private int bmpTextureUniform;

        private int blurProgram;
        private int blurScaleBiasUniform;
        private int blurKernelUniform;
        private int blurTextureUniform;
        
        private int textProgram;
        private int textScaleBiasUniform;
        private int textTextureUniform;

        private int depthProgram;
        private int depthScaleBiasUniform;

        // Must be powers of two.
        const int MinBufferSize = 16;
        const int MaxBufferSize = 128 * 1024;

        const int MinBufferSizeLog2 = 4;
        const int MaxBufferSizeLog2 = 18;
        const int NumBufferSizes    = MaxBufferSizeLog2 - MinBufferSizeLog2 + 1;

        // Index [0] is MaxBufferSize
        // Index [1] is MaxBufferSize / 2
        // Index [2] is MaxBufferSize / 4
        // ...
        List<FloatBuffer>[] freeVtxBuffers = new List<FloatBuffer>[NumBufferSizes];
        List<IntBuffer>[]   freeColBuffers = new List<IntBuffer>  [NumBufferSizes];
        List<ByteBuffer>[]  freeBytBuffers = new List<ByteBuffer> [NumBufferSizes];
        List<ShortBuffer>[] freeIdxBuffers = new List<ShortBuffer>[NumBufferSizes];

        List<FloatBuffer>[] usedVtxBuffers = new List<FloatBuffer>[NumBufferSizes];
        List<IntBuffer>[]   usedColBuffers = new List<IntBuffer>  [NumBufferSizes];
        List<ByteBuffer>[]  usedBytBuffers = new List<ByteBuffer> [NumBufferSizes];
        List<ShortBuffer>[] usedIdxBuffers = new List<ShortBuffer>[NumBufferSizes];

        ShortBuffer quadIdxBuffer;

        public Graphics(bool offscreen = false) : base(offscreen)
        {
            dashSize = 8;

            for (int i = 0; i < NumBufferSizes; i++)
            {
                freeVtxBuffers[i] = new List<FloatBuffer>();
                freeColBuffers[i] = new List<IntBuffer>();
                freeBytBuffers[i] = new List<ByteBuffer>();
                freeIdxBuffers[i] = new List<ShortBuffer>();
                usedVtxBuffers[i] = new List<FloatBuffer>();
                usedColBuffers[i] = new List<IntBuffer>();
                usedBytBuffers[i] = new List<ByteBuffer>();
                usedIdxBuffers[i] = new List<ShortBuffer>();
            }

            quadIdxBuffer = ByteBuffer.AllocateDirect(sizeof(short) * quadIdxArray.Length).Order(ByteOrder.NativeOrder()).AsShortBuffer();
            quadIdxBuffer.Put(quadIdxArray);
            quadIdxBuffer.Position(0);
            quadIdxArray = null;

            InitializeShaders();

            var temp = new int[1];
            GLES20.GlGetIntegerv(GLES20.GlMaxTextureSize, temp, 0);
            maxTextureSize = temp[0];
        }

        private void FreeBuffers<T>(List<T>[] bufferLists) where T : IDisposable
        {
            foreach (var list in bufferLists)
            {
                foreach (var buffer in list)
                {
                    buffer.Dispose();
                }

                list.Clear();
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            FreeShaders();
            FreeBuffers(freeVtxBuffers);
            FreeBuffers(freeColBuffers);
            FreeBuffers(freeBytBuffers);
            FreeBuffers(freeIdxBuffers);
            FreeBuffers(usedVtxBuffers);
            FreeBuffers(usedColBuffers);
            FreeBuffers(usedBytBuffers);
            FreeBuffers(usedIdxBuffers);
            Utils.DisposeAndNullify(ref quadIdxBuffer);
        }

        private int CompileShader(string resourceName, int type, out List<string> attributes)
        {
            var code = "";
            
            if (type == GLES20.GlVertexShader)
            {
                code += "precision highp float;\n";
            }
            else
            {
                code += "precision mediump float;\n";
            }

            code += "#define ATTRIB_IN attribute\n";
            code += "#define INTERP_IN varying\n";
            code += "#define INTERP_OUT varying\n";
            code += "#define INTERP_PERSPECTIVE_IN varying\n";
            code += "#define INTERP_PERSPECTIVE_OUT varying\n";
            code += "#define TEX texture2D\n";
            code += "#define TEXPROJ texture2DProj\n";
            code += "#define FAMISTUDIO_ANDROID 1\n";

            using (Stream stream = typeof(GraphicsBase).Assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                code += reader.ReadToEnd();
            }

            if (type == GLES20.GlVertexShader)
            {
                attributes = new List<string>();
                
                var lines = code.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var l in lines)
                {
                    if (l.StartsWith("ATTRIB_IN "))
                    {
                        var splits = l.Split(new[] { ' ', '\t', ';' }, StringSplitOptions.RemoveEmptyEntries);
                        attributes.Add(splits[splits.Length - 1]);
                    }
                }
            }
            else
            {
                attributes = null;
            }

            var shader = GLES20.GlCreateShader(type);

            GLES20.GlShaderSource(shader, code);
            GLES20.GlCompileShader(shader);

            var status = new int[1];
            GLES20.GlGetShaderiv(shader, GLES20.GlCompileStatus, status, 0);

            if (status[0] == 0)
            {
                var log = GLES20.GlGetShaderInfoLog(shader);
                Debug.WriteLine(resourceName);
                Debug.WriteLine(log);
                Debug.Assert(false);
                return 0;
            }

            return shader;
        }

        private int CompileAndLinkProgram(string resourceName, bool useFragment = true)
        {
            var program = GLES20.GlCreateProgram();

            var vert = CompileShader(resourceName + ".vert", GLES20.GlVertexShader, out var attributes);
            GLES20.GlAttachShader(program, vert);

            if (useFragment)
            {
                var frag = CompileShader(resourceName + ".frag", GLES20.GlFragmentShader, out _);
                GLES20.GlAttachShader(program, frag);
            }

            for (int i = 0; i < attributes.Count; i++)
            {
                GLES20.GlBindAttribLocation(program, i, attributes[i]);
            }
            
            GLES20.GlLinkProgram(program);

            var status = new int[1];
            GLES20.GlGetProgramiv(program, GLES20.GlLinkStatus, status, 0);

            if (status[0] == 0)
            {
                var log = GLES20.GlGetProgramInfoLog(program);
                Debug.WriteLine(resourceName);
                Debug.WriteLine(log);
                Debug.Assert(false);
                return 0;
            }

            GLES20.GlUseProgram(program);

            return program;
        }

        private void InitializeShaders()
        {
            polyProgram = CompileAndLinkProgram("FamiStudio.Resources.Shaders.Poly" );            
            polyScaleBiasUniform = GLES20.GlGetUniformLocation(polyProgram, "screenScaleBias");
            polyDashScaleUniform = GLES20.GlGetUniformLocation(polyProgram, "dashScale");

            lineProgram = CompileAndLinkProgram("FamiStudio.Resources.Shaders.Line");
            lineScaleBiasUniform = GLES20.GlGetUniformLocation(lineProgram, "screenScaleBias");
            lineDashScaleUniform = GLES20.GlGetUniformLocation(lineProgram, "dashScale");

            lineSmoothProgram = CompileAndLinkProgram("FamiStudio.Resources.Shaders.LineSmooth");
            lineSmoothScaleBiasUniform = GLES20.GlGetUniformLocation(lineSmoothProgram, "screenScaleBias");
            lineSmoothWindowSizeUniform = GLES20.GlGetUniformLocation(lineSmoothProgram, "windowSize");

            bmpProgram = CompileAndLinkProgram("FamiStudio.Resources.Shaders.Bitmap");
            bmpScaleBiasUniform = GLES20.GlGetUniformLocation(bmpProgram, "screenScaleBias");
            bmpTextureUniform = GLES20.GlGetUniformLocation(bmpProgram, "tex");

            blurProgram = CompileAndLinkProgram("FamiStudio.Resources.Shaders.Blur");
            blurScaleBiasUniform = GLES20.GlGetUniformLocation(blurProgram, "screenScaleBias");
            blurKernelUniform = GLES20.GlGetUniformLocation(blurProgram, "blurKernel");
            blurTextureUniform = GLES20.GlGetUniformLocation(blurProgram, "tex");

            textProgram = CompileAndLinkProgram("FamiStudio.Resources.Shaders.Text");
            textScaleBiasUniform = GLES20.GlGetUniformLocation(textProgram, "screenScaleBias");
            textTextureUniform = GLES20.GlGetUniformLocation(textProgram, "tex");

            depthProgram = CompileAndLinkProgram("FamiStudio.Resources.Shaders.Depth");
            depthScaleBiasUniform = GLES20.GlGetUniformLocation(depthProgram, "screenScaleBias");
        }

        private void FreeShaders()
        {
            GLES20.GlDeleteProgram(polyProgram);
            GLES20.GlDeleteProgram(lineProgram);
            GLES20.GlDeleteProgram(lineSmoothProgram);
            GLES20.GlDeleteProgram(bmpProgram);
            GLES20.GlDeleteProgram(blurProgram);
            GLES20.GlDeleteProgram(textProgram);
            GLES20.GlDeleteProgram(depthProgram);
        }

        public override void EndDrawFrame(bool clearAlpha = false)
        {
            base.EndDrawFrame(clearAlpha);

            for (int i = 0; i < NumBufferSizes; i++)
            {
                freeVtxBuffers[i].AddRange(usedVtxBuffers[i]);
                freeColBuffers[i].AddRange(usedColBuffers[i]);
                freeBytBuffers[i].AddRange(usedBytBuffers[i]);
                freeIdxBuffers[i].AddRange(usedIdxBuffers[i]);
                usedVtxBuffers[i].Clear();
                usedColBuffers[i].Clear();
                usedBytBuffers[i].Clear();
                usedIdxBuffers[i].Clear();
            }
        }

        protected override void Initialize(bool clear, Color clearColor)
        {
            GLES20.GlViewport(screenRectFlip.Left, screenRectFlip.Top, screenRectFlip.Width, screenRectFlip.Height);
            GLES20.GlDisable((int)2884); // Cull face?
            GLES20.GlBlendFunc(GLES20.GlSrcAlpha, GLES20.GlOneMinusSrcAlpha);
            GLES20.GlEnable(GLES20.GlBlend);
            GLES20.GlEnable(GLES20.GlDepthTest);
            GLES20.GlDepthFunc(GLES20.GlAlways);
            GLES20.GlDisable(GLES20.GlStencilTest);
            GLES20.GlDisable(GLES20.GlScissorTest);

            if (clear)
            {
                GLES20.GlClearColor(clearColor.R / 255.0f, clearColor.G / 255.0f, clearColor.B / 255.0f, clearColor.A / 255.0f);
                GLES20.GlClear(GLES20.GlColorBufferBit | GLES20.GlDepthBufferBit);
            }
        }

        public void DrawBlur(int textureId, int x, int y, int width, int height, float blurScale = 2.0f)
        {
            var kernel = GetBlurKernel(width, height, blurScale);

            MakeQuad(x, y, width, height);

            GLES20.GlDepthFunc(GLES20.GlAlways);
            GLES20.GlUseProgram(blurProgram);
            GLES20.GlUniform4fv(blurScaleBiasUniform, 1, viewportScaleBias, 0);
            GLES20.GlUniform1i(blurTextureUniform, 0);
            GLES20.GlUniform4fv(blurKernelUniform, kernel.Length / 4, kernel, 0);
            GLES20.GlActiveTexture(GLES20.GlTexture0 + 0);
            GLES20.GlBindTexture(GLES20.GlTexture2d, textureId);

            BindAndUpdateVertexBuffer(0, vtxArray, 8);
            BindAndUpdateVertexBuffer(1, texArray, 8, 2);

            quadIdxBuffer.Position(0);
            GLES20.GlDrawElements(GLES20.GlTriangles, 6, GLES20.GlUnsignedShort, quadIdxBuffer);
            GLES20.GlDepthFunc(GLES20.GlEqual);
        }

        protected override bool DrawDepthPrepass()
        {
            if (clipRegions.Count == 0)
            {
                GLES20.GlDepthFunc(GLES20.GlAlways);
                GLES20.GlColorMask(true, true, true, true);
                return false;
            }

            GLES20.GlDepthMask(true);
            GLES20.GlDepthFunc(GLES20.GlAlways);
            GLES20.GlColorMask(false, false, false, false);

            var vtxIdx = 0;
            var depIdx = 0;

            for (int i = 0; i < clipRegions.Count; i++)
            {
                var clip = clipRegions[i];

                var x0 = clip.rect.Left;
                var y0 = clip.rect.Top;
                var x1 = clip.rect.Right;
                var y1 = clip.rect.Bottom;

                vtxArray[vtxIdx++] = x0;
                vtxArray[vtxIdx++] = y0;
                vtxArray[vtxIdx++] = x1;
                vtxArray[vtxIdx++] = y0;
                vtxArray[vtxIdx++] = x1;
                vtxArray[vtxIdx++] = y1;
                vtxArray[vtxIdx++] = x0;
                vtxArray[vtxIdx++] = y1;

                depArray[depIdx++] = clip.depthValue;
                depArray[depIdx++] = clip.depthValue;
                depArray[depIdx++] = clip.depthValue;
                depArray[depIdx++] = clip.depthValue;
            }

            GLES20.GlUseProgram(depthProgram);
            GLES20.GlUniform4fv(depthScaleBiasUniform, 1, viewportScaleBias, 0);

            BindAndUpdateVertexBuffer(0, vtxArray, vtxIdx);
            BindAndUpdateByteBuffer(1, depArray, depIdx, true);

            quadIdxBuffer.Position(0);
            GLES20.GlDrawElements(GLES20.GlTriangles, vtxIdx / 8 * 6, GLES20.GlUnsignedShort, quadIdxBuffer);

            GLES20.GlDepthMask(false);
            GLES20.GlDepthFunc(GLES20.GlEqual);
            GLES20.GlColorMask(true, true, true, true);

            return true;
        }

        public void SetViewport(int x, int y, int width, int height)
        {
            GLES20.GlViewport(x, y, width, height);
        }

        public void Clear(Color color)
        {
            GLES20.GlClearColor(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
            GLES20.GlClear(GLES20.GlColorBufferBit);
        }

        protected override void ClearAlpha()
        {
            // Normally we would simply use seperate alpha blending to keep the alpha
            // to 1.0 all the time. But we are limiting ourselves to OpenGL 3.3 for now.
            GLES20.GlColorMask(false, false, false, true);
            GLES20.GlUseProgram(polyProgram);
            GLES20.GlUniform4fv(polyScaleBiasUniform, 1, viewportScaleBias, 0);
            GLES20.GlUniform1f(polyDashScaleUniform, 0.0f);
            GLES20.GlDepthFunc(GLES20.GlAlways);

            MakeFullScreenTriangle();
            BindAndUpdateVertexBuffer(0, vtxArray, 6);
            BindAndUpdateColorBuffer(1, colArray, 3);
            BindAndUpdateByteBuffer(2, depArray, 3, false); // Irrelevant
            BindAndUpdateByteBuffer(3, depArray, 3, true); // Unused

            GLES20.GlDrawElements(GLES20.GlTriangles, 3, GLES20.GlUnsignedShort, CopyGetIdxBuffer(idxArray, 3));
            GLES20.GlColorMask(true, true, true, true);
        }

        public void UpdateTexture(Texture bmp, int x, int y, int width, int height, int[] data)
        {
            var buffer = ByteBuffer.AllocateDirect(width * height * sizeof(int)).Order(ByteOrder.NativeOrder()).AsIntBuffer();
            buffer.Put(data);
            buffer.Position(0);

            GLES20.GlBindTexture(GLES20.GlTexture2d, bmp.Id);
            GLES20.GlTexSubImage2D(GLES20.GlTexture2d, 0, x, y, width, height, GLES20.GlRgba, GLES20.GlUnsignedByte, buffer);
        }

        public override void UpdateTexture(int id, int x, int y, int width, int height, byte[] data)
        {
            var buffer = ByteBuffer.AllocateDirect(width * height * sizeof(int)).Order(ByteOrder.NativeOrder());
            buffer.Put(data);
            buffer.Position(0);

            GLES20.GlPixelStorei(GLES20.GlUnpackAlignment, 1);
            GLES20.GlBindTexture(GLES20.GlTexture2d, id);
            GLES20.GlTexSubImage2D(GLES20.GlTexture2d, 0, x, y, width, height, GLES20.GlLuminance, GLES20.GlUnsignedByte, buffer);
            GLES20.GlPixelStorei(GLES20.GlUnpackAlignment, 4);
        }

        private int GetGLESTextureFormat(TextureFormat format)
        {
            switch (format)
            {
                case TextureFormat.R: return GLES20.GlLuminance;
                case TextureFormat.Rgb: return GLES20.GlRgb;
                case TextureFormat.Rgba: return GLES20.GlRgba;
                case TextureFormat.Depth: return GLES20.GlDepthComponent;
                default:
                    Debug.Assert(false);
                    return GLES20.GlRgba;
            }
        }

        public override int CreateTexture(int width, int height, TextureFormat format, bool filter)
        {
            Debug.Assert(Platform.ThreadOwnsGLContext);
            var id = new int[1];
            GLES20.GlGenTextures(1, id, 0);
            Debug.Assert(id[0] > 0);
            GLES20.GlBindTexture(GLES20.GlTexture2d, id[0]);
            GLES20.GlTexParameteri(GLES20.GlTexture2d, GLES20.GlTextureMinFilter, filter ? GLES20.GlLinear : GLES20.GlNearest);
            GLES20.GlTexParameteri(GLES20.GlTexture2d, GLES20.GlTextureMagFilter, filter ? GLES20.GlLinear : GLES20.GlNearest);
            GLES20.GlTexParameteri(GLES20.GlTexture2d, GLES20.GlTextureWrapS, GLES20.GlClampToEdge);
            GLES20.GlTexParameteri(GLES20.GlTexture2d, GLES20.GlTextureWrapT, GLES20.GlClampToEdge);

            var buffer = ByteBuffer.AllocateDirect(width * height * sizeof(int)).Order(ByteOrder.NativeOrder()).AsIntBuffer();
            buffer.Put(new int[width * height]);
            buffer.Position(0);

            var texFormat = GetGLESTextureFormat(format);
            GLES20.GlTexImage2D(GLES20.GlTexture2d, 0, texFormat, width, height, 0, texFormat, format == TextureFormat.Depth ? GLES20.GlUnsignedShort : GLES20.GlUnsignedByte, format == TextureFormat.Depth ? null : buffer);

            return id[0];
        }

        protected override int CreateTexture(SimpleBitmap bmp, bool filter)
        {
            var buffer = IntBuffer.Wrap(bmp.Data);
            var id = new int[1];
            GLES20.GlGenTextures(1, id, 0);
            GLES20.GlBindTexture(GLES20.GlTexture2d, id[0]);
            GLES20.GlTexImage2D(GLES20.GlTexture2d, 0, GLES20.GlRgba, bmp.Width, bmp.Height, 0, GLES20.GlRgba, GLES20.GlUnsignedByte, buffer);
            GLES20.GlTexParameteri(GLES20.GlTexture2d, GLES20.GlTextureMinFilter, filter ? GLES20.GlLinear : GLES20.GlNearest);
            GLES20.GlTexParameteri(GLES20.GlTexture2d, GLES20.GlTextureMagFilter, filter ? GLES20.GlLinear : GLES20.GlNearest);
            GLES20.GlTexParameteri(GLES20.GlTexture2d, GLES20.GlTextureWrapS, GLES20.GlClampToEdge);
            GLES20.GlTexParameteri(GLES20.GlTexture2d, GLES20.GlTextureWrapT, GLES20.GlClampToEdge);
            
            return id[0];
        }

        public override void DeleteTexture(int id)
        {
            var ids = new[] { id };
            GLES20.GlDeleteTextures(1, ids, 0);
        }

        protected override string GetScaledFilename(string name, out bool needsScaling)
        {
            var assembly = Assembly.GetExecutingAssembly();

            needsScaling = false;

            if (DpiScaling.Window >= 4.0f && assembly.GetManifestResourceInfo($"{name}@4x.tga") != null)
            {
                return $"{name}@4x.tga";
            }
            else if (DpiScaling.Window >= 2.0f && assembly.GetManifestResourceInfo($"{name}@2x.tga") != null)
            {
                return $"{name}@2x.tga";
            }
            else
            {
                return $"{name}.tga";
            }
        }

        public Texture CreateTextureFromResource(string name)
        {
            var bmp = LoadBitmapFromResourceWithScaling(name);
            return new Texture(this, CreateTexture(bmp, true), bmp.Width, bmp.Height, true, true);
        }

        protected override TextureAtlas CreateTextureAtlasFromResources(string[] names)
        {
            // Need to sort since we do binary searches on the names.
            Array.Sort(names);

            var bitmaps = new SimpleBitmap[names.Length];
            var elementSizeX = 0;
            var elementSizeY = 0;

            for (int i = 0; i < names.Length; i++)
            {
                var bmp = LoadBitmapFromResourceWithScaling(AtlasPrefix + names[i]);

                elementSizeX = Math.Max(elementSizeX, bmp.Width);
                elementSizeY = Math.Max(elementSizeY, bmp.Height);

                bitmaps[i] = bmp;
            }

            Debug.Assert(elementSizeX < MaxAtlasResolution);

            var elementsPerRow = MaxAtlasResolution / elementSizeX;
            var elementRects = new Rectangle[names.Length];
            var atlasSizeX = 0;
            var atlasSizeY = 0;

            for (int i = 0; i < names.Length; i++)
            {
                var bmp = bitmaps[i];
                var row = i / elementsPerRow;
                var col = i % elementsPerRow;

                elementRects[i] = new Rectangle(
                    col * elementSizeX,
                    row * elementSizeY,
                    bmp.Width,
                    bmp.Height);

                atlasSizeX = Math.Max(atlasSizeX, elementRects[i].Right);
                atlasSizeY = Math.Max(atlasSizeY, elementRects[i].Bottom);
            }

            atlasSizeX = Utils.NextPowerOfTwo(atlasSizeX);
            atlasSizeY = Utils.NextPowerOfTwo(atlasSizeY);

            var textureId = CreateTexture(atlasSizeX, atlasSizeY, TextureFormat.Rgba, true);
            GLES20.GlBindTexture(GLES20.GlTexture2d, textureId);

            Debug.WriteLine($"Creating bitmap atlas of size {atlasSizeX}x{atlasSizeY} with {names.Length} images:");

            for (int i = 0; i < names.Length; i++)
            {
                var bmp = bitmaps[i];
                var buffer = IntBuffer.Wrap(bmp.Data);

                //Debug.WriteLine($"  - {names[i]} ({bmp.Width} x {bmp.Height}):");

                GLES20.GlTexSubImage2D(GLES20.GlTexture2d, 0, elementRects[i].X, elementRects[i].Y, bmp.Width, bmp.Height, GLES20.GlRgba, GLES20.GlUnsignedByte, buffer);
            }

            return new TextureAtlas(this, textureId, atlasSizeX, atlasSizeY, names, elementRects, true);
        }

        public Texture CreateTextureFromOffscreenGraphics(OffscreenGraphics g)
        {
            return new Texture(this, g.Texture, g.SizeX, g.SizeY, false, false);
        }

        private T[] CopyResizeArray<T>(T[] array, int size)
        {
            var newArray = new T[size];
            Array.Copy(array, newArray, size);
            return newArray;
        }

        private FloatBuffer GetVtxBuffer(int size)
        {
            var roundedSize = Math.Max(MinBufferSize, Utils.NextPowerOfTwo(size));
            var idx = MaxBufferSizeLog2 - Utils.Log2Int(roundedSize);
            var buffer = (FloatBuffer)null;

            if (freeVtxBuffers[idx].Count == 0)
            {
                buffer = ByteBuffer.AllocateDirect(sizeof(float) * roundedSize).Order(ByteOrder.NativeOrder()).AsFloatBuffer();
            }
            else
            {
                var lastIdx = freeVtxBuffers[idx].Count - 1;
                buffer = freeVtxBuffers[idx][lastIdx];
                freeVtxBuffers[idx].RemoveAt(lastIdx);
            }

            usedVtxBuffers[idx].Add(buffer);
            buffer.Position(0);
            return buffer;
        }

        private IntBuffer GetColBuffer(int size)
        {
            var roundedSize = Math.Max(MinBufferSize, Utils.NextPowerOfTwo(size));
            var idx = MaxBufferSizeLog2 - Utils.Log2Int(roundedSize);
            var buffer = (IntBuffer)null;

            if (freeColBuffers[idx].Count == 0)
            {
                buffer = ByteBuffer.AllocateDirect(sizeof(int) * roundedSize).Order(ByteOrder.NativeOrder()).AsIntBuffer();
            }
            else
            {
                var lastIdx = freeColBuffers[idx].Count - 1;
                buffer = freeColBuffers[idx][lastIdx];
                freeColBuffers[idx].RemoveAt(lastIdx);
            }

            usedColBuffers[idx].Add(buffer);
            buffer.Position(0);
            return buffer;
        }

        private ByteBuffer GetByteBuffer(int size)
        {
            var roundedSize = Math.Max(MinBufferSize, Utils.NextPowerOfTwo(size));
            var idx = MaxBufferSizeLog2 - Utils.Log2Int(roundedSize);
            var buffer = (ByteBuffer)null;

            if (freeBytBuffers[idx].Count == 0)
            {
                buffer = ByteBuffer.AllocateDirect(sizeof(byte) * roundedSize).Order(ByteOrder.NativeOrder());
            }
            else
            {
                var lastIdx = freeBytBuffers[idx].Count - 1;
                buffer = freeBytBuffers[idx][lastIdx];
                freeBytBuffers[idx].RemoveAt(lastIdx);
            }

            usedBytBuffers[idx].Add(buffer);
            buffer.Position(0);
            return buffer;
        }

        private ShortBuffer GetIdxBuffer(int size)
        {
            var roundedSize = Math.Max(MinBufferSize, Utils.NextPowerOfTwo(size));
            var idx = MaxBufferSizeLog2 - Utils.Log2Int(roundedSize);
            var buffer = (ShortBuffer)null;

            if (freeIdxBuffers[idx].Count == 0)
            {
                buffer = ByteBuffer.AllocateDirect(sizeof(short) * roundedSize).Order(ByteOrder.NativeOrder()).AsShortBuffer();
            }
            else
            {
                var lastIdx = freeIdxBuffers[idx].Count - 1;
                buffer = freeIdxBuffers[idx][lastIdx];
                freeIdxBuffers[idx].RemoveAt(lastIdx);
            }

            usedIdxBuffers[idx].Add(buffer);
            buffer.Position(0);
            return buffer;
        }

        // TODO : Review this whole thing with ES 2.0. Might be different.
        private FloatBuffer CopyGetVtxBuffer(float[] array, int size)
        {
            var newArray = new float[size];
            Array.Copy(array, newArray, size);
            var buffer = GetVtxBuffer(size);
            buffer.Put(newArray);
            buffer.Position(0);
            return buffer;
        }

        private IntBuffer CopyGetColBuffer(int[] array, int size)
        {
            var newArray = new int[size];
            Array.Copy(array, newArray, size);
            var buffer = GetColBuffer(size);
            buffer.Put(newArray);
            buffer.Position(0);
            return buffer;
        }

        private ByteBuffer CopyGetByteBuffer(byte[] array, int size)
        {
            var newArray = new byte[size];
            Array.Copy(array, newArray, size);
            var buffer = GetByteBuffer(size);
            buffer.Put(newArray);
            buffer.Position(0);
            return buffer;
        }

        private ShortBuffer CopyGetIdxBuffer(short[] array, int size)
        {
            var newArray = new short[size];
            Array.Copy(array, newArray, size);
            var buffer = GetIdxBuffer(size);
            buffer.Put(newArray);
            buffer.Position(0);
            return buffer;
        }

        private void BindAndUpdateVertexBuffer(int attrib, float[] array, int arraySize, int numComponents = 2)
        {
            var vb = CopyGetVtxBuffer(array, arraySize);
            GLES20.GlEnableVertexAttribArray(attrib);
            GLES20.GlVertexAttribPointer(attrib, numComponents, GLES20.GlFloat, false, 0, vb);
        }

        private void BindAndUpdateColorBuffer(int attrib, int[] array, int arraySize)
        {
            var cb = CopyGetColBuffer(array, arraySize);
            GLES20.GlEnableVertexAttribArray(attrib);
            GLES20.GlVertexAttribPointer(attrib, 4, GLES20.GlUnsignedByte, true, 0, cb);
        }

        private void BindAndUpdateByteBuffer(int attrib, byte[] array, int arraySize, bool signed = false, bool normalized = true)
        {
            var bb = CopyGetByteBuffer(array, arraySize);
            GLES20.GlEnableVertexAttribArray(attrib);
            GLES20.GlVertexAttribPointer(attrib, 1, signed ? GLES20.GlByte : GLES20.GlUnsignedByte, normalized, 0, bb);
        }

        protected override void DrawCommandList(CommandList list, bool depthTest)
        {
            if (list == null)
                return;

            if (list.HasAnything)
            {
                GLES20.GlDepthFunc(depthTest ? GLES20.GlEqual : GLES20.GlAlways);

                if (list.HasAnyPolygons)
                {
                    var draws = list.GetPolygonDrawData();

                    GLES20.GlUseProgram(polyProgram);
                    GLES20.GlUniform4fv(polyScaleBiasUniform, 1, viewportScaleBias, 0);
                    GLES20.GlUniform1f(polyDashScaleUniform, 1.0f / dashSize);

                    foreach (var draw in draws)
                    { 
                        BindAndUpdateVertexBuffer(0, draw.vtxArray, draw.vtxArraySize);
                        BindAndUpdateColorBuffer(1, draw.colArray, draw.colArraySize);
                        BindAndUpdateByteBuffer(2, draw.dshArray, draw.dshArraySize, false, false);
                        BindAndUpdateByteBuffer(3, draw.depArray, draw.depArraySize, true);

                        GLES20.GlDrawElements(GLES20.GlTriangles, draw.numIndices, GLES20.GlUnsignedShort, CopyGetIdxBuffer(draw.idxArray, draw.idxArraySize));
                    }
                }

                if (list.HasAnyLines)
                {
                    var draws = list.GetLineDrawData();

                    GLES20.GlUseProgram(lineProgram);
                    GLES20.GlUniform4fv(lineScaleBiasUniform, 1, viewportScaleBias, 0);
                    GLES20.GlUniform1f(lineDashScaleUniform, 1.0f / dashSize);

                    foreach (var draw in draws)
                    {
                        BindAndUpdateVertexBuffer(0, draw.vtxArray, draw.vtxArraySize);
                        BindAndUpdateColorBuffer(1, draw.colArray, draw.colArraySize);
                        BindAndUpdateByteBuffer(2, draw.dshArray, draw.dshArraySize, false, false);
                        BindAndUpdateByteBuffer(3, draw.depArray, draw.depArraySize, true);

                        GLES20.GlDrawArrays(GLES20.GlLines, 0, draw.numVertices);
                    }
                }

                if (list.HasAnySmoothLines)
                {
                    var draws = list.GetSmoothLineDrawData();

                    GLES20.GlUseProgram(lineSmoothProgram);
                    GLES20.GlUniform4fv(lineSmoothScaleBiasUniform, 1, viewportScaleBias, 0);
                    GLES20.GlUniform2f(lineSmoothWindowSizeUniform, screenRect.Width, screenRect.Height);

                    foreach (var draw in draws)
                    { 
                        BindAndUpdateVertexBuffer(0, draw.vtxArray, draw.vtxArraySize);
                        BindAndUpdateColorBuffer(1, draw.colArray, draw.colArraySize);
                        BindAndUpdateByteBuffer(2, draw.dstArray, draw.dstArraySize);
                        BindAndUpdateByteBuffer(3, draw.depArray, draw.depArraySize, true);

                        GLES20.GlDrawElements(GLES20.GlTriangles, draw.numIndices, GLES20.GlUnsignedShort, CopyGetIdxBuffer(draw.idxArray, draw.idxArraySize));
                    }
                }

                if (list.HasAnyTextures)
                {
                    var drawDatas = list.GetTextureDrawData();

                    GLES20.GlUseProgram(bmpProgram);
                    GLES20.GlUniform4fv(bmpScaleBiasUniform, 1, viewportScaleBias, 0);
                    GLES20.GlUniform1i(bmpTextureUniform, 0);
                    GLES20.GlActiveTexture(GLES20.GlTexture0 + 0);

                    foreach (var drawData in drawDatas)
                    {
                        BindAndUpdateVertexBuffer(0, drawData.vtxArray, drawData.vtxArraySize);
                        BindAndUpdateColorBuffer(1, drawData.colArray, drawData.colArraySize);
                        BindAndUpdateVertexBuffer(2, drawData.texArray, drawData.texArraySize, 3);
                        BindAndUpdateByteBuffer(3, drawData.depArray, drawData.depArraySize, true);

                        foreach (var draw in drawData.draws)
                        {
                            quadIdxBuffer.Position(draw.start);
                            GLES20.GlBindTexture(GLES20.GlTexture2d, draw.textureId);
                            GLES20.GlDrawElements(GLES20.GlTriangles, draw.count, GLES20.GlUnsignedShort, quadIdxBuffer);
                        }

                        // TODO : Change this so that we build the draw data as we draw stuff, like the other primitives. This way
                        // we wont need to do this janky release.
                        drawData.Release(this);
                    }
                }

                if (list.HasAnyTexts)
                {
                    var drawData = list.GetTextDrawData(vtxArray, texArray, colArray, depArray, out var vtxSize, out var texSize, out var colSize, out var depSize);

                    GLES20.GlUseProgram(textProgram);
                    GLES20.GlUniform4fv(bmpScaleBiasUniform, 1, viewportScaleBias, 0);
                    GLES20.GlUniform1i(bmpTextureUniform, 0);
                    GLES20.GlActiveTexture(GLES20.GlTexture0 + 0);

                    BindAndUpdateVertexBuffer(0, vtxArray, vtxSize);
                    BindAndUpdateColorBuffer(1, colArray, colSize);
                    BindAndUpdateVertexBuffer(2, texArray, texSize);
                    BindAndUpdateByteBuffer(3, depArray, depSize, true);

                    foreach (var draw in drawData)
                    {
                        quadIdxBuffer.Position(draw.start);
                        GLES20.GlBindTexture(GLES20.GlTexture2d, draw.textureId);
                        GLES20.GlDrawElements(GLES20.GlTriangles, draw.count, GLES20.GlUnsignedShort, quadIdxBuffer);
                    }
                }
            }
        }
    }

    public class OffscreenGraphics : Graphics
    {
        protected int fbo;
        protected int texture;
        protected int depth;
        protected int resX;
        protected int resY;

        protected ByteBuffer readbackBuffer;

        public int Texture => texture;
        public int SizeX => resX;
        public int SizeY => resY;

        private OffscreenGraphics(int imageSizeX, int imageSizeY, bool renderToBackBuffer, bool filter) : base(true)
        {
            resX = imageSizeX;
            resY = imageSizeY;

            // When renderToBackBuffer = true, we dont create any resources, its all in the current context.
            if (!renderToBackBuffer)
            {
                texture = CreateTexture(imageSizeX, imageSizeY, TextureFormat.Rgba, filter);
                depth   = CreateTexture(imageSizeX, imageSizeY, TextureFormat.Depth, false);

                var fbos = new int[1];
                GLES20.GlGenFramebuffers(1, fbos, 0);
                fbo = fbos[0];

                GLES20.GlBindFramebuffer(GLES20.GlFramebuffer, fbo);
                GLES20.GlFramebufferTexture2D(GLES20.GlFramebuffer, GLES20.GlColorAttachment0, GLES20.GlTexture2d, texture, 0);
                GLES20.GlFramebufferTexture2D(GLES20.GlFramebuffer, GLES20.GlDepthAttachment, GLES20.GlTexture2d, depth, 0);
                GLES20.GlBindFramebuffer(GLES20.GlFramebuffer, 0);
            }
        }

        public static OffscreenGraphics Create(int imageSizeX, int imageSizeY, bool renderToBackBuffer, bool filter = false)
        {
#if !DEBUG
            try
#endif
            {   
                return new OffscreenGraphics(imageSizeX, imageSizeY, renderToBackBuffer, filter);
            }
#if !DEBUG
            catch
            {
            }
#endif

            return null;
        }

        public override void BeginDrawFrame(Rectangle rect, bool clear, Color color)
        {
            if (fbo > 0)
                GLES20.GlBindFramebuffer(GLES20.GlFramebuffer, fbo);

            base.BeginDrawFrame(rect, clear, color);
        }

        public override void EndDrawFrame(bool clearAlpha = false)
        {
            base.EndDrawFrame(clearAlpha);

            if (fbo > 0)
                GLES20.GlBindFramebuffer(GLES20.GlFramebuffer, 0);
        }

        public Texture GetTexture()
        {
            return new Texture(this, texture, resX, resY, false);
        }

        public unsafe void GetBitmap(byte[] data)
        {
            if (readbackBuffer == null)
            {
                readbackBuffer = ByteBuffer.AllocateDirect(resX * resY * sizeof(int));
            }

            readbackBuffer.Rewind();

            GLES20.GlBindFramebuffer(GLES20.GlFramebuffer, fbo);
            GLES20.GlReadPixels(0, 0, resX, resY, GLES20.GlRgba, GLES20.GlUnsignedByte, readbackBuffer);
            GLES20.GlBindFramebuffer(GLES20.GlFramebuffer, 0);

            readbackBuffer.Rewind();
            readbackBuffer.Get(data);
        }

        public override void Dispose()
        {
            if (texture != 0) GLES20.GlDeleteTextures(2, new[] { texture, depth }, 0);
            if (fbo     != 0) GLES20.GlDeleteFramebuffers(1, new[] { fbo }, 0);

            Utils.DisposeAndNullify(ref readbackBuffer);
            base.Dispose();
        }
    }}