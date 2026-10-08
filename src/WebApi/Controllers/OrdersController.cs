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
    [Consumes("text/plain")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> CreateFromCsv() 
    {
        using var reader = new StreamReader(Request.Body);
        var data = CreateOrderRequest.FromCsv(await reader.ReadToEndAsync());
        return Ok(Execute(data));
    }
      
    [HttpGet("last")]
    public IActionResult GetLast() => Ok(_repository.GetRecent(10));

    [HttpGet("~/health")]
    public IActionResult Health() 
    {
        _logger.Log("health ping");
        return Ok(new { status = "ok" });   
    }
    [HttpGet("~/info")]
    public IActionResult Info() =>
        Ok(new { environment = _env.EnvironmentName, version = "1.0.0" });

    private Order Execute(CreateOrderRequest data) =>
        _createOrder.Execute(
            data.Customer ?? "anon",
            data.Product ?? "unknown",
            data.Quantity ?? 1,
            data.Price ?? 0.99M);
}