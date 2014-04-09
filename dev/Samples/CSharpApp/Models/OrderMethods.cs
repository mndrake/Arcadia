namespace CSharpApp.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Arcadia;
    using Data;

    public static class OrderMethods
    {
        public static OrderResult GetOrderResults(Order order, Inventory inventory)
        {
            Thread.Sleep(1000);
            var products = inventory.Products.ToDictionary(product => product.ID);
            var preTaxAmount = order.Items.Sum(item => products[item.ProductId].UnitPrice * item.Units);
            return new OrderResult 
            { 
                TotalUnits = order.Items.Sum(item => item.Units),
                PreTaxAmount = preTaxAmount,
                TaxAmount = preTaxAmount * order.Tax,
                TotalAmount = preTaxAmount * (1 + order.Tax)
            };
        }
    }
}