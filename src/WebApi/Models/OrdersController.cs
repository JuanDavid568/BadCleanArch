using System;
using System.Globalization;
using Domain.Entities;

namespace WebApi.Models;

public record CreateOrderRequest(string? Customer, string? Product, int? Quantity, decimal? Price)
{
   public static CreateOrderRequest FromCsv (string body)
    {
        var parts = body.Split(',',StringSplitOptions.TrimEntries);

        return new CreateOrderRequest(
            GetPart(parts, 0),
            GetPart(parts, 1),
            ParseInt(GetPart(parts, 2)),
            ParseDecimal(GetPart(parts, 3))
            );
    }
    private static string? GetPart(string[] parts, int index) =>
    index < parts.Length && parts[index].Length > 0 ? parts[index] : null;

    private static int? ParseInt(string? text)
    {
        if (text is null) return null;
        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            return value;
        throw new DomainException("La cantidad debe ser un numero entero.");
    }

    private static decimal? ParseDecimal(string? text)
    {
        if (text is null) return null;
        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
            return value;
        throw new DomainException("El precio debe ser un numero valido.");
    }

}
