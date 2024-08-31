namespace emdui
{
    public static class Utility
    {
        public static double Lerp(double a, double b, double t)
        {
            var range = b - a;
            return a + (range * t);
        }
    }
}
