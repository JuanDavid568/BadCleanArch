using System.Collections.Generic;
using Domain.Entities;

namespace Domain.Services;
public interface IOrderRepository
{
    void Save(Order order);
    List<Order> GetRecent(int count);
}

