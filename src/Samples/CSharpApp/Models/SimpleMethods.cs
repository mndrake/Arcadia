namespace CSharpApp.Models
{
    using System.Threading;

    public static class SimpleMethods
    {
        public static int Add2(int x1, int x2)
        {
            Thread.Sleep(100);
            return x1 + x2;
        }

        public static int Add3(int x1, int x2, int x3)
        {
            Thread.Sleep(100);
            return x1 + x2 + x3;
        }

        public static int Add4(int x1, int x2, int x3, int x4)
        {
            Thread.Sleep(100);
            return x1 + x2 + x3 + x4;
        }
    }
}
