namespace QuanLyCuaHangTapHoa.Models
{
    using System;
    using System.Collections.Generic;

    public partial class DoanhThuView
    {
        public int Id { get; set; }
        public string Thang { get; set; }
        public double TongDoanhThu { get; set; }
        public Nullable<System.DateTime> NgayTao { get; set; }
        public Nullable<int> OrderId { get; set; }
        public Nullable<int> NhanVienId { get; set; }

        public virtual NhanVien NhanVien { get; set; }
        public virtual Order Order { get; set; }
    }
}

