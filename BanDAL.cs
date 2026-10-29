using System.Collections.Generic;
using System.Data;
using CafePOS.DTO;

namespace CafePOS.DAL
{
    public static class BanDAL
    {
        public static List<BanDTO> GetAll()
        {
            var list = new List<BanDTO>();
            DataTable dt = DataProvider.ExecuteQuery(
                "SELECT MaBan, TenBan, TrangThai FROM BanAn ORDER BY MaBan");
            foreach (DataRow r in dt.Rows)
                list.Add(new BanDTO
                {
                    MaBan = (int)r["MaBan"],
                    TenBan = (string)r["TenBan"],
                    TrangThai = (string)r["TrangThai"]
                });
            return list;
        }

        public static DataTable GetBanTrong() =>
            DataProvider.ExecuteQuery(
                "SELECT MaBan, TenBan FROM BanAn WHERE TrangThai = N'Trống' ORDER BY MaBan");
    }
}
