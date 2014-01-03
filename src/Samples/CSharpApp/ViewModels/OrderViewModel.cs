namespace CSharpApp.ViewModels
{
    using System.ComponentModel;
    using Utopia;
    using Utopia.ViewModel;
    using Data;
    using Models;

    public class OrderViewModel : PageViewModel
    {
        IOrderCalculationEngine _calculationEngine;

        public OrderViewModel(IOrderCalculationEngine calculationEngine) : base()
        {
            _calculationEngine = calculationEngine;
            _calculationEngine.OrderResult.Changed += new ChangedEventHandler((sender, args) => RaisePropertyChanged("OrderResult"));

            AddOrderItemCommand = new ActionCommand(OnAddOrderItem);
        }

        public override string Name
        {
            get { return "Order"; }
        }

        public Order Order
        {
            get { return _calculationEngine.Order.Value; }
            set
            {
                _calculationEngine.Order.Value = value;
                RaisePropertyChanged("Order");
            }
        }

        public BindingList<OrderItem> Items
        {
            get { return _calculationEngine.Order.Value.Items; }
            set 
            { 
                _calculationEngine.Order.Value.Items = value;
                RaisePropertyChanged("Items");
            }
        }

        public Inventory Inventory
        {
            get { return _calculationEngine.Inventory.Value; }
            set
            {
                _calculationEngine.Inventory.Value = value;
                RaisePropertyChanged("Inventory");
            }
        }

        public OrderResult OrderResult { get { return _calculationEngine.OrderResult.Value; } }

        public ActionCommand AddOrderItemCommand {get; private set;}

        private void OnAddOrderItem()
        {
            _calculationEngine.Order.Value.Items.AddNew();
        }
    }
}