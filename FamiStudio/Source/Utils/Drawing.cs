using System.Diagnostics;

// This is basically the handful of simple structs we still used from System.Drawing.
namespace FamiStudio
{
    public struct Color
    {
        private int color; // 0xAARRGGBB

        public static readonly Color Empty = default(Color);
                                        
        public static Color White       => new Color(255, 255, 255);
        public static Color Black       => new Color(0, 0, 0);
        public static Color Azure       => new Color(240, 255, 255);
        public static Color Transparent => new Color(0, 0, 0, 0);

        public byte A
        {
            get => (byte)((color >> 24) & 0xff);
            set => color = (color & (int)~0xff000000) | (value << 24);
        }

        public byte R
        {
            get => (byte)((color >> 16) & 0xff);
            set => color = (color & ~0xff0000) | (value << 16);
        }

        public byte G
        {
            get => (byte)((color >> 8) & 0xff);
            set => color = (color & ~0xff00) | (value << 8);
        }

        public byte B
        {
            get => (byte)((color >> 0) & 0xff);
            set => color = (color & ~0xff) | value;
        }

        public Color(int r, int g, int b, int a = 255)
        {
            Debug.Assert(r >= 0 && r <= 255 && g >= 0 && g <= 255 && b >= 0 && b <= 255 && a >= 0 && a <= 255);
            color = (a << 24) | (r << 16) | (g << 8) | b;
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
            return new Color(c.R, c.G, c.B, a);
        }

        public static Color FromArgb(int argb)
        {
            return new Color() { color = argb };
        }

        public int ToArgb()
        {
            return color;
        }

        public int ToAbgr()
        {
            Debug.Assert(false);
            return 0;
        }

        public static bool operator ==(Color left, Color right)
        {
            return left.color == right.color;
        }

        public static bool operator !=(Color left, Color right)
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

        public override string ToString()
        {
            return $"R={R} G={G} B={B} A={A}";
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

        public static Point operator +(Point pt, Size sz)
        {
            return new Point(pt.x + sz.Width, pt.y + sz.Height);
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
        public bool IsEmpty => height == 0 && width == 0 && x == 0 && y == 0;

        public Rectangle(int x, int y, int width, int height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
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

        public void Offset(int x, int y)
        {
            this.x += x;
            this.y += y;
        }
    }


    public struct RectangleF
    {
        public static readonly Rectangle Empty;

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

        public RectangleF(float x, float y, float width, float height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }
    }
}
