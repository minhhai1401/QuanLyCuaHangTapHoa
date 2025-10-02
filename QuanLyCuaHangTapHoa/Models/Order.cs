namespace QuanLyCuaHangTapHoa.Models
{
    using System;
    using System.Collections.Generic;

    public partial class Order
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Order()
        {
            this.DoanhThuViews = new HashSet<DoanhThuView>();
            this.Order_Details = new HashSet<Order_Detail>();
        }

        public int ID { get; set; }
        public string Status { get; set; }
        public string Address { get; set; }
        public Nullable<bool> Payment { get; set; }
        public Nullable<DateTime> NgayDat { get; set; }
        public Nullable<DateTime> NgayGiao { get; set; }
        public Nullable<double> ThanhTien { get; set; }
        public Nullable<int> TongSoLuong { get; set; }
        public Nullable<int> Id_NV { get; set; }
        public Nullable<int> ID_KH { get; set; }
        public DateTime? NgayCapNhat { get; set; }

        public virtual NhanVien NhanVien { get; set; }
        public virtual KhachHang KhachHang { get; set; }
        public virtual ICollection<DoanhThuView> DoanhThuViews { get; set; }
        public virtual ICollection<Order_Detail> Order_Details { get; set; }

        public string PaymentMethod { get; set; }
        public string ShippingName { get; set; }
        public string ShippingPhone { get; set; }
        public string ShippingAddress { get; set; }

        // Helper method để set thông tin shipping
        public void SetShippingInfo(ShippingInfo shippingInfo)
        {
            if (shippingInfo != null)
            {
                this.ShippingName = shippingInfo.ShippingName;
                this.ShippingPhone = shippingInfo.ShippingPhone;
                this.ShippingAddress = shippingInfo.ShippingAddress;
            }
        }
    }
}