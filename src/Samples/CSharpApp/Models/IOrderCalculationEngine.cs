namespace CSharpApp.Models
{
    using Arcadia;
    using Data;

    public interface IOrderCalculationEngine
    {
        INode<Inventory> Inventory { get;} 
        INode<Order> Order { get; }
        INode<OrderResult> OrderResult { get; }
        bool AutoCalculate { get; set; }
    }
}            