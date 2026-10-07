using System;
using System.Collections.Generic;

namespace Domain.Entities;

public class Order
{
    public int Id;
    public string CustomerName;
    public string ProductName;
    public int Quantity;
    public decimal UnitPrice;

    public void CalculateTotalAndLog()
    {
        var total = Quantity * UnitPrice; 
        Infrastructure.Logging.Logger.Log("Total (maybe): " + total);
    }
}
