using System;
using System.Collections.Generic;
using System.IO;
using Domain.Entities;
using Domain.Services;
using Microsoft.Data.Sqlite;

namespace Infrastructure.Data;


public class SqliteOrderRepository : IOrderRepository
{
   private readonly string _connectionString;

   public SqliteOrderRepository(string connectionString)
    {
        _connectionString = connectionString;
        EnsureDatabase();
    }

    private void EnsureDatabase()
    {
        var csb = new SqliteConnectionStringBuilder(_connectionString);
        var directory = Path.GetDirectoryName(Path.GetFullPath(csb.DataSource));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        const string sql = @"CREATE TABLE IF NOT EXISTS Orders (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Customer TEXT NOT NULL,
                Product TEXT NOT NULL,
                Quantity INTEGER NOT NULL CHECK (Quantity > 0),
                Price TEXT NOT NULL
            );";

        using var conection = new SqliteConnection(_connectionString);
        using var command = new SqliteCommand(sql, conection);
        conection.Open();
        command.ExecuteNonQuery();
    }

    public void Save(Order order)
    {
        const string sql = @"INSERT INTO Orders (Customer, Product, Quantity, Price) 
                             VALUES (@Customer, @Product, @Quantity, @Price);
                                SELECT last_insert_rowid()";


        using var connection = new SqliteConnection(_connectionString);
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@Customer", order.CustomerName);
        command.Parameters.AddWithValue("@Product", order.ProductName);
        command.Parameters.AddWithValue("@Quantity", order.Quantity);
        command.Parameters.AddWithValue("@Price", order.UnitPrice);

        connection.Open();
        order.Id = Convert.ToInt32(command.ExecuteScalar());
    }
    public List<Order> GetRecent(int count)
    {
        const string sql = @"SELECT Id, Customer, Product, Quantity, Price 
                             FROM Orders 
                             ORDER BY Id DESC 
                             LIMIT @Count;";

        var orders = new List<Order>();

        using var connection = new SqliteConnection(_connectionString);
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@Count", count);
        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            orders.Add(new Order 
            {
                Id = reader.GetInt32(0),
                CustomerName = reader.GetString(1),
                ProductName = reader.GetString(2),
                Quantity = reader.GetInt32(3),
                UnitPrice = reader.GetDecimal(4)
            });
        }
        return orders;
        
    }
}
