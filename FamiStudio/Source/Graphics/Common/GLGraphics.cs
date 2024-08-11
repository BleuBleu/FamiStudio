using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;

namespace FamiStudio
{
    public class Graphics : GraphicsBase
    {
        private int polyProgram;
        private int polyScaleBiasUniform;
        private int polyDashScaleUniform;
        private int polyVao;

        private int lineProgram;
        private int lineScaleBiasUniform;
        private int lineDashScaleUniform;
        private int lineVao;

        private int lineSmoothProgram;
        private int lineSmoothScaleBiasUniform;
        private int lineSmoothWindowSizeUniform;
        private int lineSmoothVao;

        private int bmpProgram;
        private int bmpScaleBiasUniform;
        private int bmpTextureUniform;
        private int bmpVao;

        private int blurProgram;
        private int blurScaleBiasUniform;
        private int blurKernelUniform;
        private int blurCocBiasScaleUniform;
        private int blurTextureUniform;
        private int blurVao;

        private int textProgram;
        private int textScaleBiasUniform;
        private int textTextureUniform;
        private int textVao;

        private int depthProgram;
        private int depthScaleBiasUniform;
        private int depthVao;

        private int vertexBuffer;
        private int colorBuffer;
        private int centerBuffer;
        private int dashBuffer;
        private int texCoordBuffer;
        private int lineDistBuffer;
        private int indexBuffer;
        private int depthBuffer;
        private int quadIdxBuffer;

        public Graphics(bool offscreen = false) : base(offscreen) 
        {
            Debug.Assert(GL.Initialized);

#if DEBUG
            if (GL.DebugMessageCallback != null)
            {
                GL.DebugMessageCallback(GLDebugMessageCallback, IntPtr.Zero);
            }
#endif

            polyVao       = GL.GenVertexArray();
            lineVao       = GL.GenVertexArray();
            lineSmoothVao = GL.GenVertexArray();
            bmpVao        = GL.GenVertexArray();
            blurVao       = GL.GenVertexArray();
            textVao       = GL.GenVertexArray();
            depthVao      = GL.GenVertexArray();

            vertexBuffer   = GL.GenBuffer();
            colorBuffer    = GL.GenBuffer();
            centerBuffer   = GL.GenBuffer();
            dashBuffer     = GL.GenBuffer();
            texCoordBuffer = GL.GenBuffer();
            lineDistBuffer = GL.GenBuffer();
            indexBuffer    = GL.GenBuffer();
            depthBuffer    = GL.GenBuffer();

            quadIdxBuffer = GL.GenBuffer();
            GL.BindBuffer(GL.ElementArrayBuffer, quadIdxBuffer);
            GL.BufferData(GL.ElementArrayBuffer, quadIdxArray, quadIdxArray.Length, GL.StaticDraw);

            InitializeShaders();

            GL.GetInteger(GL.MaxTextureSize, ref maxTextureSize);
        }

        public override void Dispose()
        {
            base.Dispose();
            GL.DeleteBuffer(quadIdxBuffer);
            GL.DeleteBuffer(vertexBuffer);
            GL.DeleteBuffer(colorBuffer);
            GL.DeleteBuffer(centerBuffer);
            GL.DeleteBuffer(dashBuffer);
            GL.DeleteBuffer(texCoordBuffer);
            GL.DeleteBuffer(lineDistBuffer);
            GL.DeleteBuffer(indexBuffer);
            GL.DeleteBuffer(depthBuffer);
            GL.DeleteVertexArray(polyVao);
            GL.DeleteVertexArray(lineVao);
            GL.DeleteVertexArray(lineSmoothVao);
            GL.DeleteVertexArray(bmpVao);
            GL.DeleteVertexArray(blurVao);
            GL.DeleteVertexArray(textVao);
            GL.DeleteVertexArray(depthVao);
            GL.DeleteProgram(polyProgram);
            GL.DeleteProgram(lineProgram);
            GL.DeleteProgram(lineSmoothProgram);
            GL.DeleteProgram(bmpProgram);
            GL.DeleteProgram(blurProgram);
            GL.DeleteProgram(textProgram);
            GL.DeleteProgram(depthProgram);
        }

        private int CompileShader(string resourceName, int type, out List<string> attributes)
        {
            var code = "";
            using (Stream stream = typeof(GraphicsBase).Assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                code = reader.ReadToEnd();
            }

            if (type == GL.VertexShader)
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

            var glslVersion = "300 es";
            var noPerspective = "";
            var precision = type == GL.VertexShader ? "precision highp float;" : "precision mediump float;";
            
            if (!GL.IsOpenGLES3)
            {
                var versionMajor = 0;
                var versionMinor = 0;

                GL.GetInteger(GL.MajorVersion, ref versionMajor);
                GL.GetInteger(GL.MinorVersion, ref versionMinor);

                // Linux can report 4.1, even though we asked for a 3.3 core context.
                if (versionMajor > 3)
                {
                    versionMajor = 3;
                    versionMinor = 3;
                }

                switch (versionMinor)
                {
                    case 0:
                        glslVersion = $"130";
                        break;
                    case 1:
                        glslVersion = $"140";
                        break;
                    case 2:
                        glslVersion = $"150";
                        break;
                    default:
                        glslVersion = $"{versionMajor}{versionMinor}0 core";
                        break;
                }

                precision = "";
                noPerspective = "noperspective ";
            }

            var shader = GL.CreateShader(type);
            var source = new []
            {
                $"#version {glslVersion}\n",
                $"{precision}\n",
                type == GL.FragmentShader ? "layout(location = 0) out vec4 outColor;\n" : "\n",
                "#define ATTRIB_IN in\n",
                $"#define INTERP_IN {noPerspective}in\n",
                $"#define INTERP_OUT {noPerspective}out\n",
                "#define INTERP_PERSPECTIVE_IN in\n",
                "#define INTERP_PERSPECTIVE_OUT out\n",
                "#define TEX texture\n",
                "#define TEXPROJ textureProj\n",
                $"#define FAMISTUDIO_MOBILE {(GL.IsOpenGLES3 ? 1 : 0)}\n",
                "#define FRAG_COLOR outColor\n",
                "#line 1\n",
                code
            };

            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);

            var status = GL.GetShaderInt(shader, GL.CompileStatus);

            if (status == 0)
            {
                var log = GL.GetShaderInfoLog(shader);
                Debug.WriteLine(resourceName);
                Debug.WriteLine(log);
                Debug.Assert(false);
                return 0;
            }

            return shader;
        }

        private int CompileAndLinkProgram(string resourceName, bool useFragment = true)
        {
            var program = GL.CreateProgram();

            var vert = CompileShader(resourceName + ".vert", GL.VertexShader, out var attributes);
            GL.AttachShader(program, vert);

            // Fragment shaders are optional, but MacOS misbehave when there is
            // none bound. I don't trust exotic GL implementations, so we force
            // a fragment shader on all platforms but Windows.
        #if FAMISTUDIO_WINDOWS
            if (useFragment)
        #endif
            { 
                var frag = CompileShader(resourceName + ".frag", GL.FragmentShader, out _);
                GL.AttachShader(program, frag);
            }

            for (int i = 0; i < attributes.Count; i++)
            {
                GL.BindAttribLocation(program, i, attributes[i]);
            }

            GL.LinkProgram(program);
            
            var status = GL.GetProgramInt(program, GL.LinkStatus);

			if (status == 0)
			{
                var log = GL.GetProgramInfoLog(program);
                Debug.WriteLine(resourceName);
                Debug.WriteLine(log);
                Debug.Assert(false);
                return 0;
            }

            GL.UseProgram(program);

            return program;
        }

