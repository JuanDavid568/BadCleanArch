using Application.UseCases;
using Domain.Entities;
using Domain.Services;
using Infrastructure.Data;
using Infrastructure.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using WebApi.Controllers;
using WebApi.Models;

namespace Tests;

// ---------- Fakes ----------

internal sealed class FakeLogger : IAppLogger
{
    public List<string> Messages { get; } = new();
    public void Log(string message) => Messages.Add(message);
}

internal sealed class FakeOrderRepository : IOrderRepository
{
    private readonly List<Order> _orders = new();
    private int _nextId = 1;

    public void Save(Order order)
    {
        order.Id = _nextId++;
        _orders.Add(order);
    }

    public List<Order> GetRecent(int count) =>
        _orders.OrderByDescending(o => o.Id).Take(count).ToList();

    public Order? GetById(int id) => _orders.FirstOrDefault(o => o.Id == id);

    public List<Order> GetAll() => _orders.ToList();

    public bool Update(Order order)
    {
        var index = _orders.FindIndex(o => o.Id == order.Id);
        if (index < 0) return false;
        _orders[index] = order;
        return true;
    }

    public bool Delete(int id) => _orders.RemoveAll(o => o.Id == id) > 0;
}

internal sealed class FakeHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Testing";
    public string ApplicationName { get; set; } = "Tests";
    public string ContentRootPath { get; set; } = string.Empty;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

// ---------- Domain ----------

public class OrderTests
{
    [Fact]
    public void Constructor_WithValidData_SetsProperties()
    {
        var order = new Order("Ana", "Laptop", 2, 10.5m);

        Assert.Equal("Ana", order.CustomerName);
        Assert.Equal("Laptop", order.ProductName);
        Assert.Equal(2, order.Quantity);
        Assert.Equal(10.5m, order.UnitPrice);
    }

    [Fact]
    public void DefaultConstructor_InitializesEmptyStrings()
    {
        var order = new Order();

        Assert.Equal(string.Empty, order.CustomerName);
        Assert.Equal(string.Empty, order.ProductName);
        Assert.Equal(0, order.Id);
    }

    [Fact]
    public void CalculateTotal_MultipliesQuantityByUnitPrice()
    {
        var order = new Order("Ana", "Laptop", 3, 2.5m);

        Assert.Equal(7.5m, order.CalculateTotal());
    }

    [Theory]
    [InlineData("", "Laptop", 1, 1, "cliente")]
    [InlineData("  ", "Laptop", 1, 1, "cliente")]
    [InlineData("Ana", "", 1, 1, "producto")]
    [InlineData("Ana", " ", 1, 1, "producto")]
    [InlineData("Ana", "Laptop", 0, 1, "cantidad")]
    [InlineData("Ana", "Laptop", -1, 1, "cantidad")]
    [InlineData("Ana", "Laptop", 1, -0.01, "precio")]
    public void Constructor_WithInvalidData_ThrowsDomainException(
        string customer, string product, int quantity, double price, string messagePart)
    {
        var ex = Assert.Throws<DomainException>(
            () => new Order(customer, product, quantity, (decimal)price));

        Assert.Contains(messagePart, ex.Message);
    }

    [Fact]
    public void Constructor_WithZeroPrice_IsAllowed()
    {
        var order = new Order("Ana", "Regalo", 1, 0m);

        Assert.Equal(0m, order.CalculateTotal());
    }

    [Fact]
    public void OrderService_Create_ReturnsOrderWithGivenData()
    {
        var order = OrderService.Create("Luis", "Mouse", 4, 5m);

        Assert.Equal("Luis", order.CustomerName);
        Assert.Equal(20m, order.CalculateTotal());
    }

    [Fact]
    public void OrderService_Create_WithInvalidData_Throws()
    {
        Assert.Throws<DomainException>(() => OrderService.Create("", "Mouse", 1, 1m));
    }
}

// ---------- Application ----------

public class CreateOrderUseCaseTests
{
    [Fact]
    public void Execute_SavesOrderAndLogs()
    {
        var repo = new FakeOrderRepository();
        var logger = new FakeLogger();
        var useCase = new CreateOrderUseCase(repo, logger);

        var order = useCase.Execute("Ana", "Laptop", 2, 10m);

        Assert.Equal(1, order.Id);
        Assert.Single(repo.GetAll());
        Assert.Equal(2, logger.Messages.Count);
        Assert.Contains("Total: 20", logger.Messages[1]);
    }

