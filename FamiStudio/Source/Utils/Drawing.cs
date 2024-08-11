using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

// This is basically the handful of simple structs we still used from System.Drawing
// and is done in preparation for an eventual migration to more modern .NET versions.
namespace FamiStudio
{
    // Unlike the System.Drawing.Color, the internal encoding is AABBGGRR as opposed
    // to AARRGGBB. This is done in order to match our OpenGL color packing and avoid
    // a bunch of conversions.
    public struct Color : IEquatable<Color>
    {
        private int color = unchecked((int)0xff000000); // 0xAABBGGRR

        public static readonly Color Empty = default(Color);
                                        
        public static Color White       => new Color(255, 255, 255);
        public static Color Black       => new Color(0, 0, 0);
        public static Color Azure       => new Color(240, 255, 255);
        public static Color Invisible   => new Color(0, 0, 0, 0);
        public static Color SpringGreen => new Color(0, 255, 127);
        public static Color Pink        => new Color(255, 192, 203);

        public byte A
        {
            get => (byte)((color >> 24) & 0xff);
            set => color = (color & (int)~0xff000000) | (value << 24);
        }

        public byte B
        {
            get => (byte)((color >> 16) & 0xff);
            set => color = (color & ~0xff0000) | (value << 16);
        }

        public byte G
        {
            get => (byte)((color >> 8) & 0xff);
            set => color = (color & ~0xff00) | (value << 8);
        }

        public byte R
        {
            get => (byte)((color >> 0) & 0xff);
            set => color = (color & ~0xff) | value;
        }

        public Color(int packed)
        {
            color = packed;
        }

        public Color(int r, int g, int b, int a = 255)
        {
            Debug.Assert(r >= 0 && r <= 255 && g >= 0 && g <= 255 && b >= 0 && b <= 255 && a >= 0 && a <= 255);
            color = (a << 24) | (b << 16) | (g << 8) | r;
        }

        public static Color FromArgb(int a, int r, int g, int b)
        {
            return new Color(r, g, b, a);
        }

        public static Color FromArgb(int r, int g, int b)
        {
            return new Color(r, g, b);
        }

        public static Color FromArgb(int a, Color c)
        {
            Debug.Assert(a >= 0 && a <= 255);
            return new Color((a << 24) | (c.ToAbgr() & 0xffffff));
        }

        public static Color FromArgb(int argb)
        {
            return new Color(
                (argb >> 16) & 0xff, 
                (argb >>  8) & 0xff, 
                (argb >>  0) & 0xff,
                (argb >> 24) & 0xff);
        }

        public int ToArgb()
        {
            return (A << 24) | (R << 16) | (G << 8) | B;
        }

        public int ToAbgr()
        {
            return color;
        }

        public Color Scaled(float scale)
        {
            return new Color((int)(R * scale), (int)(G * scale), (int)(B * scale), A);
        }

        // Integer math, 255 = no change, 128 = half, etc.
        public Color Scaled(int scale, bool alpha = false)
        {
            var r = Utils.ColorMultiply(R, scale);
            var g = Utils.ColorMultiply(G, scale);
            var b = Utils.ColorMultiply(B, scale);
            var a = alpha ? Utils.ColorMultiply(A, scale) : A;
            return new Color(r, g, b, a);
        }

        public Color Transparent(int a, bool multiply = false)
        {
            if (multiply)
                a = Utils.ColorMultiply(a, A);
            return FromArgb(a, this);
        }

        public static bool operator==(Color left, Color right)
        {
            return left.color == right.color;
        }

        public static bool operator!=(Color left, Color right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (obj is Color c)
            {
                return this.color == c.color;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return color;
        }

        public string ToHexString()
        {
            return $"{R:x2}{G:x2}{B:x2}";
        }

        public static Color FromHexString(string str)
        {
            int parsed = Convert.ToInt32(str, 16);
            return new Color((parsed >> 16) & 0xff, (parsed >> 8) & 0xff, (parsed >> 0) & 0xff);
        }

        public override string ToString()
        {
            return $"R={R} G={G} B={B} A={A}";
        }

        public bool Equals(Color other)
        {
            return color.Equals(other.color);
        }
    }

    public struct Point
    {
        public static readonly Point Empty;

        private int x;
        private int y;

        public int X { get => x; set => x = value; }
        public int Y { get => y; set => y = value; }

        public Point(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public static Point operator+(Point pt, Size sz)
        {
            return new Point(pt.x + sz.Width, pt.y + sz.Height);
        }

        public static Point operator-(Point p1, Point p2)
        {
            return new Point(p1.x - p2.x, p1.y - p2.y);
        }

        public override string ToString()
        {
            return $"({x},{y})";
        }
    }

    public struct PointF
    {
        public static readonly PointF Empty;

        private float x;
        private float y;

        public float X { get => x; set => x = value; }
        public float Y { get => y; set => y = value; }

        public PointF(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
    }

    public struct Size
    {
        public static readonly Size Empty;

        private int width;
        private int height;

        public int Width { get => width; set => width = value; }
        public int Height { get => height; set => height = value; }

        public Size(int w, int h)
        {
            width  = w;
            height = h;
        }

        public static bool operator ==(Size s1, Size s2)
        {
            return s1.width == s2.width && s1.height == s2.height;
        }

        public static bool operator !=(Size s1, Size s2)
        {
            return !(s1 == s2);
        }