        private void InitializeShaders()
        {
            polyProgram = CompileAndLinkProgram("FamiStudio.Resources.Shaders.Poly");
            polyScaleBiasUniform = GL.GetUniformLocation(polyProgram, "screenScaleBias");
            polyDashScaleUniform = GL.GetUniformLocation(polyProgram, "dashScale");

            lineProgram = CompileAndLinkProgram("FamiStudio.Resources.Shaders.Line");
            lineScaleBiasUniform = GL.GetUniformLocation(lineProgram, "screenScaleBias");
            lineDashScaleUniform = GL.GetUniformLocation(lineProgram, "dashScale");

            lineSmoothProgram = CompileAndLinkProgram("FamiStudio.Resources.Shaders.LineSmooth");
            lineSmoothScaleBiasUniform = GL.GetUniformLocation(lineSmoothProgram, "screenScaleBias");
            lineSmoothWindowSizeUniform = GL.GetUniformLocation(lineSmoothProgram, "windowSize");

            bmpProgram = CompileAndLinkProgram("FamiStudio.Resources.Shaders.Bitmap");
            bmpScaleBiasUniform = GL.GetUniformLocation(bmpProgram, "screenScaleBias");
            bmpTextureUniform = GL.GetUniformLocation(bmpProgram, "tex");

            blurProgram = CompileAndLinkProgram("FamiStudio.Resources.Shaders.Blur");
            blurScaleBiasUniform = GL.GetUniformLocation(blurProgram, "screenScaleBias");
            blurKernelUniform = GL.GetUniformLocation(blurProgram, "blurKernel");
            blurCocBiasScaleUniform = GL.GetUniformLocation(blurProgram, "blurCocBiasScale");
            blurTextureUniform = GL.GetUniformLocation(blurProgram, "tex");

            textProgram = CompileAndLinkProgram("FamiStudio.Resources.Shaders.Text");
            textScaleBiasUniform = GL.GetUniformLocation(textProgram, "screenScaleBias");
            textTextureUniform = GL.GetUniformLocation(textProgram, "tex");

            depthProgram = CompileAndLinkProgram("FamiStudio.Resources.Shaders.Depth", false);
            depthScaleBiasUniform = GL.GetUniformLocation(depthProgram, "screenScaleBias");
        }

        protected override void Initialize(bool clear, Color clearColor)
        {
            if (!GL.IsOpenGLES3)
            {
                GL.PolygonMode(GL.FrontAndBack, GL.Fill);
            }

            GL.Viewport(screenRectFlip.Left, screenRectFlip.Top, screenRectFlip.Width, screenRectFlip.Height);
            GL.Disable(GL.CullFace);
            GL.BlendFunc(GL.SrcAlpha, GL.OneMinusSrcAlpha);
            GL.Enable(GL.Blend);
            GL.Enable(GL.DepthTest);
            GL.DepthFunc(GL.Always);
            GL.Disable(GL.StencilTest);
            GL.Disable(GL.ScissorTest);

            if (clear)
            {
                GL.ClearDepth(0.0f);
                GL.ClearColor(clearColor.R / 255.0f, clearColor.G / 255.0f, clearColor.B / 255.0f, clearColor.A / 255.0f);
                GL.Clear(GL.ColorBufferBit | GL.DepthBufferBit);
            }
        }


        public void DrawBlur(int textureId, int x, int y, int width, int height, int blurStartY, float blurScale = 2.0f)
        {
            var kernel = GetBlurKernel(width, height, blurScale);

            MakeQuad(x, y, width, height);

            GL.PushDebugGroup("Blur");
            GL.DepthFunc(GL.Always);
            GL.UseProgram(blurProgram);
            GL.BindVertexArray(blurVao);
            GL.Uniform(blurScaleBiasUniform, viewportScaleBias, 4);
            GL.Uniform(blurTextureUniform, 0);
            GL.Uniform(blurKernelUniform, kernel, 4, kernel.Length / 4);
            GL.Uniform(blurCocBiasScaleUniform, -(height - blurStartY) / (float)height, height / (float)blurStartY);
            GL.ActiveTexture(GL.Texture0 + 0);
            GL.BindTexture(GL.Texture2D, textureId);

            BindAndUpdateVertexBuffer(0, vertexBuffer, vtxArray, 8);
            BindAndUpdateVertexBuffer(1, texCoordBuffer, texArray, 8);
            GL.BindBuffer(GL.ElementArrayBuffer, quadIdxBuffer);
            GL.DrawElements(GL.Triangles, 6, GL.UnsignedShort, IntPtr.Zero);
            GL.DepthFunc(GL.Equal);
            GL.PopDebugGroup();
        }

        protected unsafe override bool DrawDepthPrepass()
        {
            if (clipRegions.Count == 0)
            {
                GL.DepthFunc(GL.Always);
                GL.ColorMask(true, true, true, true);
                return false;
            }

            GL.PushDebugGroup("Depth Pre-pass");
            GL.DepthMask(1);
            GL.DepthFunc(GL.Always);
            GL.ColorMask(false, false, false, false);

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

            GL.UseProgram(depthProgram);
            GL.BindVertexArray(depthVao);
            GL.Uniform(depthScaleBiasUniform, viewportScaleBias, 4);

            BindAndUpdateVertexBuffer(0, vertexBuffer, vtxArray, vtxIdx);
            BindAndUpdateByteBuffer(1, depthBuffer, depArray, depIdx, true);
            GL.BindBuffer(GL.ElementArrayBuffer, quadIdxBuffer);
            GL.DrawElements(GL.Triangles, vtxIdx / 8 * 6, GL.UnsignedShort, IntPtr.Zero);

            GL.DepthMask(0);
            GL.DepthFunc(GL.Equal);
            GL.ColorMask(true, true, true, true);
            GL.PopDebugGroup();

            return true;
        }

        protected override void ClearAlpha()
        {
            // Normally we would simply use seperate alpha blending to keep the alpha
            // to 1.0 all the time. But we are limiting ourselves to OpenGL 3.3 for now.
            GL.PushDebugGroup("Clear Alpha");
            GL.ColorMask(false, false, false, true);
            GL.UseProgram(polyProgram);
            GL.BindVertexArray(polyVao);
            GL.Uniform(polyScaleBiasUniform, viewportScaleBias, 4);
            GL.Uniform(polyDashScaleUniform, 0.0f);
            GL.DepthFunc(GL.Always);

            MakeFullScreenTriangle();
            BindAndUpdateVertexBuffer(0, vertexBuffer, vtxArray, 6);
            BindAndUpdateColorBuffer(1, colorBuffer, colArray, 3);
            BindAndUpdateByteBuffer(2, dashBuffer, depArray, 3, false, false); // Irrelevant
            BindAndUpdateByteBuffer(3, depthBuffer, depArray, 3, true); // Unused
            BindAndUpdateIndexBuffer(indexBuffer, idxArray, 3);

            GL.DrawElements(GL.Triangles, 3, GL.UnsignedShort, IntPtr.Zero);
            GL.ColorMask(true, true, true, true);
            GL.PopDebugGroup();
        }

        public void UpdateTexture(Texture bmp, int x, int y, int width, int height, byte[] data, TextureFormat format)
        {
            var glFormat = GetGLTextureFormat(format, out var bpp);
            Debug.Assert(data.Length == width * height * bpp);

            GL.BindTexture(GL.Texture2D, bmp.Id);
            GL.TexSubImage2D(GL.Texture2D, 0, x, y, width, height, glFormat, GL.UnsignedByte, data);
        }

        public void UpdateTexture(Texture bmp, int x, int y, int width, int height, int[] data)
        {
            GL.BindTexture(GL.Texture2D, bmp.Id);
            GL.TexSubImage2D(GL.Texture2D, 0, x, y, width, height, GL.Rgba, GL.UnsignedByte, data);
        }

        public override void UpdateTexture(int id, int x, int y, int width, int height, byte[] data)
        {
            // Only used by fonts, so red channel is assumed here.
            GL.BindTexture(GL.Texture2D, id);
            GL.PixelStore(GL.UnpackAlignment, 1);
            GL.TexSubImage2D(GL.Texture2D, 0, x, y, width, height, GL.Red, GL.UnsignedByte, data);
            GL.PixelStore(GL.UnpackAlignment, 4);
        }

        private int GetGLTextureFormat(TextureFormat format, out int bpp)
        {
            switch (format)
            {
                case TextureFormat.R: bpp = 1; return GL.Red;
                case TextureFormat.Rgb: bpp = 3; return GL.IsOpenGLES3 ? GL.Rgb: GL.Bgr;
                case TextureFormat.Rgba: bpp = 4; return GL.IsOpenGLES3 ? GL.Rgba : GL.Bgra;
                default:
                    Debug.Assert(false);
                    bpp = 4;
                    return GL.Bgra;
            }
        }

        private int GetGLInternalTextureFormat(TextureFormat format)
        {
            switch (format)
            {
                case TextureFormat.R: return GL.R8;
                case TextureFormat.Rgb: return GL.Rgb8;
                case TextureFormat.Rgba: return GL.Rgba8;
                default:
                    Debug.Assert(false);
                    return GL.Rgba8;
            }
        }

        public override int CreateTexture(int width, int height, TextureFormat format, bool filter)
        {
            int id = GL.GenTexture();

            GL.BindTexture(GL.Texture2D, id);
            GL.TexParameter(GL.Texture2D, GL.TextureMinFilter, filter ? GL.Linear : GL.Nearest);
            GL.TexParameter(GL.Texture2D, GL.TextureMagFilter, filter ? GL.Linear : GL.Nearest);
            GL.TexParameter(GL.Texture2D, GL.TextureWrapS, GL.ClampToEdge);
            GL.TexParameter(GL.Texture2D, GL.TextureWrapT, GL.ClampToEdge);
            GL.TexImage2D(GL.Texture2D, 0, GetGLInternalTextureFormat(format), width, height, 0, GetGLTextureFormat(format, out var bpp), GL.UnsignedByte, new byte[width * height * bpp]);

            return id;
        }

