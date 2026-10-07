using System;
using System.Collections.Generic;

namespace Domain.Services;

using Domain.Entities;

public static class OrderService
{
    public static List<Order> LastOrders = new List<Order>();

    public static Order CreateTerribleOrder(string customer, string product, int qty, decimal price)
    {
        var o = new Order { Id = new Random().Next(1, 9999999), CustomerName = customer, ProductName = product, Quantity = qty, UnitPrice = price };
        LastOrders.Add(o);
        Infrastructure.Logging.Logger.Log("Created order " + o.Id + " for " + customer);
        return o;
    }
}
