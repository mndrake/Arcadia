C# Example
----------

Snippet from C# example that can be found on GitHub site.

__version 0.2__

    [lang=csharp]
    public class OrderCalculationEngine : CalculationEngine, IOrderCalculationEngine
    {
        public OrderCalculationEngine(IDataService data)
            : base()
        {
            // inputs
            var inventory = Setable(data.LoadInventory(), "Inventory");
            var order = Setable(data.LoadOrder(), "Order");

            // outputs
            var orderResult = Computed(() => OrderMethods.GetOrderResults(order, inventory), "OrderResult");

            Inventory = inventory;
            Order = order;
            OrderResult = orderResult;
        }

        public INode<Inventory> Inventory { get; private set; }

        public INode<Order> Order { get; private set; }

        public INode<OrderResult> OrderResult { get; private set; }

        public bool AutoCalculate
        {
            get { return this.Calculation.Automatic; }
            set { this.Calculation.Automatic = value; }
        }
    }

__version 0.1__

    [lang=csharp]
    public class OrderCalculationEngine : CalculationEngine, IOrderCalculationEngine
    {
        public OrderCalculationEngine(IDataService data)
            : base()
        {
            // inputs
            var inventory = AddInput(data.LoadInventory(), "Inventory");
            var order = AddInput(data.LoadOrder(), "Order");

            // outputs
            var orderResult = AddOutput(Tuple.Create(order, inventory),
                              new NodeFunc<Tuple<Order, Inventory>, OrderResult>(OrderMethods.GetOrderResults),
                              "OrderResult");

            Inventory = inventory;
            Order = order;
            OrderResult = orderResult;
        }

        public INode<Inventory> Inventory { get; private set; }

        public INode<Order> Order { get; private set; }

        public INode<OrderResult> OrderResult { get; private set; }

        public bool AutoCalculate
        {
            get { return this.Calculation.Automatic; }
            set { this.Calculation.Automatic = value; }
        }
    }