using System;
using VRageMath;
using static Math0424.AnimationCoreAPI.AnimationCoreAPI;

namespace Math0424.AnimationCore
{

    

    public static class LerpExtensions
    {

        public static float Lerp(this LerpType type, EaseType ease, float one, float two, double val)
        {
            return MathHelper.Lerp(one, two, (float)type.LerpVal(val, ease));
        }

        public static Vector3 Lerp(this LerpType type, EaseType ease, Vector3 one,  Vector3 two, double val)
        {
            return Vector3.Lerp(one, two, (float)type.LerpVal(val, ease));
        }

        public static Matrix Lerp(this LerpType type, EaseType ease, Matrix one, Matrix two, double val)
        {
            return Matrix.Slerp(one, two, (float)type.LerpVal(val, ease));
        }

        public static Quaternion Lerp(this LerpType type, EaseType ease, Quaternion one, Quaternion two, double val)
        {
            return Quaternion.Slerp(one, two, (float)type.LerpVal(val, ease));
        }

        public static double LerpVal(this LerpType lerp, double val, EaseType ease)
        {
            switch (lerp)
            {
                case LerpType.Instant: return 1;
                case LerpType.Linear: return val;

                case LerpType.Bounce: return Bounce(val, ease);
                case LerpType.Elastic: return Elastic(val, ease);
                case LerpType.Expo: return Expo(val, ease);
                case LerpType.Cubic: return Cubic(val, ease);
                case LerpType.Back: return Back(val, ease);
            }
            return 0;
        }

        //Functions
        private static double Bounce(double x, EaseType type)
        {
            const double n1 = 7.5625;
            const double d1 = 2.75;

            switch (type)
            {
                case EaseType.In:
                    return 1 - Bounce(1 - x, EaseType.Out);
                case EaseType.Out:
                    if (x < 1 / d1)
                    {
                        return n1 * x * x;
                    }
                    else if (x < 2 / d1)
                    {
                        return n1 * (x -= 1.5 / d1) * x + 0.75;
                    }
                    else if (x < 2.5 / d1)
                    {
                        return n1 * (x -= 2.25 / d1) * x + 0.9375;
                    }
                    else
                    {
                        return n1 * (x -= 2.625 / d1) * x + 0.984375;
                    }
                case EaseType.InOut:
                    return x < 0.5 ? (1 - Bounce(1 - 2 * x, EaseType.Out)) / 2 : (1 + Bounce(2 * x - 1, EaseType.Out)) / 2;
            }
            return x;
        }

        private static double Elastic(double x, EaseType type)
        {
            const double c4 = (2 * Math.PI) / 3;
            const double c5 = (2 * Math.PI) / 4.5;

            switch (type)
            {
                case EaseType.In:
                    return x == 0 ? 0 : x == 1 ? 1 : Math.Pow(2, -10 * x) * Math.Sin((x * 10 - 0.75) * c4) + 1;
                case EaseType.Out:
                    return x == 0 ? 0 : x == 1 ? 1 : Math.Pow(2, -10 * x) * Math.Sin((x * 10 - 0.75) * c4) + 1;
                case EaseType.InOut:
                    return x == 0 ? 0 : x == 1 ? 1 : x < 0.5 ? -(Math.Pow(2, 20 * x - 10) * Math.Sin((20 * x - 11.125) * c5)) / 2 : (Math.Pow(2, -20 * x + 10) * Math.Sin((20 * x - 11.125) * c5)) / 2 + 1; ;
            }
            return x;
        }

        private static double Expo(double x, EaseType type)
        {
            switch (type)
            {
                case EaseType.In:
                    return x == 0 ? 0 : Math.Pow(2, 10 * x - 10);
                case EaseType.Out:
                    return x == 1 ? 1 : 1 - Math.Pow(2, -10 * x);
                case EaseType.InOut:
                    return x == 0 ? 0 : x == 1 ? 1 : x < 0.5 ? Math.Pow(2, 20 * x - 10) / 2 : (2 - Math.Pow(2, -20 * x + 10)) / 2;
            }
            return x;
        }

        private static double Cubic(double x, EaseType type)
        {
            switch (type)
            {
                case EaseType.In:
                    return x * x * x;
                case EaseType.Out:
                    return 1 - Math.Pow(1 - x, 3);
                case EaseType.InOut:
                    return x < 0.5 ? 4 * x * x * x : 1 - Math.Pow(-2 * x + 2, 3) / 2;
            }
            return x;
        }

        private static double Back(double x, EaseType type)
        {
            const double c1 = 1.70158;
            const double c2 = c1 * 1.525;
            const double c3 = c1 + 1;

            switch (type)
            {
                case EaseType.In:
                    return c3 * x * x * x - c1 * x * x;
                case EaseType.Out:
                    return 1 + c3 * Math.Pow(x - 1, 3) + c1 * Math.Pow(x - 1, 2);
                case EaseType.InOut:
                    return x < 0.5 ? (Math.Pow(2 * x, 2) * ((c2 + 1) * 2 * x - c2)) / 2 : (Math.Pow(2 * x - 2, 2) * ((c2 + 1) * (x * 2 - 2) + c2) + 2) / 2;
            }
            return x;
        }



    }

}
