using System.Data;
using Microsoft.Data.SqlClient;

namespace HotelManagement.DAL
{
    public static class DbHelper
    {
        // Sửa Server theo tên server hiển thị khi đăng nhập SSMS (vd: .\SQLEXPRESS, localhost, tên-máy)
        public static string ConnectionString =
            @"Server=.;Database=HotelDB;Trusted_Connection=True;TrustServerCertificate=True;";

        public static SqlConnection GetConnection() => new SqlConnection(ConnectionString);

        /// <summary>Chế độ ngắt kết nối: đổ dữ liệu vào DataTable rồi đóng kết nối.</summary>
        public static DataTable GetTable(string sql, params SqlParameter[] ps)
        {
            using var conn = GetConnection();
            using var da = new SqlDataAdapter(sql, conn);
            if (ps != null) da.SelectCommand.Parameters.AddRange(ps);
            var dt = new DataTable();
            da.Fill(dt);
            return dt;
        }

        public static object ExecuteScalar(string sql, params SqlParameter[] ps)
        {
            using var conn = GetConnection();
            using var cmd = new SqlCommand(sql, conn);
            if (ps != null) cmd.Parameters.AddRange(ps);
            conn.Open();
            return cmd.ExecuteScalar();
        }

        public static int ExecuteNonQuery(string sql, params SqlParameter[] ps)
        {
            using var conn = GetConnection();
            using var cmd = new SqlCommand(sql, conn);
            if (ps != null) cmd.Parameters.AddRange(ps);
            conn.Open();
            return cmd.ExecuteNonQuery();
        }
    }
}
