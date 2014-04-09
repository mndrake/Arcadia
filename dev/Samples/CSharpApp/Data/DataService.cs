namespace CSharpApp.Data
{
    using System;
    using System.ComponentModel;

    public class DataService : IDataService
    {
        public Inventory LoadInventory()
        {
            return new Inventory
            {
                Products =
                    new Product[]
                    {
                        new Product{ID=0,Name="<Select Item>",UnitPrice=0.0},
                        new Product{ ID = 1,Name = "Dharamsala Tea",UnitPrice = 18.0 },
                        new Product{ ID = 2,Name = "Tibetan Barley Beer",UnitPrice = 19.0 },
                        new Product{ ID = 3,Name = "Aniseed Syrup",UnitPrice = 10.0 },
                        new Product{ ID = 4,Name = "Chef Anton's Cajun Seasoning",UnitPrice = 22.0 },
                        new Product{ ID = 5,Name = "Chef Anton's Gumbo Mix",UnitPrice = 21.35 }
                    }
            };
        }

        public Order LoadOrder()
        {
        var items = new BindingList<OrderItem>();
        items.Add(new OrderItem{ProductId = 1, Units = 10});
        items.Add(new OrderItem{ProductId = 3, Units = 5});
        return new Order { ID = 12345, Date = DateTime.Parse("01-01-2014"), Tax = 0.07, Items = items };
        }
    }
}