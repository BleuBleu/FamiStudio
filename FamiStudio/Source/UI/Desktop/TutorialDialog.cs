using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using RenderBitmapAtlasRef = FamiStudio.GLBitmapAtlasRef;
using RenderBrush          = FamiStudio.GLBrush;
using RenderGeometry       = FamiStudio.GLGeometry;
using RenderControl        = FamiStudio.GLControl;
using RenderGraphics       = FamiStudio.GLGraphics;
using RenderCommandList    = FamiStudio.GLCommandList;

namespace FamiStudio
{
    public partial class TutorialDialog : Dialog
    {
#if FAMISTUDIO_WINDOWS
        private const string GifDecDll = "GifDec.dll";
#elif FAMISTUDIO_MACOS
        private const string GifDecDll = "GifDec.dylib";
#else
        private const string GifDecDll = "GifDec.so";
#endif

        [DllImport(GifDecDll, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr GifOpen(IntPtr data, int swap);

        [DllImport(GifDecDll, CallingConvention = CallingConvention.StdCall)]
        private static extern int GifGetWidth(IntPtr gif);

        [DllImport(GifDecDll, CallingConvention = CallingConvention.StdCall)]
        private static extern int GifGetHeight(IntPtr gif);

        [DllImport(GifDecDll, CallingConvention = CallingConvention.StdCall)]
        private static extern int GifAdvanceFrame(IntPtr gif, IntPtr buffer, int stride);

        [DllImport(GifDecDll, CallingConvention = CallingConvention.StdCall)]
        private static extern int GifGetFrameDelay(IntPtr gif);

        [DllImport(GifDecDll, CallingConvention = CallingConvention.StdCall)]
        private static extern void GifClose(IntPtr gif);

        public static readonly string[] TutorialMessages = new[]
        {
            @"(1/9) Welcome to FamiStudio! Let's take a few seconds to review some of the basic controls to make sure you use the app to its fullest.",
            @"(2/9) To PAN around the piano roll or the sequencer, simply PRESS and HOLD the MIDDLE MOUSE BUTTON and DRAG around to smoothly move the viewport. Yes, that wheel on your mouse is also a button!",
            @"(3/9) To ZOOM in and out in the piano roll or the sequencer, simply rotate the mouse wheel.",
            @"(4/9) If you are on a TRACKPAD or a LAPTOP, simply enable TRACKPAD CONTROLS in the settings.",
            @"(5/9) To ADD things like patterns and notes, simply CLICK with the LEFT MOUSE BUTTON.",
            @"(6/9) To DELETE things like patterns, notes, instruments and songs, simply CLICK on them with the RIGHT MOUSE BUTTON.",
            @"(7/9) SNAPPING is ON by default and is expressed in BEATS. With the default settings, it will snap to 1/4 notes. You can change the snapping precision or disable it completely to create notes of different lengths!",
            @"(8/9) Always keep an eye on the TOOLTIPS! They change constantly as you move the mouse and they will teach you how to use the app! For the complete DOCUMENTATION and over 1 hour of VIDEO TUTORIAL, please click on the big QUESTION MARK!",
            @"(9/9) Join us on DISCORD to meet other FamiStudio users and share your songs with them! Link in the documentation (question mark icon in the toolbar)."
        };

        public static readonly string[] TutorialImages = new[]
        {
            "Tutorial0.gif",
            "Tutorial1.gif",
            "Tutorial2.gif",
            "Tutorial3.gif",
            "Tutorial4.gif",
            "Tutorial5.gif",
            "Tutorial6.gif",
            "Tutorial7.gif",
            "Tutorial8.gif",
        };

        int pageIndex = 0;

        private Button2 buttonRight;
        private Button2 buttonLeft;
        private ImageBox2 imageBox;
        private Label2 label;
        private CheckBox2 checkBoxDontShow;

        private int margin     = DpiScaling.ScaleForMainWindow(8);
        private int imageSizeX = DpiScaling.ScaleForMainWindow(960);
        private int imageSizeY = DpiScaling.ScaleForMainWindow(540);
        private int labelSizeY = DpiScaling.ScaleForMainWindow(36);
        private int buttonSize = DpiScaling.ScaleForMainWindow(36);
        private int checkSizeY = DpiScaling.ScaleForMainWindow(16);

        private Timer gifTimer = new Timer();
        private IntPtr gif;
        private Bitmap gifBmp;
        private byte[] gifData;
        private GCHandle gifHandle;

        public TutorialDialog()
        {
            Move(0, 0, 
                imageSizeX + margin * 2, 
                imageSizeY + margin * 4 + buttonSize + labelSizeY);

            Init();
        }

        private void Init()
        {
            buttonLeft = new Button2("ArrowLeft", null);
            buttonLeft.Click += ButtonLeft_Click;
            buttonLeft.Resize(buttonSize, buttonSize);
            buttonLeft.ToolTip = "Previous";
            buttonLeft.Move(width - buttonSize * 2 - margin * 2, height - buttonSize - margin);

            buttonRight = new Button2("ArrowRight", null);
            buttonRight.Click += ButtonRight_Click;
            buttonRight.Resize(buttonSize, buttonSize);
            buttonRight.ToolTip = "Next";
            buttonRight.Move(width - buttonSize - margin, height - buttonSize - margin);

            label = new Label2("This is a nice label", true);
            label.Move(margin, margin, width - margin, labelSizeY);

            imageBox = new ImageBox2("VideoWatermark");
            imageBox.Move(margin, margin * 2 + labelSizeY, imageSizeX, imageSizeY);

            checkBoxDontShow = new CheckBox2(false, "Do not show again");
            checkBoxDontShow.Move(margin, margin * 3 + labelSizeY + imageSizeY, width - buttonSize * 3, checkSizeY);

            AddControl(buttonLeft);
            AddControl(buttonRight);
            AddControl(label);
            AddControl(imageBox);
            AddControl(checkBoxDontShow);

            gifTimer.Interval = 10;
            gifTimer.Tick += GifTimer_Tick;

            SetPage(0);
        }


        private void GifTimer_Tick(object sender, EventArgs e)
        {
            UpdateGif();
        }

        private void OpenGif(string filename)
        {
            if (gif != IntPtr.Zero)
            {
                CloseGif();
            }

            // GifDec works only with files, create a copy.
            // TODO : Change it to take a buffer as input.
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"FamiStudio.Resources.{filename}"))
            {
                gifData = new byte[stream.Length];
                stream.Read(gifData, 0, (int)stream.Length);
                stream.Close();
            }

            gifHandle = GCHandle.Alloc(gifData, GCHandleType.Pinned);
            
            gif = GifOpen(gifHandle.AddrOfPinnedObject(), 1);

            var width  = GifGetWidth(gif);
            var height = GifGetHeight(gif);

            // MATTT
            //gifBmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            //imageBox.Image = gifBmp;
        }

