using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Android.Opengl;
using Android.Runtime;
using Java.Nio;
using Javax.Microedition.Khronos.Opengles;

using Bitmap = Android.Graphics.Bitmap;

namespace FamiStudio
{
    public class Graphics : GraphicsBase
    {
        private int polyProgram;
        private int polyScaleBiasUniform;

        private int lineProgram;
        private int lineScaleBiasUniform;
        private int lineDashTextureUniform;

        private int lineSmoothProgram;
        private int lineSmoothScaleBiasUniform;
        private int lineSmoothWindowSizeUniform;

        private int bmpProgram;
        private int bmpScaleBiasUniform;
        private int bmpTextureUniform;

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

        public Graphics()
        {
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

            dashedBitmap = CreateBitmapFromResource("Dash");
            GLES20.GlTexParameteri(GLES20.GlTexture2d, GLES20.GlTextureWrapS, GLES20.GlRepeat);
            GLES20.GlTexParameteri(GLES20.GlTexture2d, GLES20.GlTextureWrapT, GLES20.GlRepeat);
        }

        private int CompileShader(string resourceName, int type, out List<string> attributes)
        {
            var code = "";
            using (Stream stream = typeof(GraphicsBase).Assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                code = reader.ReadToEnd();
            }

            if (type == GLES20.GlVertexShader)
            {
                attributes = new List<string>();
                
                var lines = code.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var l in lines)
                {
                    if (l.StartsWith("attribute "))
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
            polyProgram = CompileAndLinkProgram("FamiStudio.Resources.Shaders.Droid.Poly" );            
            polyScaleBiasUniform = GLES20.GlGetUniformLocation(polyProgram, "screenScaleBias");

            lineProgram = CompileAndLinkProgram("FamiStudio.Resources.Shaders.Droid.Line");
            lineScaleBiasUniform = GLES20.GlGetUniformLocation(lineProgram, "screenScaleBias");
            lineDashTextureUniform = GLES20.GlGetUniformLocation(lineProgram, "dashTexture");

            lineSmoothProgram = CompileAndLinkProgram("FamiStudio.Resources.Shaders.Droid.LineSmooth");
            lineSmoothScaleBiasUniform = GLES20.GlGetUniformLocation(lineSmoothProgram, "screenScaleBias");
            lineSmoothWindowSizeUniform = GLES20.GlGetUniformLocation(lineSmoothProgram, "windowSize");

            bmpProgram = CompileAndLinkProgram("FamiStudio.Resources.Shaders.Droid.Bitmap");
            bmpScaleBiasUniform = GLES20.GlGetUniformLocation(bmpProgram, "screenScaleBias");
            bmpTextureUniform = GLES20.GlGetUniformLocation(bmpProgram, "tex");

            depthProgram = CompileAndLinkProgram("FamiStudio.Resources.Shaders.Droid.Depth");
            depthScaleBiasUniform = GLES20.GlGetUniformLocation(depthProgram, "screenScaleBias");
        }

        public override void BeginDrawFrame(Rectangle rect, Color clear)
        {
            base.BeginDrawFrame(rect, clear);

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

        protected override void Clear()
        {
            GLES20.GlViewport(screenRectFlip.Left, screenRectFlip.Top, screenRectFlip.Width, screenRectFlip.Height);
            GLES20.GlDisable((int)2884); // Cull face?
            GLES20.GlBlendFunc(GLES20.GlSrcAlpha, GLES20.GlOneMinusSrcAlpha);
            GLES20.GlEnable(GLES20.GlBlend);
            GLES20.GlEnable(GLES20.GlDepthTest);
            GLES20.GlDepthFunc(GLES20.GlAlways);
            GLES20.GlDisable(GLES20.GlStencilTest);
            GLES20.GlDisable(GLES20.GlScissorTest);

            GLES20.GlClearColor(clearColor.R / 255.0f, clearColor.G / 255.0f, clearColor.B / 255.0f, clearColor.A / 255.0f);
            GLES20.GlClear(GLES20.GlColorBufferBit | GLES20.GlDepthBufferBit);
        }

        protected override void DrawDepthPrepass()
        {
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
            GLES20.GlUniform4fv(depthScaleBiasUniform, 1, viewportScaleBias, 0); // MATTT : Wrong, needs full screen!

            BindAndUpdateVertexBuffer(0, vtxArray, vtxIdx);
            BindAndUpdateByteBuffer(1, depArray, depIdx, true);

            quadIdxBuffer.Position(0);
            GLES20.GlDrawElements(GLES20.GlTriangles, vtxIdx / 8 * 6, GLES20.GlUnsignedShort, quadIdxBuffer);

            GLES20.GlDepthFunc(GLES20.GlEqual);
            GLES20.GlColorMask(true, true, true, true);
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

        public void UpdateBitmap(Bitmap bmp, int x, int y, int width, int height, int[] data)
        {
            var buffer = ByteBuffer.AllocateDirect(width * height * sizeof(int)).Order(ByteOrder.NativeOrder()).AsIntBuffer();
            buffer.Put(data);
            buffer.Position(0);

            GLES20.GlBindTexture(GLES20.GlTexture2d, bmp.Id);
            GLES20.GlTexSubImage2D(GLES20.GlTexture2d, 0, x, y, width, height, GLES20.GlRgba, GLES20.GlUnsignedByte, buffer);
        }

        protected override int CreateEmptyTexture(int width, int height, bool alpha, bool filter)
        {
            var id = new int[1];
            GLES20.GlGenTextures(1, id, 0);
            GLES20.GlBindTexture(GLES20.GlTexture2d, id[0]);
            GLES20.GlTexParameteri(GLES20.GlTexture2d, GLES20.GlTextureMinFilter, filter ? GLES20.GlLinear : GLES20.GlNearest);
            GLES20.GlTexParameteri(GLES20.GlTexture2d, GLES20.GlTextureMagFilter, filter ? GLES20.GlLinear : GLES20.GlNearest);
            GLES20.GlTexParameteri(GLES20.GlTexture2d, GLES20.GlTextureWrapS, GLES20.GlClampToEdge);
            GLES20.GlTexParameteri(GLES20.GlTexture2d, GLES20.GlTextureWrapT, GLES20.GlClampToEdge);

            var buffer = ByteBuffer.AllocateDirect(width * height * sizeof(int)).Order(ByteOrder.NativeOrder()).AsIntBuffer();
            buffer.Put(new int[width * height]);
            buffer.Position(0);

            GLES20.GlTexImage2D(GLES20.GlTexture2d, 0, GLES20.GlRgba, width, height, 0, GLES20.GlRgba, GLES20.GlUnsignedByte, buffer);

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

            if (DpiScaling.Window >= 4.0f && assembly.GetManifestResourceInfo($"FamiStudio.Resources.{name}@4x.tga") != null)
            {
                return $"FamiStudio.Resources.{name}@4x.tga";
            }
            else if (DpiScaling.Window >= 2.0f && assembly.GetManifestResourceInfo($"FamiStudio.Resources.{name}@2x.tga") != null)
            {
                return $"FamiStudio.Resources.{name}@2x.tga";
            }
            else
            {
                return $"FamiStudio.Resources.{name}.tga";
            }
        }

        public Bitmap CreateBitmapFromResource(string name)
        {
            var bmp = LoadBitmapFromResourceWithScaling(name);
            return new Bitmap(this, CreateTexture(bmp, true), bmp.Width, bmp.Height, true, true);
        }

        protected override BitmapAtlas CreateBitmapAtlasFromResources(string[] names)
        {
            // Need to sort since we do binary searches on the names.
            Array.Sort(names);

            var bitmaps = new SimpleBitmap[names.Length];
            var elementSizeX = 0;
            var elementSizeY = 0;

            for (int i = 0; i < names.Length; i++)
            {
                var bmp = LoadBitmapFromResourceWithScaling(names[i]);

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

            var textureId = CreateEmptyTexture(atlasSizeX, atlasSizeY, true, true);
            GLES20.GlBindTexture(GLES20.GlTexture2d, textureId);

            Debug.WriteLine($"Creating bitmap atlas of size {atlasSizeX}x{atlasSizeY} with {names.Length} images:");

            for (int i = 0; i < names.Length; i++)
            {
                var bmp = bitmaps[i];
                var buffer = IntBuffer.Wrap(bmp.Data);

                Debug.WriteLine($"  - {names[i]} ({bmp.Width} x {bmp.Height}):");

                GLES20.GlTexSubImage2D(GLES20.GlTexture2d, 0, elementRects[i].X, elementRects[i].Y, bmp.Width, bmp.Height, GLES20.GlRgba, GLES20.GlUnsignedByte, buffer);

                names[i] = names[i].Substring(6); // Remove "Atlas."
            }

            return new BitmapAtlas(this, textureId, atlasSizeX, atlasSizeY, names, elementRects, true);
        }

        public Bitmap CreateBitmapFromOffscreenGraphics(OffscreenGraphics g)
        {
            return new Bitmap(this, g.Texture, g.SizeX, g.SizeY, false, false);
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

        // MATTT : Review this whole thing with ES 2.0. Might be different.
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

        private void BindAndUpdateVertexBuffer(int attrib, float[] array, int arraySize)
        {
            var vb = CopyGetVtxBuffer(array, arraySize);
            GLES20.GlEnableVertexAttribArray(attrib);
            GLES20.GlVertexAttribPointer(attrib, 2, GLES20.GlFloat, false, 0, vb);
        }

        private void BindAndUpdateColorBuffer(int attrib, int[] array, int arraySize)
        {
            var cb = CopyGetColBuffer(array, arraySize);
            GLES20.GlEnableVertexAttribArray(attrib);
            GLES20.GlVertexAttribPointer(attrib, 4, GLES20.GlUnsignedByte, true, 0, cb);
        }

        private void BindAndUpdateByteBuffer(int attrib, byte[] array, int arraySize, bool signed = false)
        {
            var bb = CopyGetByteBuffer(array, arraySize);
            GLES20.GlEnableVertexAttribArray(attrib);
            GLES20.GlVertexAttribPointer(attrib, 1, signed ? GLES20.GlByte : GLES20.GlUnsignedByte, true, 0, bb);
        }

        protected override void DrawCommandList(CommandList list)
        {
            if (list == null)
                return;

            if (list.HasAnything)
            {
                if (list.HasAnyPolygons)
                {
                    var draw = list.GetPolygonDrawData();

                    GLES20.GlUseProgram(polyProgram);
                    GLES20.GlUniform4fv(polyScaleBiasUniform, 1, viewportScaleBias, 0);

                    BindAndUpdateVertexBuffer(0, draw.vtxArray, draw.vtxArraySize);
                    BindAndUpdateColorBuffer(1, draw.colArray, draw.colArraySize);
                    BindAndUpdateByteBuffer(2, draw.depArray, draw.depArraySize, true);

                    GLES20.GlDrawElements(GLES20.GlTriangles, draw.numIndices, GLES20.GlUnsignedShort, CopyGetIdxBuffer(draw.idxArray, draw.idxArraySize));
                }

                if (list.HasAnyLines)
                {
                    var draw = list.GetLineDrawData();

                    GLES20.GlUseProgram(lineProgram);
                    GLES20.GlUniform4fv(lineScaleBiasUniform, 1, viewportScaleBias, 0);
                    GLES20.GlUniform1i(lineDashTextureUniform, 0);
                    GLES20.GlActiveTexture(GLES20.GlTexture0 + 0);
                    GLES20.GlBindTexture(GLES20.GlTexture2d, dashedBitmap.Id);

                    BindAndUpdateVertexBuffer(0, draw.vtxArray, draw.vtxArraySize);
                    BindAndUpdateColorBuffer(1, draw.colArray, draw.colArraySize);
                    BindAndUpdateVertexBuffer(2, draw.texArray, draw.texArraySize);
                    BindAndUpdateByteBuffer(3, draw.depArray, draw.depArraySize, true);

                    GLES20.GlDrawArrays(GLES20.GlLines, 0, draw.numVertices);
                }

                // MATTT : Smooth lines look like ass on Android.
                if (list.HasAnySmoothLines)
                {
                    var draw = list.GetSmoothLineDrawData();

                    GLES20.GlUseProgram(lineSmoothProgram);
                    GLES20.GlUniform4fv(lineSmoothScaleBiasUniform, 1, viewportScaleBias, 0);
                    GLES20.GlUniform2f(lineSmoothWindowSizeUniform, screenRect.Width, screenRect.Height);

                    BindAndUpdateVertexBuffer(0, draw.vtxArray, draw.vtxArraySize);
                    BindAndUpdateColorBuffer(1, draw.colArray, draw.colArraySize);
                    BindAndUpdateByteBuffer(2, draw.dstArray, draw.dstArraySize);
                    BindAndUpdateByteBuffer(3, draw.depArray, draw.depArraySize, true);

                    GLES20.GlDrawElements(GLES20.GlTriangles, draw.numIndices, GLES20.GlUnsignedShort, CopyGetIdxBuffer(draw.idxArray, draw.idxArraySize));
                }

                if (list.HasAnyBitmaps)
                {
                    var drawData = list.GetBitmapDrawData(vtxArray, texArray, colArray, depArray, out var vtxSize, out var texSize, out var colSize, out var depSize, out _);

                    GLES20.GlUseProgram(bmpProgram);
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

                if (list.HasAnyBitmaps)
                {
                    var drawData = list.GetTextDrawData(vtxArray, texArray, colArray, depArray, out var vtxSize, out var texSize, out var colSize, out var depSize, out _);

                    GLES20.GlUseProgram(bmpProgram);
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
        protected int resX;
        protected int resY;

        public int Texture => texture;
        public int SizeX => resX;
        public int SizeY => resY;

        private OffscreenGraphics(int imageSizeX, int imageSizeY, bool allowReadback)
        {
            resX = imageSizeX;
            resY = imageSizeY;

            if (!allowReadback)
            {
                texture = CreateEmptyTexture(imageSizeX, imageSizeY, true, false);

                var fbos = new int[1];
                GLES11Ext.GlGenFramebuffersOES(1, fbos, 0);
                fbo = fbos[0];

                GLES11Ext.GlBindFramebufferOES(GLES11Ext.GlFramebufferOes, fbo);
                GLES11Ext.GlFramebufferTexture2DOES(GLES11Ext.GlFramebufferOes, GLES11Ext.GlColorAttachment0Oes, GLES20.GlTexture2d, texture, 0);
                GLES11Ext.GlBindFramebufferOES(GLES11Ext.GlFramebufferOes, 0);
            }
        }

        public static OffscreenGraphics Create(int imageSizeX, int imageSizeY, bool allowReadback)
        {
#if !DEBUG
            try
#endif
            {
                var extentions = GLES20.GlGetString(GLES20.GlExtensions);

                if (extentions.ToUpper().Contains("GL_OES_FRAMEBUFFER_OBJECT"))
                    return new OffscreenGraphics(imageSizeX, imageSizeY, allowReadback);
            }
#if !DEBUG
            catch
            {
            }
#endif

            return null;
        }

        // MATTT : This is surely wrong, we do all the drawing at end of frame now.
        public override void BeginDrawFrame(Rectangle rect, Color clear)
        {
            if (fbo > 0)
                GLES11Ext.GlBindFramebufferOES(GLES11Ext.GlFramebufferOes, fbo);

            base.BeginDrawFrame(rect, clear);
        }

        public override void EndDrawFrame()
        {
            base.EndDrawFrame();

            if (fbo > 0)
                GLES11Ext.GlBindFramebufferOES(GLES11Ext.GlFramebufferOes, 0);
        }

        public unsafe void GetBitmap(byte[] data)
        {
            // Our rendering is fed directly to the encoder on Android.
        }

        public override void Dispose()
        {
            if (texture != 0) GLES20.GlDeleteTextures(1, new[] { texture }, 0);
            if (fbo     != 0) GLES11Ext.GlDeleteFramebuffersOES(1, new[] { fbo }, 0);

            base.Dispose();
        }
    }}