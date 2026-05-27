namespace ARCYN.Platform
{
    public interface IWindowEffects
    {
        void EnableAcrylic(IntPtr windowHandle, uint opacityColor = 0xEB000000);
    }
}