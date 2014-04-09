C# Simple Example
-----------------

Review the GitHub site for an example of this in a MVVM app.

__version 0.2__

    [lang=csharp]
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

__version 0.1__

    [lang=csharp]
    public class SimpleCalculationEngine : CalculationEngine
    {
        public SimpleCalculationEngine()
            : base()
        {
            // NodeFunc wrappers around static methods

            var add2 = new NodeFunc<Tuple<int, int>, int>
                        (x => SimpleMethods.Add2(x.Item1, x.Item2));
            var add3 = new NodeFunc<Tuple<int, int, int>, int>
                        (x => SimpleMethods.Add3(x.Item1, x.Item2, x.Item3));

            // input nodes

            var in0 = AddInput(1);
            var in1 = AddInput(1);
            var in2 = AddInput(1);
            var in3 = AddInput(1);
            var in4 = AddInput(1);
            var in5 = AddInput(1);
            var in6 = AddInput(1);
            var in7 = AddInput(1);
            var in8 = AddInput(1);
            var in9 = AddInput(1);
            var in10 = AddInput(1);
            var in11 = AddInput(1);
            var in12 = AddInput(1);
            var in13 = AddInput(1);

            // output nodes

            //main calculation chain

            var out0 = AddOutput(Tuple.Create(in0, in1), add2);
            var out1 = AddOutput(Tuple.Create(in2, in3), add2);
            var out2 = AddOutput(Tuple.Create(in4, in5, in6), add3);
            var out3 = AddOutput(Tuple.Create(in7, in8), add2);
            var out4 = AddOutput(Tuple.Create(out1, out2), add2);
            var out5 = AddOutput(Tuple.Create(out0, out3), add2);
            var out6 = AddOutput(Tuple.Create(in9, in10), add2);
            var out7 = AddOutput(Tuple.Create(in11, in12), add2);
            var out8 = AddOutput(Tuple.Create(out4, out6), add2);
            var out9 = AddOutput(Tuple.Create(out5, out7, out8), add3);

            // secondary calculation chain
            var out10 = AddOutput(Tuple.Create(out0, out5), add2);

            // single input -> output example
            var out11 = AddOutput(in13, new NodeFunc<int, int>(x => x));
        }
    }