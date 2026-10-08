namespace Domain.Entities;

public class Order
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public Order() { }

    public Order(string customerName, string productName, int quantity, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(customerName))
            throw new DomainException("El nombre del cliente es obligatorio.");

        if(string.IsNullOrWhiteSpace(productName))
            throw new DomainException("El nombre del producto es obligatorio.");

        if(quantity <= 0)
            throw new DomainException("La cantidad debe ser mayor a cero.");

        if(unitPrice < 0)
            throw new DomainException("El precio no puede ser negativo.");

        CustomerName = customerName;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;

    }

    public decimal CalculateTotal() => Quantity * UnitPrice;

}