        public override bool Equals(object obj)
        {
            if (obj is Size s)
            {
                return width == s.width && height == s.height;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Utils.HashCombine(width, height);
        }

        public override string ToString()
        {
            return $"{width}x{height}";
        }
    }

    public struct Rectangle
    {
        public static readonly Rectangle Empty;

        private int x;
        private int y;
        private int width;
        private int height;

        public int X { get => x; set => x = value; }
        public int Y { get => y; set => y = value; }
        public int Width { get => width; set => width = value; }
        public int Height { get => height; set => height = value; }

        public int Left => x;
        public int Top => y;
        public int Right => x + width;
        public int Bottom => y + height;
        public int Area => width * height;

        public Point Min => new Point(x, y);    
        public Point Max => new Point(x + width, y + height);

        public bool IsEmpty => height == 0 && width == 0 && x == 0 && y == 0;

        public Rectangle(int x, int y, int width, int height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public Rectangle(Point p, Size s)
        {
            x = p.X;
            y = p.Y;
            width  = s.Width;
            height = s.Height;
        }

        public Size Size
        {
            get
            {
                return new Size(width, height);
            }
            set
            {
                width  = value.Width;
                height = value.Height;
            }
        }

        public bool Contains(int px, int py)
        {
            return px >= x && px < x + width && 
                   py >= y && py < y + height;
        }

        public bool Contains(Point p)
        {
            return p.X >= x && p.X < x + width &&
                   p.Y >= y && p.Y < y + height;
        }

        public bool Contains(Rectangle r)
        {
            return Contains(r.Min) && Contains(r.Max);
        }

        public void Offset(int x, int y)
        {
            this.x += x;
            this.y += y;
        }

        public Rectangle Offsetted(int ox, int oy)
        {
            return new Rectangle(x + ox, y + oy, width, height);
        }

        public Rectangle Resized(int sx, int sy)
        {
            return new Rectangle(x, y, width + sx, height + sy);
        }

        public bool Intersects(Rectangle r)
        {
            // TODO : Do we want > or >= ?
            if (Left   > r.Right  ||
                Right  < r.Left   ||
                Top    > r.Bottom ||
                Bottom < r.Top)
            {
                return false;
            }

            return true;
        }

        public static Rectangle Union(Rectangle a, Rectangle b)
        {
            int minX = Math.Min(a.X, b.X);
            int maxX = Math.Max(a.X + a.Width, b.X + b.Width);
            int minY = Math.Min(a.Y, b.Y);
            int maxY = Math.Max(a.Y + a.Height, b.Y + b.Height);
            
            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }

        public static Rectangle Intersection(Rectangle a, Rectangle b)
        {
            int maxX = Math.Max(a.X, b.X);
            int minX = Math.Min(a.X + a.Width, b.X + b.Width);
            int maxY = Math.Max(a.Y, b.Y);
            int minY = Math.Min(a.Y + a.Height, b.Y + b.Height);

            if (minX >= maxX && minY >= maxY)
            {
                return new Rectangle(maxX, maxY, minX - maxX, minY - maxY);
            }

            return Empty;
        }

        // returns a - b (Not tested).
        public static Rectangle[] Difference(Rectangle a, Rectangle b) 
        {
            if (a.Area == 0)
            {
                return null;
            }
            
            if (a.Equals(b))
            {
                return null;
            }

            if (Intersection(a, b).Area == 0)
            {
                return new Rectangle[] { a };
            }

            var rt = new Rectangle(a.Left, a.Top, a.Width, Math.Max(b.Top, 0));
            var rl = new Rectangle(a.Left, b.Top, Math.Max(b.Left - a.Left, 0), b.Height);
            var rr = new Rectangle(b.Right, b.Top, Math.Max(a.Right - b.Right, 0), b.Height);
            var rb = new Rectangle(a.Left, b.Bottom, a.Width, Math.Max(a.Height - b.Bottom, 0));

            var count =
                (rt.Area > 0 ? 1 : 0) +
                (rl.Area > 0 ? 1 : 0) +
                (rr.Area > 0 ? 1 : 0) +
                (rb.Area > 0 ? 1 : 0);

            var i = 0;
            var result = new Rectangle[count];

            if (rt.Area > 0) result[i++] = rt;
            if (rl.Area > 0) result[i++] = rl;
            if (rr.Area > 0) result[i++] = rr;
            if (rb.Area > 0) result[i++] = rb;

            return result;
        }

        public override string ToString()
        {
            return $"{Min.ToString()}x{Max.ToString()}";
        }
    }


    public struct RectangleF
    {
        public static readonly RectangleF Empty;

        private float x;
        private float y;
        private float width;
        private float height;

        public float X { get => x; set => x = value; }
        public float Y { get => y; set => y = value; }
        public float Width { get => width; set => width = value; }
        public float Height { get => height; set => height = value; }

        public float Left => x;
        public float Top => y;
        public float Right => x + width;
        public float Bottom => y + height;
        public bool IsEmpty => height == 0 && width == 0 && x == 0 && y == 0;

        public RectangleF(float x, float y, float width, float height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public static RectangleF Intersection(RectangleF a, RectangleF b)
        {
            float maxX = Math.Max(a.X, b.X);
            float minX = Math.Min(a.X + a.Width, b.X + b.Width);
            float maxY = Math.Max(a.Y, b.Y);
            float minY = Math.Min(a.Y + a.Height, b.Y + b.Height);

            if (minX >= maxX && minY >= maxY)
            {
                return new RectangleF(maxX, maxY, minX - maxX, minY - maxY);
            }

            return Empty;
        }
    }
}
