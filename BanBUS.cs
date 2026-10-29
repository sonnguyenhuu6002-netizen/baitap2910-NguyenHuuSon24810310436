using System.Collections.Generic;
using System.Data;
using CafePOS.DAL;
using CafePOS.DTO;

namespace CafePOS.BUS
{
    public static class BanBUS
    {
        public static List<BanDTO> GetAll() => BanDAL.GetAll();
        public static DataTable GetBanTrong() => BanDAL.GetBanTrong();
    }
}
