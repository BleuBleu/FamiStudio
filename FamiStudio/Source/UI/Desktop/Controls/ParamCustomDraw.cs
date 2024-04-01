namespace FamiStudio
{
    public class ParamCustomDraw : Control
    {
        private ParamInfo param;

        public ParamCustomDraw(ParamInfo p)
        {
            param = p;
        }

        protected override void OnRender(Graphics g)
        {
            param.CustomDraw.Invoke(g.DefaultCommandList, Fonts, ClientRectangle, param.CustomUserData1, param.CustomUserData2);
        }
    }
}
