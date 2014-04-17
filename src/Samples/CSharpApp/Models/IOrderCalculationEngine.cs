namespace CSharpApp.Models
{
    using Arcadia;
    using Data;

    public interface IOrderCalculationEngine
    {
        Inventory Inventory { get; set; }
        Order Order { get; set; }
        OrderResult OrderResult { get; }
        bool AutoCalculate { get; set; }
    }
}            