    [Fact]
    public void Execute_WithInvalidData_DoesNotSave()
    {
        var repo = new FakeOrderRepository();
        var useCase = new CreateOrderUseCase(repo, new FakeLogger());

        Assert.Throws<DomainException>(() => useCase.Execute("", "Laptop", 1, 1m));
        Assert.Empty(repo.GetAll());
    }
}

// ---------- WebApi: modelo ----------

public class CreateOrderRequestTests
{
    [Fact]
    public void FromCsv_WithAllFields_ParsesValues()
    {
        var request = CreateOrderRequest.FromCsv("Ana, Laptop, 2, 10.5");

        Assert.Equal("Ana", request.Customer);
        Assert.Equal("Laptop", request.Product);
        Assert.Equal(2, request.Quantity);
        Assert.Equal(10.5m, request.Price);
    }

    [Fact]
    public void FromCsv_WithMissingFields_ReturnsNulls()
    {
        var request = CreateOrderRequest.FromCsv("Ana");

        Assert.Equal("Ana", request.Customer);
        Assert.Null(request.Product);
        Assert.Null(request.Quantity);
        Assert.Null(request.Price);
    }

    [Fact]
    public void FromCsv_WithInvalidQuantity_Throws()
    {
        var ex = Assert.Throws<DomainException>(() => CreateOrderRequest.FromCsv("Ana,Laptop,abc,5"));

        Assert.Contains("cantidad", ex.Message);
    }

    [Fact]
    public void FromCsv_WithInvalidPrice_Throws()
    {
        var ex = Assert.Throws<DomainException>(() => CreateOrderRequest.FromCsv("Ana,Laptop,1,xyz"));

        Assert.Contains("precio", ex.Message);
    }
}

// ---------- Infrastructure ----------

public sealed class SqliteOrderRepositoryTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"orders-{Guid.NewGuid():N}.db");
    private readonly SqliteOrderRepository _repository;

    public SqliteOrderRepositoryTests()
    {
        _repository = new SqliteOrderRepository($"Data Source={_dbPath}");
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    [Fact]
    public void Save_AssignsIdAndPersists()
    {
        var order = new Order("Ana", "Laptop", 2, 10.5m);

        _repository.Save(order);

        Assert.True(order.Id > 0);
        var stored = _repository.GetById(order.Id);
        Assert.NotNull(stored);
        Assert.Equal("Ana", stored!.CustomerName);
        Assert.Equal(10.5m, stored.UnitPrice);
    }

    [Fact]
    public void GetById_WhenMissing_ReturnsNull()
    {
        Assert.Null(_repository.GetById(999));
    }

    [Fact]
    public void GetAll_ReturnsOrdersById()
    {
        _repository.Save(new Order("A", "P1", 1, 1m));
        _repository.Save(new Order("B", "P2", 1, 1m));

        var all = _repository.GetAll();

        Assert.Equal(2, all.Count);
        Assert.Equal("A", all[0].CustomerName);
    }

    [Fact]
    public void GetRecent_ReturnsNewestFirstLimitedByCount()
    {
        _repository.Save(new Order("A", "P1", 1, 1m));
        _repository.Save(new Order("B", "P2", 1, 1m));
        _repository.Save(new Order("C", "P3", 1, 1m));

        var recent = _repository.GetRecent(2);

        Assert.Equal(2, recent.Count);
        Assert.Equal("C", recent[0].CustomerName);
    }

    [Fact]
    public void Update_ExistingOrder_ReturnsTrueAndChangesData()
    {
        var order = new Order("A", "P1", 1, 1m);
        _repository.Save(order);
        var updated = new Order("Z", "P9", 5, 3m) { Id = order.Id };

        Assert.True(_repository.Update(updated));
        Assert.Equal("Z", _repository.GetById(order.Id)!.CustomerName);
    }

    [Fact]
    public void Update_MissingOrder_ReturnsFalse()
    {
        var order = new Order("A", "P1", 1, 1m) { Id = 12345 };

        Assert.False(_repository.Update(order));
    }

    [Fact]
    public void Delete_ExistingOrder_ReturnsTrue()
    {
        var order = new Order("A", "P1", 1, 1m);
        _repository.Save(order);

        Assert.True(_repository.Delete(order.Id));
        Assert.Null(_repository.GetById(order.Id));
    }

    [Fact]
    public void Delete_MissingOrder_ReturnsFalse()
    {
        Assert.False(_repository.Delete(999));
    }

    [Fact]
    public void Logger_WritesMessageToConsole()
    {
        var original = Console.Out;
        using var writer = new StringWriter();
        try
        {
            Console.SetOut(writer);
            new Logger().Log("hola");
        }
        finally
        {
            Console.SetOut(original);
        }

        Assert.Contains("[LOG]", writer.ToString());
        Assert.Contains("hola", writer.ToString());
    }
}

