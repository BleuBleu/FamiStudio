namespace FamiStudio
{
    public class ParamCustomDraw : Control
    {
        private ParamInfo.CustomDrawDelegate draw;
        private object userData1;
        private object userData2;

        public ParamCustomDraw(ParamInfo.CustomDrawDelegate d, object d1 = null, object d2 = null)
        {
            draw = d;
            userData1 = d1;
            userData2 = d2; 
        }

        protected override void OnRender(Graphics g)
        {
            draw.Invoke(g.GetCommandList(), Fonts, ClientRectangle, userData1, userData2);
        }
    }
}