        protected unsafe override int CreateTexture(SimpleBitmap bmp, bool filter)
        {
            fixed (int* ptr = &bmp.Data[0])
            {
                var stride = sizeof(int) * bmp.Width;

                int id = GL.GenTexture();
                GL.BindTexture(GL.Texture2D, id);
                GL.TexImage2D(GL.Texture2D, 0, GL.Rgba8, bmp.Width, bmp.Height, 0, GL.Rgba, GL.UnsignedByte, new IntPtr(ptr));
                GL.TexParameter(GL.Texture2D, GL.TextureMinFilter, GL.Nearest);
                GL.TexParameter(GL.Texture2D, GL.TextureMagFilter, GL.Nearest);

                return id;
            }
        }

        public override void DeleteTexture(int id)
        {
            GL.DeleteTexture(id);
        }

        protected override string GetScaledFilename(string name, out bool needsScaling)
        {
            var assembly = Assembly.GetExecutingAssembly();

            needsScaling = false;

            if (Platform.IsMobile)
            {
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
            else
            {
                if (DpiScaling.Window == 1.5f && assembly.GetManifestResourceInfo($"{name}@15x.tga") != null)
                {
                    return $"{name}@15x.tga";
                }
                else if (DpiScaling.Window > 1.0f && assembly.GetManifestResourceInfo($"{name}@2x.tga") != null)
                {
                    needsScaling = DpiScaling.Window != 2.0f;
                    return $"{name}@2x.tga";
                }
                else
                {
                    return $"{name}.tga";
                }
            }
        }

        public Texture CreateTextureFromResource(string name)
        {
            var bmp = LoadBitmapFromResourceWithScaling(name);
            return new Texture(this, CreateTexture(bmp, false), bmp.Width, bmp.Height);
        }

        protected unsafe override TextureAtlas CreateTextureAtlasFromResources(string[] names)
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

            var textureId = CreateTexture(atlasSizeX, atlasSizeY, TextureFormat.Rgba, false);
            GL.BindTexture(GL.Texture2D, textureId);

            Debug.WriteLine($"Creating bitmap atlas of size {atlasSizeX}x{atlasSizeY} with {names.Length} images:");

            for (int i = 0; i < names.Length; i++)
            {
                var bmp = bitmaps[i];

                //Debug.WriteLine($"  - {names[i]} ({bmp.Width} x {bmp.Height}):");

                fixed (int* ptr = &bmp.Data[0])
                {
                    var stride = sizeof(int) * bmp.Width;
                    GL.TexSubImage2D(GL.Texture2D, 0, elementRects[i].X, elementRects[i].Y, bmp.Width, bmp.Height, GL.Rgba, GL.UnsignedByte, new IntPtr(ptr));
                }
            }

            return new TextureAtlas(this, textureId, atlasSizeX, atlasSizeY, names, elementRects);
        }

        private void BindAndUpdateVertexBuffer(int attrib, int buffer, float[] array, int arraySize, int numComponents = 2)
        {
            GL.BindBuffer(GL.ArrayBuffer, buffer);
            GL.BufferData(GL.ArrayBuffer, array, arraySize, GL.DynamicDraw);
            GL.EnableVertexAttribArray(attrib);
            GL.VertexAttribPointer(attrib, numComponents, GL.Float, false, 0);
        }

        private void BindAndUpdateColorBuffer(int attrib, int buffer, int[] array, int arraySize)
        {
            GL.BindBuffer(GL.ArrayBuffer, buffer);
            GL.BufferData(GL.ArrayBuffer, array, arraySize, GL.DynamicDraw);
            GL.EnableVertexAttribArray(attrib);
            GL.VertexAttribPointer(attrib, 4, GL.UnsignedByte, true, 0);
        }

        private void BindAndUpdateByteBuffer(int attrib, int buffer, byte[] array, int arraySize, bool signed = false, bool normalized = true)
        {
            GL.BindBuffer(GL.ArrayBuffer, buffer);
            GL.BufferData(GL.ArrayBuffer, array, arraySize, GL.DynamicDraw);
            GL.EnableVertexAttribArray(attrib);
            GL.VertexAttribPointer(attrib, 1, signed ? GL.Byte : GL.UnsignedByte, normalized, 0);
        }

        private void BindAndUpdateIndexBuffer(int buffer, short[] array, int arraySize)
        {
            GL.BindBuffer(GL.ElementArrayBuffer, buffer);
            GL.BufferData(GL.ElementArrayBuffer, array, arraySize, GL.DynamicDraw);
        }

