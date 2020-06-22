namespace Bomberman.Api
{
    public static class Extensions
    {
        public static int AsInt(this bool b)
        {
            return b ? 1 : 0;
        }
    }
}
