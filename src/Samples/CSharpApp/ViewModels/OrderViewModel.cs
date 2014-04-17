namespace CSharpApp.ViewModels
{
    using System.ComponentModel;
    using Arcadia;
    using Arcadia.MVVM;
    using Data;
    using Models;

    public class OrderViewModel : PageViewModel
    {
        public OrderViewModel(IOrderCalculationEngine calculationEngine) : base()
        {
            Model = calculationEngine;
            AddOrderItemCommand = new ActionCommand(OnAddOrderItem);
        }

        public override string Name { get { return "Order"; } }

        public IOrderCalculationEngine Model { get; private set; }
        public ActionCommand AddOrderItemCommand {get; private set;}

        private void OnAddOrderItem()
        {
            Model.Order.Items.AddNew();
        }
    }
}