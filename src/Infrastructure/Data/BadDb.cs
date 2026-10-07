using System;
using System.Data;
using System.Data.SqlClient;

namespace Infrastructure.Data;

using System.Data;
using System.Data.SqlClient;

public static class BadDb
{
    public static string ConnectionString = "Server=localhost;Database=master;User Id=sa;Password=SuperSecret123!;TrustServerCertificate=True";


    public static int ExecuteNonQueryUnsafe(string sql)
    {
        var conn = new SqlConnection(ConnectionString);
        var cmd = new SqlCommand(sql, conn);
        conn.Open();
        return cmd.ExecuteNonQuery();
    }

    public static IDataReader ExecuteReaderUnsafe(string sql)
    {
        var conn = new SqlConnection(ConnectionString);
        var cmd = new SqlCommand(sql, conn);
        conn.Open();
        return cmd.ExecuteReader(); 
}
