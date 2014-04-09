namespace CSharpApp.Models
{
    using System;
    using Arcadia;

    public class SimpleCalculationEngine : CalculationEngine
    {
        public SimpleCalculationEngine()
            : base()
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
            var out0 = Computed(() => in0 + in1);
            var out1 = Computed(() => in2 + in3);
            var out2 = Computed(() => in4 + in5 + in6);
            var out3 = Computed(() => in7 + in8);
            var out4 = Computed(() => out1 + out2);
            var out5 = Computed(() => out0 + out3);
            var out6 = Computed(() => in9 + in10);
            var out7 = Computed(() => in11 + in12);
            var out8 = Computed(() => out4 + out6);
            var out9 = Computed(() => out5 + out7 + out8);
            var out10 = Computed(() => out0 + out5);

            var out11 = Computed(() => in13.Value);
        }
    }
}