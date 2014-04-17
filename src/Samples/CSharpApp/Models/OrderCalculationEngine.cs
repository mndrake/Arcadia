namespace CSharpApp.Models
{
    using System;
    using Arcadia;
    using Data;

    public class OrderCalculationEngine : CalculationEngine, IOrderCalculationEngine
    {
        Setable<Inventory> _inventory;
        Setable<Order> _order;
        Computed<OrderResult> _orderResult;

        public OrderCalculationEngine(IDataService data) : base()
        {
            // inputs
            _inventory = Setable(data.LoadInventory(), "Inventory");
            _order = Setable(data.LoadOrder(), "Order");

            // outputs
            _orderResult = Computed(() => OrderMethods.GetOrderResults(_order, _inventory), "OrderResult");
        }

        public Inventory Inventory
        {
            get { return _inventory; }
            set { _inventory.Value = value; }
        }

        public Order Order
        {
            get { return _order; }
            set { _order.Value = value; }
        }

        public OrderResult OrderResult 
        { 
            get { return _orderResult; } 
        }

        public bool AutoCalculate
        {
            get { return this.Calculation.Automatic; }
            set { this.Calculation.Automatic = value; }
        }
    }
}