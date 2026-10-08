using Domain.Entities;

namespace Domain.Services;

public static class OrderService
{

    public static Order Create(string customer, string product, int quantity, decimal price) 
    { 
        return new Order(customer, product, quantity, price);
    }
    
}
