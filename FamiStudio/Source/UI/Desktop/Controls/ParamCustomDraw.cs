namespace FamiStudio
{
    public class ParamCustomDraw : ParamControl
    {
        public ParamCustomDraw(ParamInfo p) : base (p)
        {
        }

        protected override void OnRender(Graphics g)
        {
            param.CustomDraw.Invoke(g.DefaultCommandList, Fonts, ClientRectangle, param.CustomUserData1, param.CustomUserData2);
        }
    }
}
