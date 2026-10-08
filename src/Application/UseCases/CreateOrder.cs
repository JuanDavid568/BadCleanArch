using Domain.Entities;
using Domain.Services;

namespace Application.UseCases;

public class CreateOrderUseCase
{
    private readonly IOrderRepository _orderRepository;

    private readonly IAppLogger _logger;

    public CreateOrderUseCase(IOrderRepository orderRepository, IAppLogger logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }
    public Order Execute(string customer, string product, int quantity, decimal price)
    {
        _logger.Log("CreateOrderUseCase Starting");

        var order = OrderService.Create(customer, product, quantity, price);
        _orderRepository.Save(order);

        _logger.Log($"Orden{order.Id} creada para el cliente {customer}. Total: {order.CalculateTotal}");

        return order;
    }
}
