namespace FamiStudio
{
    public class SimpleBitmap
    {
        private int width;
        private int height;
        private int[] data;

        public int Width => width;
        public int Height => height;
        public int[] Data => data;

        public SimpleBitmap(int w, int h)
        {
            width = w;
            height = h;
            data = new int[w * h];
        }

        public int GetPixel(int x, int y)
        {
            return data[y * width + x];
        }

        public void SetPixel(int x, int y, int value)
        {
            data[y * width + x] = value;
        }

        public SimpleBitmap Resize(int newWidth, int newHeight)
        {
            var newBmp = new SimpleBitmap(newWidth, newHeight);
            var factorX = width  / (float)newWidth;
            var factorY = height / (float)newHeight;

            for (var y = 0; y < newHeight; y++)
            {
                var yy = y * factorY;
                var fy = Utils.Frac(yy);
                var y0 = (int)(yy + 0);
                var y1 = (int)(yy + 1);

                for (var x = 0; x < newWidth; x++)
                {
                    var xx = x * factorX;
                    var fx = Utils.Frac(xx);
                    var x0 = (int)(xx + 0);
                    var x1 = (int)(xx + 1);

                    var pixel00 = GetPixel(x0, y0);
                    var pixel01 = GetPixel(x1, y0);
                    var pixel10 = GetPixel(x0, y1);
                    var pixel11 = GetPixel(x1, y1);

                    newBmp.SetPixel(x, y, 
                        ((int)Utils.BiLerp((float)((pixel00 >>  0) & 0xff), (float)((pixel01 >>  0) & 0xff), (float)((pixel10 >>  0) & 0xff), (float)((pixel11 >>  0) & 0xff), fx, fy) <<  0) |
                        ((int)Utils.BiLerp((float)((pixel00 >>  8) & 0xff), (float)((pixel01 >>  8) & 0xff), (float)((pixel10 >>  8) & 0xff), (float)((pixel11 >>  8) & 0xff), fx, fy) <<  8) |
                        ((int)Utils.BiLerp((float)((pixel00 >> 16) & 0xff), (float)((pixel01 >> 16) & 0xff), (float)((pixel10 >> 16) & 0xff), (float)((pixel11 >> 16) & 0xff), fx, fy) << 16) |
                        ((int)Utils.BiLerp((float)((pixel00 >> 24) & 0xff), (float)((pixel01 >> 24) & 0xff), (float)((pixel10 >> 24) & 0xff), (float)((pixel11 >> 24) & 0xff), fx, fy) << 24));
                }
            }

            return newBmp;
        }
    }
}
