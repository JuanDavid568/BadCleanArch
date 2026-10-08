using System.Collections.Generic;
using Domain.Entities;

namespace Domain.Services;

public interface IOrderRepository
{
    void Save(Order order);
    List<Order> GetRecent(int count);

    Order? GetById(int id);
    List<Order> GetAll();
    bool Update(Order order);
    bool Delete(int id);

}

