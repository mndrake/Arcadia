namespace CSharpApp.Models
{
    using System;
    using System.Threading;
    using Arcadia;

    //// async functions that are used to calculate output nodes
    public class SimpleCalculationEngine : CalculationEngine
    {
        static int Add2(int x1, int x2)
        {
            Thread.Sleep(100);
            if (x1 == 0 || x2 == 0)
                throw new Exception("inputs cannot be 0");
            return x1 + x2;
        }

        static int Add3(int x1, int x2, int x3)
        {
            Thread.Sleep(100);
            if (x1 == 0 || x1 == 0 || x3 == 0)
                throw new Exception("input cannot be 0");
            return x1 + x2 + x3;
        }

        public SimpleCalculationEngine() : base()
        {
            // input nodes
            var in0 = Setable(1);
            var in1 = Setable(1);
            var in2 = Setable(1);
            var in3 = Setable(1);
            var in4 = Setable(1);
            var in5 = Setable(1);
            var in6 = Setable(1);
            var in7 = Setable(1);
            var in8 = Setable(1);
            var in9 = Setable(1);
            var in10 = Setable(1);
            var in11 = Setable(1);
            var in12 = Setable(1);
            var in13 = Setable(1);

            // output nodes

            //main calculation chain
            var out0 = Computed(() => Add2(in0, in1));
            var out1 = Computed(() => in2 + in3);
            var out2 = Computed(() => Add3(in4, in5, in6));
            var out3 = Computed(() => in7 + in8);
            var out4 = Computed(() => out1 + out2);
            var out5 = Computed(() => out0 + out3);
            var out6 = Computed(() => in9 + in10);
            var out7 = Computed(() => in11 + in12);
            var out8 = Computed(() => out4 + out6);
            var out9 = Computed(() => out5 + out7 + out8);
            var out10 = Computed(() => out0 + out5);

            var out11 = Computed(() => in13.Value);
            var out12 = Computed(() => out10 + out3 + in6);
        }
    }
}