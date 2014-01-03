namespace CSharpApp.Data
{
    public interface IDataService
    {
        Inventory LoadInventory();
        Order LoadOrder();
    }
}