        private void CloseGif()
        {
            if (gif != IntPtr.Zero)
            {
                gifData = null;
                gifHandle.Free();
                gifTimer.Stop();
                GifClose(gif);
                gif = IntPtr.Zero;
                gifBmp = null;
            }
        }

        private void UpdateGif()
        {
            // MATTT
            //var data = gifBmp.LockBits(new Rectangle(0, 0, gifBmp.Width, gifBmp.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            //GifAdvanceFrame(gif, data.Scan0, data.Stride);
            //gifTimer.Interval = GifGetFrameDelay(gif);
            //gifTimer.Start();
            //gifBmp.UnlockBits(data);
            MarkDirty();
        }

        protected override void OnShowDialog()
        {
            CenterToForm();
        }

        private void SetPage(int idx)
        {
            pageIndex = Utils.Clamp(idx, 0, TutorialMessages.Length - 1);
            label.Text = TutorialMessages[pageIndex];
            label.ResizeForMultiline();
            buttonLeft.Visible = pageIndex != 0;

            OpenGif(TutorialImages[pageIndex]);
            UpdateGif();

            // MATTT
            //string suffix = DpiScaling.MainWindow >= 2.0f ? "@2x" : "";
            //buttonRight.Image = pageIndex == TutorialMessages.Length - 1 ?
            //    Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream($"FamiStudio.Resources.Yes{suffix}.png")) :
            //    Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream($"FamiStudio.Resources.ArrowRight{suffix}.png"));
        }

        private void ButtonLeft_Click(RenderControl sender)
        {
            SetPage(pageIndex - 1);
        }

        private void ButtonRight_Click(RenderControl sender)
        {
            if (pageIndex == TutorialMessages.Length - 1)
            {
                // MATTT
                //DialogResult = checkBoxDontShow.Checked ? DialogResult.OK : DialogResult.Cancel;
                //Close();
            }
            else
            {
                SetPage(pageIndex + 1);
            }
        }

        private void TutorialDialog_KeyDown(object sender, KeyEventArgs e)
        {
            //buttonRight_Click(null, EventArgs.Empty);
        }
    }
}
