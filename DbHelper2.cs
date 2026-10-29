using System.Data;
using Microsoft.Data.SqlClient;

namespace InventoryManagement.DAL
{
    public static class DbHelper
    {
        public static string ConnectionString =
            @"Server=DESKTOP-5NDKGQD\SQLEXPRESS;Database=InventoryDB;Trusted_Connection=True;TrustServerCertificate=True;";

        public static SqlConnection GetConnection() => new SqlConnection(ConnectionString);

        /// <summary>Đổi lỗi SQL thường gặp thành thông báo dễ hiểu.</summary>
        public static Exception Translate(SqlException ex)
        {
            switch (ex.Number)
            {
                case 2601:
                case 2627:
                    return new InvalidOperationException("Dữ liệu bị trùng (mã hoặc số điện thoại đã tồn tại).");
                case 547:
                    return new InvalidOperationException(
                        "Vi phạm ràng buộc: bản ghi đang được sử dụng ở bảng khác hoặc giá trị không hợp lệ.");
                default:
                    return ex;
            }
        }

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
            try
            {
                using var conn = GetConnection();
                using var cmd = new SqlCommand(sql, conn);
                if (ps != null) cmd.Parameters.AddRange(ps);
                conn.Open();
                return cmd.ExecuteScalar();
            }
            catch (SqlException ex) { throw Translate(ex); }
        }

        public static int ExecuteNonQuery(string sql, params SqlParameter[] ps)
        {
            try
            {
                using var conn = GetConnection();
                using var cmd = new SqlCommand(sql, conn);
                if (ps != null) cmd.Parameters.AddRange(ps);
                conn.Open();
                return cmd.ExecuteNonQuery();
            }
            catch (SqlException ex) { throw Translate(ex); }
        }
    }
}
