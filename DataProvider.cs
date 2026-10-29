using System.Configuration;
using System.Data;
using Microsoft.Data.SqlClient;

namespace CafePOS.DAL
{
    /// <summary>Lop truy cap du lieu dung chung (SqlDataAdapter + DataTable).</summary>
    public static class DataProvider
    {
        private static readonly string _cs =
            ConfigurationManager.ConnectionStrings["CafePOSDB"].ConnectionString;

        public static SqlConnection CreateConnection() => new SqlConnection(_cs);

        // Mo hinh ngat ket noi: Fill xong la dong ket noi ngay
        public static DataTable ExecuteQuery(string sql, params SqlParameter[] ps)
        {
            var dt = new DataTable();
            using (var conn = CreateConnection())
            using (var da = new SqlDataAdapter(sql, conn))
            {
                da.SelectCommand.Parameters.AddRange(ps);
                da.Fill(dt);
            }
            return dt;
        }

        public static int ExecuteNonQuery(string sql, params SqlParameter[] ps)
        {
            using (var conn = CreateConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddRange(ps);
                conn.Open();
                return cmd.ExecuteNonQuery();
            }
        }
    }
}
