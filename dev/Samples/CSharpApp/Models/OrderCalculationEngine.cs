namespace CSharpApp.Models
{
    using System;
    using Arcadia;
    using Data;

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
}