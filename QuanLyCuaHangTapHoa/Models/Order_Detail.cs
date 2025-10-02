namespace QuanLyCuaHangTapHoa.Models
{
    using System;
    using System.Collections.Generic;

    public partial class Order_Detail
    {
        public int ID_Order { get; set; }
        public int ID_Product { get; set; }
        public Nullable<int> SoLuong { get; set; }
        public Nullable<double> Price { get; set; }

        public virtual Order Order { get; set; }
        public virtual Product Product { get; set; }
    }
}