// ---------- WebApi: controlador ----------

public class OrdersControllerTests
{
    private readonly FakeOrderRepository _repository = new();
    private readonly FakeLogger _logger = new();
    private readonly OrdersController _controller;

    public OrdersControllerTests()
    {
        var useCase = new CreateOrderUseCase(_repository, _logger);
        _controller = new OrdersController(useCase, _repository, _logger, new FakeHostEnvironment());
    }

    [Fact]
    public void Create_WithData_ReturnsCreated()
    {
        var result = _controller.Create(new CreateOrderRequest("Ana", "Laptop", 2, 10m));

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var order = Assert.IsType<Order>(created.Value);
        Assert.Equal("Ana", order.CustomerName);
    }

    [Fact]
    public void Create_WithoutData_UsesDefaults()
    {
        var result = _controller.Create(new CreateOrderRequest(null, null, null, null));

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var order = Assert.IsType<Order>(created.Value);
        Assert.Equal("anon", order.CustomerName);
        Assert.Equal(1, order.Quantity);
        Assert.Equal(0.99m, order.UnitPrice);
    }

    [Fact]
    public void GetAll_ReturnsOk()
    {
        _controller.Create(new CreateOrderRequest("Ana", "Laptop", 1, 1m));

        var ok = Assert.IsType<OkObjectResult>(_controller.GetAll());
        Assert.Single(Assert.IsType<List<Order>>(ok.Value));
    }

    [Fact]
    public void GetLast_WhenEmpty_ReturnsNotFound()
    {
        Assert.IsType<NotFoundResult>(_controller.GetLast());
    }

    [Fact]
    public void GetLast_WithOrders_ReturnsNewest()
    {
        _controller.Create(new CreateOrderRequest("A", "P", 1, 1m));
        _controller.Create(new CreateOrderRequest("B", "P", 1, 1m));

        var ok = Assert.IsType<OkObjectResult>(_controller.GetLast());
        Assert.Equal("B", Assert.IsType<Order>(ok.Value).CustomerName);
    }

    [Fact]
    public void GetById_WhenMissing_ReturnsNotFound()
    {
        Assert.IsType<NotFoundResult>(_controller.GetById(42));
    }

    [Fact]
    public void GetById_WhenExists_ReturnsOk()
    {
        _controller.Create(new CreateOrderRequest("A", "P", 1, 1m));

        Assert.IsType<OkObjectResult>(_controller.GetById(1));
    }

    [Fact]
    public void Update_WhenMissing_ReturnsNotFound()
    {
        var result = _controller.Update(42, new CreateOrderRequest("A", "P", 1, 1m));

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void Update_WhenExists_ReturnsOkWithNewData()
    {
        _controller.Create(new CreateOrderRequest("A", "P", 1, 1m));

        var result = _controller.Update(1, new CreateOrderRequest("Z", "Q", 3, 2m));

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Z", Assert.IsType<Order>(ok.Value).CustomerName);
        Assert.Equal("Z", _repository.GetById(1)!.CustomerName);
    }

    [Fact]
    public void Update_WithInvalidData_ThrowsDomainException()
    {
        _controller.Create(new CreateOrderRequest("A", "P", 1, 1m));

        Assert.Throws<DomainException>(
            () => _controller.Update(1, new CreateOrderRequest(null, null, null, null)));
    }

    [Fact]
    public void Delete_WhenExists_ReturnsNoContent()
    {
        _controller.Create(new CreateOrderRequest("A", "P", 1, 1m));

        Assert.IsType<NoContentResult>(_controller.Delete(1));
    }

    [Fact]
    public void Delete_WhenMissing_ReturnsNotFound()
    {
        Assert.IsType<NotFoundResult>(_controller.Delete(1));
    }

    [Fact]
    public void Health_ReturnsOkAndLogs()
    {
        Assert.IsType<OkObjectResult>(_controller.Health());
        Assert.Contains("Health ping", _logger.Messages);
    }

    [Fact]
    public void Info_ReturnsOk()
    {
        Assert.IsType<OkObjectResult>(_controller.Info());
    }
}
