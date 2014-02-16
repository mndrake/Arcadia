namespace CSharpApp.Data
{
    using System;
    using System.ComponentModel;
    using Arcadia.MVVM;

    public class Product
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public double UnitPrice { get; set; }
    }

    public class Inventory
    {
        public Product[] Products { get; set; }
    }

    public class OrderItem : ObservableObject
    {
        int _productId;
        int _units;

        public OrderItem()
        {
            _productId = 0;
            _units = 0;
        }

        public int ProductId
        {
            get { return _productId; }
            set
            {
                _productId = value;
                RaisePropertyChanged("ProductId");
            }
        }

        public int Units
        {
            get { return _units; }
            set
            {
                _units = value;
                RaisePropertyChanged("Units");
            }
        }
    }

    public class Order : ObservableObject
    {
        int _id;
        DateTime _date;
        double _tax;
        BindingList<OrderItem> _items;

        public int ID
        {
            get { return _id; }
            set
            {
                _id = value;
                RaisePropertyChanged("ID");
            }
        }

        public DateTime Date
        {
            get { return _date; }
            set
            {
                _date = value;
                RaisePropertyChanged("Date");
            }
        }

        public BindingList<OrderItem> Items
        {
            get { return _items; }
            set
            {
                _items = value;
                _items.ListChanged += new ListChangedEventHandler((sender, args) => RaisePropertyChanged("Items"));
            }
        }

        public double Tax
        {
            get { return _tax; }
            set
            {
                _tax = value;
                RaisePropertyChanged("Tax");
            }
        }
    }

    public class OrderResult
    {
        public int TotalUnits { get; set; }
        public double PreTaxAmount { get; set; }
        public double TaxAmount { get; set; }
        public double TotalAmount { get; set; }
    }
}