        protected override void DrawCommandList(CommandList list, bool depthTest)
        {
            if (list == null)
                return;

            if (list.HasAnything)
            {
                GL.PushDebugGroup("Draw Command List");
                GL.DepthFunc(depthTest ? GL.Equal : GL.Always);

                if (list.HasAnyPolygons)
                {
                    var draws = list.GetPolygonDrawData();

                    GL.UseProgram(polyProgram);
                    GL.BindVertexArray(polyVao);
                    GL.Uniform(polyScaleBiasUniform, viewportScaleBias, 4);
                    GL.Uniform(polyDashScaleUniform, 1.0f / dashSize);

                    foreach (var draw in draws)
                    { 
                        BindAndUpdateVertexBuffer(0, vertexBuffer, draw.vtxArray, draw.vtxArraySize);
                        BindAndUpdateColorBuffer(1, colorBuffer, draw.colArray, draw.colArraySize);
                        if (Platform.IsMobile)
                        {
                            // We dont use thick dashed line on desktop.
                            BindAndUpdateByteBuffer(2, dashBuffer, draw.dshArray, draw.dshArraySize, false, false);
                        }
                        BindAndUpdateByteBuffer(3, depthBuffer, draw.depArray, draw.depArraySize, true);
                        BindAndUpdateIndexBuffer(indexBuffer,  draw.idxArray, draw.idxArraySize);

                        GL.DrawElements(GL.Triangles, draw.numIndices, GL.UnsignedShort, IntPtr.Zero);
                    }
                }

                if (list.HasAnyLines)
                {
                    var draws = list.GetLineDrawData();

                    GL.UseProgram(lineProgram);
                    GL.BindVertexArray(lineVao);
                    GL.Uniform(lineScaleBiasUniform, viewportScaleBias, 4);
                    GL.Uniform(lineDashScaleUniform, 1.0f / dashSize);

                    foreach (var draw in draws)
                    {
                        BindAndUpdateVertexBuffer(0, vertexBuffer, draw.vtxArray, draw.vtxArraySize);
                        BindAndUpdateColorBuffer(1, colorBuffer, draw.colArray, draw.colArraySize);
                        BindAndUpdateByteBuffer(2, dashBuffer, draw.dshArray, draw.dshArraySize, false, false);
                        BindAndUpdateByteBuffer(3, depthBuffer, draw.depArray, draw.depArraySize, true);

                        GL.DrawArrays(GL.Lines, 0, draw.numVertices);
                    }
                }

                if (list.HasAnySmoothLines)
                {
                    var draws = list.GetSmoothLineDrawData();

                    GL.UseProgram(lineSmoothProgram);
                    GL.BindVertexArray(lineSmoothVao);
                    GL.Uniform(lineSmoothScaleBiasUniform, viewportScaleBias, 4);
                    GL.Uniform(lineSmoothWindowSizeUniform, screenRect.Width, screenRect.Height);

                    foreach (var draw in draws)
                    { 
                        BindAndUpdateVertexBuffer(0, vertexBuffer, draw.vtxArray, draw.vtxArraySize);
                        BindAndUpdateColorBuffer(1, colorBuffer, draw.colArray, draw.colArraySize);
                        BindAndUpdateByteBuffer(2, lineDistBuffer, draw.dstArray, draw.dstArraySize);
                        BindAndUpdateByteBuffer(3, depthBuffer, draw.depArray, draw.depArraySize, true);
                        BindAndUpdateIndexBuffer(indexBuffer, draw.idxArray, draw.idxArraySize);
                        GL.DrawElements(GL.Triangles, draw.numIndices, GL.UnsignedShort, IntPtr.Zero);
                    }
                }

                if (list.HasAnyTextures)
                {
                    var drawDatas = list.GetTextureDrawData();

                    GL.UseProgram(bmpProgram);
                    GL.BindVertexArray(bmpVao);
                    GL.Uniform(bmpScaleBiasUniform, viewportScaleBias, 4);
                    GL.Uniform(bmpTextureUniform, 0);
                    GL.ActiveTexture(GL.Texture0 + 0);
                    GL.BindBuffer(GL.ElementArrayBuffer, quadIdxBuffer);

                    foreach (var drawData in drawDatas)
                    {
                        BindAndUpdateVertexBuffer(0, vertexBuffer, drawData.vtxArray, drawData.vtxArraySize);
                        BindAndUpdateColorBuffer(1, colorBuffer, drawData.colArray, drawData.colArraySize);
                        BindAndUpdateVertexBuffer(2, texCoordBuffer, drawData.texArray, drawData.texArraySize, 3);
                        BindAndUpdateByteBuffer(3, depthBuffer, drawData.depArray, drawData.depArraySize, true);

                        foreach (var draw in drawData.drawCalls)
                        {
                            GL.BindTexture(GL.Texture2D, draw.textureId);
                            GL.DrawElements(GL.Triangles, draw.count, GL.UnsignedShort, new IntPtr(draw.start * sizeof(short)));
                        }

                        // TODO : Change this so that we build the draw data as we draw stuff, like the other primitives. This way
                        // we wont need to do this janky release.
                        drawData.Release(this);
                    }
                }

                if (list.HasAnyTexts)
                {
                    var drawDatas = list.GetTextDrawData();

                    GL.UseProgram(textProgram);
                    GL.BindVertexArray(textVao);
                    GL.Uniform(textScaleBiasUniform, viewportScaleBias, 4);
                    GL.Uniform(textTextureUniform, 0);
                    GL.ActiveTexture(GL.Texture0 + 0);
                    GL.BindBuffer(GL.ElementArrayBuffer, quadIdxBuffer);

                    foreach (var drawData in drawDatas)
                    {
                        BindAndUpdateVertexBuffer(0, vertexBuffer, drawData.vtxArray, drawData.vtxArraySize);
                        BindAndUpdateColorBuffer(1, colorBuffer, drawData.colArray, drawData.colArraySize);
                        BindAndUpdateVertexBuffer(2, texCoordBuffer, drawData.texArray, drawData.texArraySize);
                        BindAndUpdateByteBuffer(3, depthBuffer, drawData.depArray, drawData.depArraySize, true);

                        foreach (var draw in drawData.drawCalls)
                        {
                            GL.BindTexture(GL.Texture2D, draw.textureId);
                            GL.DrawElements(GL.Triangles, draw.count, GL.UnsignedShort, new IntPtr(draw.start * sizeof(short)));
                        }

                        // TODO : Change this so that we build the draw data as we draw stuff, like the other primitives. This way
                        // we wont need to do this janky release.
                        drawData.Release(this);
                    }
                }

                GL.PopDebugGroup();
            }
        }

#if DEBUG
        static void GLDebugMessageCallback(int source, int type, int id, int severity, int length, string message, IntPtr userParam)
        {
            if (severity != GL.DebugSeverityNotification)
            {
                Debug.WriteLine(message);
                Debug.Assert(severity != GL.DebugSeverityHigh);
            }
        }
#endif
    }
    public class OffscreenGraphics : Graphics
    {
        protected int fbo;
        protected int texture;
        protected int depth;
        protected int resX;
        protected int resY;

        public int Texture => texture;
        public int SizeX => resX;
        public int SizeY => resY;

        private OffscreenGraphics(int imageSizeX, int imageSizeY, bool renderToBackBuffer, bool filter) : base(true) 
        {
            resX = imageSizeX;
            resY = imageSizeY;

            texture = GL.GenTexture();
            GL.BindTexture(GL.Texture2D, texture);
            GL.TexImage2D(GL.Texture2D, 0, GL.Rgba, imageSizeX, imageSizeY, 0, GL.Rgba, GL.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(GL.Texture2D, GL.TextureMinFilter, filter ? GL.Linear : GL.Nearest); 
            GL.TexParameter(GL.Texture2D, GL.TextureMagFilter, filter ? GL.Linear : GL.Nearest); 
            GL.TexParameter(GL.Texture2D, GL.TextureMaxAnisotropy, filter ? 8 : 1);
            GL.TexParameter(GL.Texture2D, GL.TextureWrapS, GL.ClampToEdge);
            GL.TexParameter(GL.Texture2D, GL.TextureWrapT, GL.ClampToEdge);

            depth = GL.GenTexture();
            GL.BindTexture(GL.Texture2D, depth);
            GL.TexImage2D(GL.Texture2D, 0, GL.DepthComponent, imageSizeX, imageSizeY, 0, GL.DepthComponent, GL.UnsignedShort, IntPtr.Zero);
            GL.TexParameter(GL.Texture2D, GL.TextureMinFilter, GL.Nearest);
            GL.TexParameter(GL.Texture2D, GL.TextureMagFilter, GL.Nearest);
            GL.TexParameter(GL.Texture2D, GL.TextureWrapS, GL.ClampToEdge);
            GL.TexParameter(GL.Texture2D, GL.TextureWrapT, GL.ClampToEdge);

            fbo = GL.GenFramebuffer();
            GL.BindFramebuffer(GL.Framebuffer, fbo);
            GL.FramebufferTexture2D(GL.Framebuffer, GL.ColorAttachment0, GL.Texture2D, texture, 0);
            GL.FramebufferTexture2D(GL.Framebuffer, GL.DepthAttachment, GL.Texture2D, depth, 0);
            GL.BindFramebuffer(GL.Framebuffer, 0);
        }

        public static OffscreenGraphics Create(int imageSizeX, int imageSizeY, bool renderToBackBuffer, bool filter = false)
        {
            return new OffscreenGraphics(imageSizeX, imageSizeY, renderToBackBuffer, filter);
        }

        public override void BeginDrawFrame(Rectangle rect, bool clear, Color color)
        {
            GL.BindFramebuffer(GL.DrawFramebuffer, fbo);
            GL.DrawBuffer(GL.ColorAttachment0);

            base.BeginDrawFrame(rect, clear, color);
        }

        public override void EndDrawFrame(bool clearAlpha = false)
        {
            base.EndDrawFrame(clearAlpha);

            // This fixes some graphical corruption on my intel laptop. 
            GL.Flush();
            GL.BindFramebuffer(GL.DrawFramebuffer, 0);
        }

        public Texture GetTexture()
        {
            return new Texture(this, texture, resX, resY, false);
        }

        public unsafe void GetBitmap(byte[] data)
        {
            byte[] tmp = new byte[data.Length];

            GL.BindFramebuffer(GL.ReadFramebuffer, fbo);
            fixed (byte* tmpPtr = &tmp[0])
            {
                GL.ReadPixels(0, 0, resX, resY, GL.Rgba, GL.UnsignedByte, new IntPtr(tmpPtr));
                GL.BindFramebuffer(GL.ReadFramebuffer, 0);

                // Flip image vertically to match D3D. 
                for (int y = 0; y < resY; y++)
                {
                    int y0 = y;
                    int y1 = resY - y - 1;

                    y0 *= resX * 4;
                    y1 *= resX * 4;

                    // ABGR -> RGBA
                    byte* p = tmpPtr + y0;
                    for (int x = 0; x < resX * 4; x += 4)
                    {
                        data[y1 + x + 1] = *p++;
                        data[y1 + x + 2] = *p++;
                        data[y1 + x + 3] = *p++;
                        data[y1 + x + 0] = *p++;
                    }
                }
            }
        }

        public override void Dispose()
        {
            if (texture != 0) GL.DeleteTexture(texture);
            if (depth != 0)   GL.DeleteTexture(depth);
            if (fbo != 0)     GL.DeleteFramebuffer(fbo);

            base.Dispose();
        }
    };

    public static class GL
    {
        private static bool initialized;
        private static bool glES3;
    #if DEBUG && !FAMISTUDIO_MACOS
        private static bool renderdoc;
#endif

        public static bool Initialized => initialized;
        public static bool IsOpenGLES3 => glES3;

        public const int DepthBufferBit            = 0x0100;
        public const int ColorBufferBit            = 0x4000;
        public const int Texture2D                 = 0x0DE1;
        public const int Modelview                 = 0x1700;
        public const int Projection                = 0x1701;
        public const int CullFace                  = 0x0B44;
        public const int VertexArray               = 0x8074;
        public const int TextureWrapS              = 0x2802;
        public const int TextureWrapT              = 0x2803;
        public const int Clamp                     = 0x2900;
        public const int Repeat                    = 0x2901;
        public const int ClampToBorder             = 0x812D;
        public const int ClampToEdge               = 0x812F;
        public const int SrcAlpha                  = 0x0302;
        public const int OneMinusSrcAlpha          = 0x0303;
        public const int Blend                     = 0x0BE2;
        public const int DepthTest                 = 0x0B71;
        public const int StencilTest               = 0x0B90;
        public const int ScissorTest               = 0x0C11;
        public const int Red                       = 0x1903;
        public const int R8                        = 0x8229;
        public const int Bgr                       = 0x80E0;
        public const int Bgra                      = 0x80E1;
        public const int Rgb                       = 0x1907;
        public const int Rgba                      = 0x1908;
        public const int Byte                      = 0x1400;
        public const int UnsignedByte              = 0x1401;
        public const int TextureMagFilter          = 0x2800;
        public const int TextureMinFilter          = 0x2801;
        public const int Nearest                   = 0x2600;
        public const int Linear                    = 0x2601;
        public const int Rgb8                      = 0x8051;
        public const int Rgba8                     = 0x8058;
        public const int ColorArray                = 0x8076;
        public const int TextureCoordArray         = 0x8078;
        public const int LineSmooth                = 0x0B20;
        public const int PolygonSmooth             = 0x0B41;
        public const int Float                     = 0x1406;
        public const int Lines                     = 0x0001;
        public const int Triangles                 = 0x0004;
        public const int Quads                     = 0x0007;
        public const int UnsignedShort             = 0x1403;
        public const int Framebuffer               = 0x8D40;
        public const int ColorAttachment0          = 0x8CE0;
        public const int DepthAttachment           = 0x8D00;
        public const int DrawFramebuffer           = 0x8CA9;
        public const int ReadFramebuffer           = 0x8CA8;
        public const int LineWidthRange            = 0x0B22;
        public const int DebugSeverityHigh         = 0x9146;
        public const int DebugSeverityLow          = 0x9148;
        public const int DebugSeverityMedium       = 0x9147;
        public const int DebugSeverityNotification = 0x826B;
        public const int VertexShader              = 0x8B31;
        public const int FragmentShader            = 0x8B30;
        public const int CompileStatus             = 0x8B81;
        public const int LinkStatus                = 0x8B82;
        public const int ActiveUniforms            = 0x8B86;
        public const int DynamicDraw               = 0x88E8;
        public const int StaticDraw                = 0x88E4;
        public const int ArrayBuffer               = 0x8892;
        public const int ElementArrayBuffer        = 0x8893;
        public const int Nicest                    = 0x1102;
        public const int LineSmoothHint            = 0x0C52;
        public const int Texture0                  = 0x84C0;
        public const int Always                    = 0x0207;
        public const int Equal                     = 0x0202;
        public const int DebugSourceApplication    = 0x824A;
        public const int FrontAndBack              = 0x0408;
        public const int Fill                      = 0x1B02;
        public const int DepthComponent            = 0x1902;
        public const int UnpackAlignment           = 0x0CF5;
        public const int MaxTextureSize            = 0x0D33;
        public const int MajorVersion              = 0x821B;
        public const int MinorVersion              = 0x821C;
        public const int TextureMaxAnisotropy      = 0x84FE;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void DebugCallback(int source, int type, int id, int severity, int length, [MarshalAs(UnmanagedType.LPStr)] string message, IntPtr userParam);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate int  CheckFramebufferStatusDelegate(int target);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate int  CreateProgramDelegate();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate int  CreateShaderDelegate(int shaderType);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate int  GetErrorDelegate();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate int  GetUniformLocationDelegate(int program, [MarshalAs(UnmanagedType.LPStr)] string name);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void ActiveTextureDelegate(int texture);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void AttachShaderDelegate(int program, int shader);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void BindAttribLocationDelegate(int program, int index, [MarshalAs(UnmanagedType.LPStr)] string name);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void BindBufferDelegate(int target, int buffer);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void BindFramebufferDelegate(int target, int framebuffer);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void BindTextureDelegate(int target, int id);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void BindVertexArrayDelegate(int vao);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void BlendFuncDelegate(int src, int dst);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void BufferDataDelegate(int target, IntPtr size, IntPtr data, int usage);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void BufferSubDataDelegate(int target, IntPtr offset, IntPtr size, IntPtr data);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void ClearColorDelegate(float red, float green, float blue, float alpha);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void ClearDelegate(uint mask);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void ClearDepthDelegate(float depth);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void ColorMaskDelegate(byte red, byte green, byte blue, byte alpha);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void ColorPointerDelegate(int size, int type, int stride, IntPtr pointer);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void CompileShaderDelegate(int shader);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void DebugMessageCallbackDelegate([MarshalAs(UnmanagedType.FunctionPtr)] DebugCallback callback, IntPtr userParam);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void DeleteBuffersDelegate(int n, IntPtr buffers);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void DeleteFramebuffersDelegate(int n, IntPtr framebuffers);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void DeleteProgramDelegate(int id);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void DeleteTexturesDelegate(int n, IntPtr textures);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void DeleteVertexArraysDelegate(int n, IntPtr arrays);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void DepthFuncDelegate(int func);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void DepthMaskDelegate(int func);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void DisableClientStateDelegate(int cap);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void DisableDelegate(int cap);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void DrawArraysDelegate(int mode, int first, int count);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void DrawBufferDelegate(int buf);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void DrawElementsDelegate(int mode, int count, int type, IntPtr indices);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void EnableClientStateDelegate(int cap);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void EnableDelegate(int cap);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void EnableVertexAttribArrayDelegate(int index);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void FlushDelegate();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void FramebufferTexture2DDelegate(int target,int attachment, int textarget, int texture, int level);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void GenBuffersDelegate(int n, IntPtr buffers);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void GenFramebuffersDelegate(int n, IntPtr indices);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void GenTexturesDelegate(int n, IntPtr textures);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void GenVertexArraysDelegate(int n, IntPtr arrays);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void GetFloatDelegate(int par3am, IntPtr floats);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void GetIntegerDelegate(int name, ref int data);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void GetProgramInfoLogDelegate(int shader, int maxLength, ref int length, IntPtr infoLog);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void GetProgramIntDelegate(int program, int param, IntPtr ints);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void GetShaderInfoLogDelegate(int shader, int maxLength, ref int length, IntPtr infoLog);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void GetShaderIntDelegate(int shader, int param, IntPtr ints);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void HintDelegate(int target, int mode);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void LineWidthDelegate(float width);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void LinkProgramDelegate(int program);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void PixelStoreDelegate(int name, int value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void PolygonModeDelegate(int face, int mode);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void PopDebugGroupDelegate();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void PushDebugGroupDelegate(int source, int id, int length, [MarshalAs(UnmanagedType.LPStr)] string message);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void ReadPixelsDelegate(int x, int y, int width, int height, int format, int type, IntPtr data);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void ScissorDelegate(int x, int y, int width, int height);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void ShaderSourceDelegate(int shader, int count, IntPtr strings, IntPtr length);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void TexCoordPointerDelegate(int size, int type, int stride, IntPtr pointer);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void TexImage2DDelegate(int target, int level, int internalformat, int width, int height, int border, int format, int type, IntPtr data);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void TexParameterDelegate(int target, int name, int val);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void TexSubImage2DDelegate(int target, int level, int xoffset, int yoffset, int width, int height, int format, int type, IntPtr pixels);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void Uniform1fDelegate(int location, float x);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void Uniform1fvDelegate(int location, int count, IntPtr values);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void Uniform1iDelegate(int location, int x);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void Uniform2fDelegate(int location, float x, float y);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void Uniform2fvDelegate(int location, int count, IntPtr values);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void Uniform3fDelegate(int location, float x, float y, float z);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void Uniform3fvDelegate(int location, int count, IntPtr values);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void Uniform4fDelegate(int location, float x, float y, float z, float w);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void Uniform4fvDelegate(int location, int count, IntPtr values);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void UseProgramDelegate(int program);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void VertexAttribIPointerDelegate(int index, int size, int type, int stride, IntPtr pointer);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void VertexAttribPointerDelegate(int index, int size, int type, byte normalized, int stride, IntPtr pointer);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void VertexPointerDelegate(int size, int type, int stride, IntPtr pointer);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void ViewportDelegate(int left, int top, int width, int height);

        public static ActiveTextureDelegate           ActiveTexture;
        public static AttachShaderDelegate            AttachShader;
        public static BindAttribLocationDelegate      BindAttribLocation;
        public static BindBufferDelegate              BindBuffer;
        public static BindFramebufferDelegate         BindFramebuffer;
        public static BindTextureDelegate             BindTexture;
        public static BindVertexArrayDelegate         BindVertexArray;
        public static BlendFuncDelegate               BlendFunc;
        public static BufferDataDelegate              BufferDataRaw;
        public static BufferSubDataDelegate           BufferSubDataRaw;
        public static CheckFramebufferStatusDelegate  CheckFramebufferStatus;
        public static ClearColorDelegate              ClearColor;
        public static ClearDelegate                   Clear;
        public static ClearDepthDelegate              ClearDepth;
        public static ColorMaskDelegate               ColorMaskRaw;
        public static ColorPointerDelegate            ColorPointerRaw;
        public static CompileShaderDelegate           CompileShader;
        public static CreateProgramDelegate           CreateProgram;
        public static CreateShaderDelegate            CreateShader;
        public static DebugMessageCallbackDelegate    DebugMessageCallback;
        public static DeleteBuffersDelegate           DeleteBuffersRaw;
        public static DeleteFramebuffersDelegate      DeleteFramebuffers;
        public static DeleteProgramDelegate           DeleteProgram;
        public static DeleteTexturesDelegate          DeleteTextures;
        public static DeleteVertexArraysDelegate      DeleteVertexArraysRaw;
        public static DepthFuncDelegate               DepthFunc;
        public static DepthMaskDelegate               DepthMask;
        public static DisableClientStateDelegate      DisableClientState;
        public static DisableDelegate                 Disable;
        public static DrawArraysDelegate              DrawArrays;
        public static DrawBufferDelegate              DrawBuffer;
        public static DrawElementsDelegate            DrawElementsRaw;
        public static EnableClientStateDelegate       EnableClientState;
        public static EnableDelegate                  Enable;
        public static EnableVertexAttribArrayDelegate EnableVertexAttribArray;
        public static FlushDelegate                   Flush;
        public static FramebufferTexture2DDelegate    FramebufferTexture2D;
        public static GenBuffersDelegate              GenBuffersRaw;
        public static GenFramebuffersDelegate         GenFramebuffers;
        public static GenTexturesDelegate             GenTextures;
        public static GenVertexArraysDelegate         GenVertexArraysRaw;
        public static GetErrorDelegate                GetError;
        public static GetFloatDelegate                GetFloatRaw;
        public static GetIntegerDelegate              GetInteger;
        public static GetProgramInfoLogDelegate       GetProgramInfoLogRaw;
        public static GetProgramIntDelegate           GetProgramIntRaw;
        public static GetShaderInfoLogDelegate        GetShaderInfoLogRaw;
        public static GetShaderIntDelegate            GetShaderIntRaw;
        public static GetUniformLocationDelegate      GetUniformLocation;
        public static HintDelegate                    Hint;
        public static LineWidthDelegate               LineWidth;
        public static LinkProgramDelegate             LinkProgram;
        public static PixelStoreDelegate              PixelStore;
        public static PolygonModeDelegate             PolygonMode;
        public static PopDebugGroupDelegate           PopDebugGroupRaw;
        public static PushDebugGroupDelegate          PushDebugGroupRaw;
        public static ReadPixelsDelegate              ReadPixels;
        public static ScissorDelegate                 Scissor;
        public static ShaderSourceDelegate            ShaderSourceRaw;
        public static TexCoordPointerDelegate         TexCoordPointerRaw;
        public static TexImage2DDelegate              TexImage2DRaw;
        public static TexParameterDelegate            TexParameter;
        public static TexSubImage2DDelegate           TexSubImage2DRaw;
        public static Uniform1fDelegate               Uniform1fRaw;
        public static Uniform1fvDelegate              Uniform1fvRaw;
        public static Uniform1iDelegate               Uniform1iRaw;
        public static Uniform2fDelegate               Uniform2fRaw;
        public static Uniform2fvDelegate              Uniform2fvRaw;
        public static Uniform3fDelegate               Uniform3fRaw;
        public static Uniform3fvDelegate              Uniform3fvRaw;
        public static Uniform4fDelegate               Uniform4fRaw;
        public static Uniform4fvDelegate              Uniform4fvRaw;
        public static UseProgramDelegate              UseProgram;
        public static VertexAttribIPointerDelegate    VertexAttribIPointerRaw;
        public static VertexAttribPointerDelegate     VertexAttribPointerRaw;
        public static VertexPointerDelegate           VertexPointerRaw;
        public static ViewportDelegate                Viewport;

        public static void StaticInitialize(Func<string, IntPtr> GetProcAddress, bool es3)
        {
            if (initialized)
                return;

            ActiveTexture           = Marshal.GetDelegateForFunctionPointer<ActiveTextureDelegate>(GetProcAddress("glActiveTexture"));
            AttachShader            = Marshal.GetDelegateForFunctionPointer<AttachShaderDelegate>(GetProcAddress("glAttachShader"));
            BindAttribLocation      = Marshal.GetDelegateForFunctionPointer<BindAttribLocationDelegate>(GetProcAddress("glBindAttribLocation"));
            BindBuffer              = Marshal.GetDelegateForFunctionPointer<BindBufferDelegate>(GetProcAddress("glBindBuffer"));
            BindFramebuffer         = Marshal.GetDelegateForFunctionPointer<BindFramebufferDelegate>(GetProcAddress("glBindFramebuffer"));
            BindTexture             = Marshal.GetDelegateForFunctionPointer<BindTextureDelegate>(GetProcAddress("glBindTexture"));
            BindVertexArray         = Marshal.GetDelegateForFunctionPointer<BindVertexArrayDelegate>(GetProcAddress("glBindVertexArray"));
            BlendFunc               = Marshal.GetDelegateForFunctionPointer<BlendFuncDelegate>(GetProcAddress("glBlendFunc"));
            BufferDataRaw           = Marshal.GetDelegateForFunctionPointer<BufferDataDelegate>(GetProcAddress("glBufferData"));
            BufferSubDataRaw        = Marshal.GetDelegateForFunctionPointer<BufferSubDataDelegate>(GetProcAddress("glBufferSubData"));
            CheckFramebufferStatus  = Marshal.GetDelegateForFunctionPointer<CheckFramebufferStatusDelegate>(GetProcAddress("glCheckFramebufferStatus"));
            Clear                   = Marshal.GetDelegateForFunctionPointer<ClearDelegate>(GetProcAddress("glClear"));
            ClearColor              = Marshal.GetDelegateForFunctionPointer<ClearColorDelegate>(GetProcAddress("glClearColor"));
            ClearDepth              = Marshal.GetDelegateForFunctionPointer<ClearDepthDelegate>(GetProcAddress(es3 ? "glClearDepthf" : "glClearDepth"));
            ColorMaskRaw            = Marshal.GetDelegateForFunctionPointer<ColorMaskDelegate>(GetProcAddress("glColorMask"));
            ColorPointerRaw         = Marshal.GetDelegateForFunctionPointer<ColorPointerDelegate>(GetProcAddress("glColorPointer"));
            CompileShader           = Marshal.GetDelegateForFunctionPointer<CompileShaderDelegate>(GetProcAddress("glCompileShader"));
            CreateProgram           = Marshal.GetDelegateForFunctionPointer<CreateProgramDelegate>(GetProcAddress("glCreateProgram"));
            CreateShader            = Marshal.GetDelegateForFunctionPointer<CreateShaderDelegate>(GetProcAddress("glCreateShader"));
            DeleteBuffersRaw        = Marshal.GetDelegateForFunctionPointer<DeleteBuffersDelegate>(GetProcAddress("glDeleteBuffers"));
            DeleteFramebuffers      = Marshal.GetDelegateForFunctionPointer<DeleteFramebuffersDelegate>(GetProcAddress("glDeleteFramebuffers"));
            DeleteProgram           = Marshal.GetDelegateForFunctionPointer<DeleteProgramDelegate>(GetProcAddress("glDeleteProgram"));
            DeleteTextures          = Marshal.GetDelegateForFunctionPointer<DeleteTexturesDelegate>(GetProcAddress("glDeleteTextures"));
            DeleteVertexArraysRaw   = Marshal.GetDelegateForFunctionPointer<DeleteVertexArraysDelegate>(GetProcAddress("glDeleteVertexArrays"));
            DepthFunc               = Marshal.GetDelegateForFunctionPointer<DepthFuncDelegate>(GetProcAddress("glDepthFunc"));
            DepthMask               = Marshal.GetDelegateForFunctionPointer<DepthMaskDelegate>(GetProcAddress("glDepthMask"));
            Disable                 = Marshal.GetDelegateForFunctionPointer<DisableDelegate>(GetProcAddress("glDisable"));
            DisableClientState      = Marshal.GetDelegateForFunctionPointer<DisableClientStateDelegate>(GetProcAddress("glDisableClientState"));
            DrawArrays              = Marshal.GetDelegateForFunctionPointer<DrawArraysDelegate>(GetProcAddress("glDrawArrays"));
            DrawElementsRaw         = Marshal.GetDelegateForFunctionPointer<DrawElementsDelegate>(GetProcAddress("glDrawElements"));
            Enable                  = Marshal.GetDelegateForFunctionPointer<EnableDelegate>(GetProcAddress("glEnable"));
            EnableClientState       = Marshal.GetDelegateForFunctionPointer<EnableClientStateDelegate>(GetProcAddress("glEnableClientState"));
            EnableVertexAttribArray = Marshal.GetDelegateForFunctionPointer<EnableVertexAttribArrayDelegate>(GetProcAddress("glEnableVertexAttribArray"));
            Flush                   = Marshal.GetDelegateForFunctionPointer<FlushDelegate>(GetProcAddress("glFlush"));
            FramebufferTexture2D    = Marshal.GetDelegateForFunctionPointer<FramebufferTexture2DDelegate>(GetProcAddress("glFramebufferTexture2D"));
            GenBuffersRaw           = Marshal.GetDelegateForFunctionPointer<GenBuffersDelegate>(GetProcAddress("glGenBuffers"));
            GenFramebuffers         = Marshal.GetDelegateForFunctionPointer<GenFramebuffersDelegate>(GetProcAddress("glGenFramebuffers"));
            GenTextures             = Marshal.GetDelegateForFunctionPointer<GenTexturesDelegate>(GetProcAddress("glGenTextures"));
            GenVertexArraysRaw      = Marshal.GetDelegateForFunctionPointer<GenVertexArraysDelegate>(GetProcAddress("glGenVertexArrays"));
            GetError                = Marshal.GetDelegateForFunctionPointer<GetErrorDelegate>(GetProcAddress("glGetError"));
            GetFloatRaw             = Marshal.GetDelegateForFunctionPointer<GetFloatDelegate>(GetProcAddress("glGetFloatv"));
            GetInteger              = Marshal.GetDelegateForFunctionPointer<GetIntegerDelegate>(GetProcAddress("glGetIntegerv"));
            GetProgramInfoLogRaw    = Marshal.GetDelegateForFunctionPointer<GetProgramInfoLogDelegate>(GetProcAddress("glGetProgramInfoLog"));
            GetProgramIntRaw        = Marshal.GetDelegateForFunctionPointer<GetProgramIntDelegate>(GetProcAddress("glGetProgramiv"));
            GetShaderInfoLogRaw     = Marshal.GetDelegateForFunctionPointer<GetShaderInfoLogDelegate>(GetProcAddress("glGetShaderInfoLog"));
            GetShaderIntRaw         = Marshal.GetDelegateForFunctionPointer<GetShaderIntDelegate>(GetProcAddress("glGetShaderiv"));
            GetUniformLocation      = Marshal.GetDelegateForFunctionPointer<GetUniformLocationDelegate>(GetProcAddress("glGetUniformLocation"));
            Hint                    = Marshal.GetDelegateForFunctionPointer<HintDelegate>(GetProcAddress("glHint"));
            LineWidth               = Marshal.GetDelegateForFunctionPointer<LineWidthDelegate>(GetProcAddress("glLineWidth"));
            LinkProgram             = Marshal.GetDelegateForFunctionPointer<LinkProgramDelegate>(GetProcAddress("glLinkProgram"));
            PixelStore              = Marshal.GetDelegateForFunctionPointer<PixelStoreDelegate>(GetProcAddress("glPixelStorei"));
            ReadPixels              = Marshal.GetDelegateForFunctionPointer<ReadPixelsDelegate>(GetProcAddress("glReadPixels"));
            Scissor                 = Marshal.GetDelegateForFunctionPointer<ScissorDelegate>(GetProcAddress("glScissor"));
            ShaderSourceRaw         = Marshal.GetDelegateForFunctionPointer<ShaderSourceDelegate>(GetProcAddress("glShaderSource"));
            TexCoordPointerRaw      = Marshal.GetDelegateForFunctionPointer<TexCoordPointerDelegate>(GetProcAddress("glTexCoordPointer"));
            TexImage2DRaw           = Marshal.GetDelegateForFunctionPointer<TexImage2DDelegate>(GetProcAddress("glTexImage2D"));
            TexParameter            = Marshal.GetDelegateForFunctionPointer<TexParameterDelegate>(GetProcAddress("glTexParameteri"));
            TexSubImage2DRaw        = Marshal.GetDelegateForFunctionPointer<TexSubImage2DDelegate>(GetProcAddress("glTexSubImage2D"));
            Uniform1fRaw            = Marshal.GetDelegateForFunctionPointer<Uniform1fDelegate>(GetProcAddress("glUniform1f"));
            Uniform1fvRaw           = Marshal.GetDelegateForFunctionPointer<Uniform1fvDelegate>(GetProcAddress("glUniform1fv"));
            Uniform1iRaw            = Marshal.GetDelegateForFunctionPointer<Uniform1iDelegate>(GetProcAddress("glUniform1i"));
            Uniform2fRaw            = Marshal.GetDelegateForFunctionPointer<Uniform2fDelegate>(GetProcAddress("glUniform2f"));
            Uniform2fvRaw           = Marshal.GetDelegateForFunctionPointer<Uniform2fvDelegate>(GetProcAddress("glUniform2fv"));
            Uniform3fRaw            = Marshal.GetDelegateForFunctionPointer<Uniform3fDelegate>(GetProcAddress("glUniform3f"));
            Uniform3fvRaw           = Marshal.GetDelegateForFunctionPointer<Uniform3fvDelegate>(GetProcAddress("glUniform3fv"));
            Uniform4fRaw            = Marshal.GetDelegateForFunctionPointer<Uniform4fDelegate>(GetProcAddress("glUniform4f"));
            Uniform4fvRaw           = Marshal.GetDelegateForFunctionPointer<Uniform4fvDelegate>(GetProcAddress("glUniform4fv"));
            UseProgram              = Marshal.GetDelegateForFunctionPointer<UseProgramDelegate>(GetProcAddress("glUseProgram"));
            VertexAttribIPointerRaw = Marshal.GetDelegateForFunctionPointer<VertexAttribIPointerDelegate>(GetProcAddress("glVertexAttribIPointer"));
            VertexAttribPointerRaw  = Marshal.GetDelegateForFunctionPointer<VertexAttribPointerDelegate>(GetProcAddress("glVertexAttribPointer"));
            VertexPointerRaw        = Marshal.GetDelegateForFunctionPointer<VertexPointerDelegate>(GetProcAddress("glVertexPointer"));
            Viewport                = Marshal.GetDelegateForFunctionPointer<ViewportDelegate>(GetProcAddress("glViewport"));

            if (!es3)
            {
                DrawBuffer  = Marshal.GetDelegateForFunctionPointer<DrawBufferDelegate>(GetProcAddress("glDrawBuffer"));
                PolygonMode = Marshal.GetDelegateForFunctionPointer<PolygonModeDelegate>(GetProcAddress("glPolygonMode"));
            }
#if DEBUG
            if (!Platform.IsMacOS)
            {
                PushDebugGroupRaw    = Marshal.GetDelegateForFunctionPointer<PushDebugGroupDelegate>(GetProcAddress("glPushDebugGroupKHR"));
                PopDebugGroupRaw     = Marshal.GetDelegateForFunctionPointer<PopDebugGroupDelegate>(GetProcAddress("glPopDebugGroupKHR"));
                DebugMessageCallback = Marshal.GetDelegateForFunctionPointer<DebugMessageCallbackDelegate>(GetProcAddress("glDebugMessageCallback"));
            }

            renderdoc = Array.FindIndex(Environment.GetCommandLineArgs(), c => c.ToLower() == "-renderdoc") >= 0;
#endif

            glES3 = es3;
            initialized = true;
        }

        public static unsafe void GetFloat(int param, float[] floats)
        {
            fixed (float* p = &floats[0])
                GetFloatRaw(param, (IntPtr)p);
        }

        public static unsafe void ShaderSource(int shader, string[] source)
        {
            var ptrs = new IntPtr[source.Length];
            for (int i = 0; i < source.Length; i++)
                ptrs[i] = Marshal.StringToHGlobalAnsi(source[i]);

            fixed (IntPtr* p = &ptrs[0])
                ShaderSourceRaw(shader, source.Length, (IntPtr)p, IntPtr.Zero);

            for (int i = 0; i < source.Length; i++)
                Marshal.FreeHGlobal(ptrs[i]);
        }

        public static unsafe string GetShaderInfoLog(int shader)
        {
            var ptr = Marshal.AllocHGlobal(4096);
            var len = 0;

            GL.GetShaderInfoLogRaw(shader, 4096, ref len, ptr);

            var log = Marshal.PtrToStringAnsi(ptr);
            Marshal.FreeHGlobal(ptr);

            return log;
        }

        public static unsafe string GetProgramInfoLog(int shader)
        {
            var ptr = Marshal.AllocHGlobal(4096);
            var len = 0;

            GL.GetProgramInfoLogRaw(shader, 4096, ref len, ptr);

            var log = Marshal.PtrToStringAnsi(ptr);
            Marshal.FreeHGlobal(ptr);

            return log;
        }

        public static unsafe int GetShaderInt(int shader, int param)
        {
            int val = 0;
            GetShaderIntRaw(shader, param, new IntPtr(&val));
            return val;
        }

        public static unsafe int GetProgramInt(int program, int param)
        {
            int val = 0;
            GetProgramIntRaw(program, param, new IntPtr(&val));
            return val;
        }

        public unsafe static int GenFramebuffer()
        {
            var tmp = new int[1];
            fixed (int* p = &tmp[0])
                GenFramebuffers(1, new IntPtr(p));
            return tmp[0];
        }

        public unsafe static void DeleteFramebuffer(int id)
        {
            var tmp = new int[1] { id };
            fixed (int* p = &tmp[0])
                DeleteFramebuffers(1, new IntPtr(p));
        }

        public unsafe static void ColorPointer(int size, int type, int stride, int[] data)
        {
            fixed (int* p = &data[0])
                ColorPointerRaw(size, type, stride, new IntPtr(p));
        }

        public unsafe static void VertexPointer(int size, int type, int stride, float[] data)
        {
            fixed (float* p = &data[0])
                VertexPointerRaw(size, type, stride, new IntPtr(p));
        }

        public unsafe static void TexCoordPointer(int size, int type, int stride, float[] data)
        {
            fixed (float* p = &data[0])
                TexCoordPointerRaw(size, type, stride, new IntPtr(p));
        }

        public unsafe static void VertexAttribPointer(int index, int size, int type, bool normalized, int stride)
        {
            VertexAttribPointerRaw(index, size, type, (byte)(normalized ? 1 : 0), stride, IntPtr.Zero);
        }

        public unsafe static void VertexAttribIPointer(int index, int size, int type, int stride)
        {
            VertexAttribIPointerRaw(index, size, type, stride, IntPtr.Zero);
        }

        public static void ColorMask(bool r, bool g, bool b, bool a)
        {
            ColorMaskRaw(
                (byte)(r ? 1 : 0),
                (byte)(g ? 1 : 0),
                (byte)(b ? 1 : 0),
                (byte)(a ? 1 : 0));
        }

        public unsafe static void DrawElements(int mode, int count, int type, IntPtr data)
        {
            DrawElementsRaw(mode, count, type, data);
        }

        public unsafe static void DrawElements(int mode, int count, int type, short[] data)
        {
            fixed (short* p = &data[0])
                DrawElementsRaw(mode, count, type, new IntPtr(p));
        }

        public unsafe static int GenTexture()
        {
            var tmp = new int[1];
            fixed (int* p = &tmp[0])
                GenTextures(1, new IntPtr(p));
            return tmp[0];
        }

        public unsafe static void DeleteTexture(int id)
        {
            var tmp = new int[1] { id };
            fixed (int* p = &tmp[0])
                DeleteTextures(1, new IntPtr(p));
        }

        public unsafe static int GenBuffer()
        {
            var tmp = new int[1];
            fixed (int* p = &tmp[0])
                GenBuffersRaw(1, new IntPtr(p));
            return tmp[0];
        }

        public unsafe static void DeleteBuffer(int id)
        {
            var tmp = new int[1] { id };
            fixed (int* p = &tmp[0])
                DeleteBuffersRaw(1, new IntPtr(p));
        }

        public unsafe static int GenVertexArray()
        {
            var tmp = new int[1];
            fixed (int* p = &tmp[0])
                GenVertexArraysRaw(1, new IntPtr(p));
            return tmp[0];
        }

        public unsafe static void DeleteVertexArray(int id)
        {
            var tmp = new int[1] { id };
            fixed (int* p = &tmp[0])
                DeleteVertexArraysRaw(1, new IntPtr(p));
        }

        public unsafe static void BufferData(int target, float[] data, int len, int usage)
        {
            Debug.Assert(len <= data.Length);

            fixed (float* p = &data[0])
                BufferDataRaw(target, new IntPtr(len * sizeof(float)), (IntPtr)p, usage);
        }

        public unsafe static void BufferData(int target, int[] data, int len, int usage)
        {
            Debug.Assert(len <= data.Length);

            fixed (int* p = &data[0])
                BufferDataRaw(target, new IntPtr(len * sizeof(int)), (IntPtr)p, usage);
        }

        public unsafe static void BufferData(int target, short[] data, int len, int usage)
        {
            Debug.Assert(len <= data.Length);

            fixed (short* p = &data[0])
                BufferDataRaw(target, new IntPtr(len * sizeof(short)), (IntPtr)p, usage);
        }

        public unsafe static void BufferData(int target, byte[] data, int len, int usage)
        {
            Debug.Assert(len <= data.Length);

            fixed (byte* p = &data[0])
                BufferDataRaw(target, new IntPtr(len * sizeof(byte)), (IntPtr)p, usage);
        }

        public static void Uniform(int location, int x)
        {
            Uniform1iRaw(location, x);
        }

        public static void Uniform(int location, float x)
        {
            Uniform1fRaw(location, x);
        }

        public static void Uniform(int location, float x, float y)
        {
            Uniform2fRaw(location, x, y);
        }

        public static void Uniform(int location, float x, float y, float z)
        {
            Uniform3fRaw(location, x, y, z);
        }

        public static void Uniform(int location, float x, float y, float z, float w)
        {
            Uniform4fRaw(location, x, y, z, w);
        }

        public static unsafe void Uniform(int location, float[] values, int numComponents, int numElements = 1)
        {
            fixed (float* p = &values[0])
            {
                switch (numComponents)
                {
                    case 1: Uniform1fvRaw(location, numElements, (IntPtr)p); break;
                    case 2: Uniform2fvRaw(location, numElements, (IntPtr)p); break;
                    case 3: Uniform3fvRaw(location, numElements, (IntPtr)p); break;
                    case 4: Uniform4fvRaw(location, numElements, (IntPtr)p); break;
                    default: Debug.Assert(false); break;
                }
            }
        }

        public unsafe static void TexImage2D(int target, int level, int internalformat, int width, int height, int border, int format, int type, IntPtr data)
        {
            TexImage2DRaw(target, level, internalformat, width, height, border, format, type, data);
        }

        public unsafe static void TexImage2D(int target, int level, int internalformat, int width, int height, int border, int format, int type, int[] data)
        {
            fixed (int* p = &data[0])
                TexImage2DRaw(target, level, internalformat, width, height, border, format, type, new IntPtr(p));
        }

        public unsafe static void TexImage2D(int target, int level, int internalformat, int width, int height, int border, int format, int type, byte[] data)
        {
            fixed (byte* p = &data[0])
                TexImage2DRaw(target, level, internalformat, width, height, border, format, type, new IntPtr(p));
        }

        public unsafe static void TexSubImage2D(int target, int level, int xoffset, int yoffset, int width, int height, int format, int type, IntPtr data)
        {
            TexSubImage2DRaw(target, level, xoffset, yoffset, width, height, format, type, data);
        }

        public unsafe static void TexSubImage2D(int target, int level, int xoffset, int yoffset, int width, int height, int format, int type, int[] data)
        {
            fixed (int* p = &data[0])
                TexSubImage2DRaw(target, level, xoffset, yoffset, width, height, format, type, new IntPtr(p));
        }

        public unsafe static void TexSubImage2D(int target, int level, int xoffset, int yoffset, int width, int height, int format, int type, byte[] data)
        {
            fixed (byte* p = &data[0])
                TexSubImage2DRaw(target, level, xoffset, yoffset, width, height, format, type, new IntPtr(p));
        }

        public static void PushDebugGroup(string name)
        {
#if DEBUG && !FAMISTUDIO_MACOS
            if (renderdoc && PushDebugGroupRaw != null)
                PushDebugGroupRaw(DebugSourceApplication, 0, -1, name);
#endif
        }

        public static void PopDebugGroup()
        {
#if DEBUG && !FAMISTUDIO_MACOS
            if (renderdoc && PopDebugGroupRaw != null)
                PopDebugGroupRaw();
#endif
        }
    }
}