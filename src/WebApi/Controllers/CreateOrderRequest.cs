using Application.UseCases;
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
    public async Task<IActionResult> Create ()
    {
        CreateOrderRequest data;

        if (!Request.HasJsonContentType())
        {
            data = await Request.ReadFromJsonAsync<CreateOrderRequest>()
            ?? new CreateOrderRequest(null, null, null, null);
        }
        else
        {
            using var reader = new StreamReader(Request.Body);
            data = CreateOrderRequest.FromCsv(await reader.ReadToEndAsync());
        }
        var order = _createOrder.Execute(
            data.Customer ?? "anon",
            data.Product ?? "unknown",
            data.Quantity ?? 1,
            data.Price ?? 0.99m);
        return Ok(order);
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
}