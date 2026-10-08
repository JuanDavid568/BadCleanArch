using Application.UseCases;
using Domain.Entities;
using Domain.Services;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;

namespace WebApi.Controllers;

[ApiController]
[Route("Orders")]

public class OrdersController : ControllerBase
{
    private readonly CreateOrderUseCase _createOrder;
    private readonly IOrderRepository _repository;
    private readonly IAppLogger _logger;
    private readonly IHostEnvironment _env;

    public OrdersController(
        CreateOrderUseCase createOrder,
        IOrderRepository repository,
        IAppLogger logger,
        IHostEnvironment env)
    {
        _createOrder = createOrder;
        _repository = repository;
        _logger = logger;
        _env = env;
    }   

    [HttpPost]
    [ProducesResponseType(typeof(Order), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)] 
    public IActionResult Create([FromBody] CreateOrderRequest data)
    {
         var order = _createOrder.Execute(
            data.Customer ?? "anon",
            data.Product ?? "unknown",
            data.Quantity ?? 1,
            data.Price ?? 0.99M);

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    [HttpGet]
    public IActionResult GetAll() => Ok(_repository.GetAll());

    [HttpGet("last")]
    [ProducesResponseType(typeof(List<Order>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetLast()
    {
        var order = _repository.GetRecent(1).FirstOrDefault();
        return order is null ? NotFound() : Ok(order);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Order), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetById(int id)
    {
        var order = _repository.GetById(id);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(Order), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Update(int id, [FromBody] CreateOrderRequest data)
    {
        if (_repository.GetById(id) is null)
            return NotFound();

        var order = new Order(
            data.Customer ?? "",
            data.Product ?? "",
            data.Quantity ?? 0,
            data.Price ?? 0)
        { Id = id };

        _repository.Update(order);
        return Ok(order);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(int id) =>
        _repository.Delete(id) ? NoContent() : NotFound();


    [HttpGet("~/health")]
    public IActionResult Health()

    {
        _logger.Log("Health ping");
        return Ok(new { status = "Ok" });
    }


    [HttpGet("~/info")]
    public IActionResult Info() =>
        Ok(new { environment = _env.EnvironmentName, version = "1.0.0" });
    
}