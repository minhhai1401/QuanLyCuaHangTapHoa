using QuanLyCuaHangTapHoa.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace QuanLyCuaHangTapHoa.Controllers
{
    public class KhachHangController : Controller
    {
        quantaphoaEntities _db = new quantaphoaEntities();
        // GET: KhachHang
        public ActionResult Index(string searchStr)
        {
            NhanVien nv = (NhanVien)Session["NV"];
            var dsKhachHang = _db.KhachHangs.ToList();

            // Tìm kiếm khách hàng trong quản lí khách hàng bằng email 
            if (!String.IsNullOrEmpty(searchStr))
            {
                searchStr = searchStr.ToLower();
                dsKhachHang = dsKhachHang.Where(s => s.Email.ToLower().Contains(searchStr)).ToList();
                ViewBag.dsKh = dsKhachHang;
            }
            else
            {
                ViewBag.dsKH = dsKhachHang;
            }

            return View();
        }
        // Thêm khách hàng mới 
        public ActionResult Create()
        {
            return View();
        }

        // POST: Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(KhachHang khachHang, HttpPostedFileBase file)
        {
            if (ModelState.IsValid)
            {
                var check = _db.KhachHangs.FirstOrDefault(s => s.Email == khachHang.Email);
                if (check == null)
                {
                    string extension = Path.GetExtension(file.FileName);
                    if (extension.Equals(".jpg") || extension.Equals(".png") || extension.Equals(".jpeg"))
                    {
                        string filename = Path.GetFileName(file.FileName);
                        string path = Path.Combine(Server.MapPath("~/Hinh/KhachHang"), filename);
                        khachHang.Picture = filename;
                        file.SaveAs(path);
                    }

                    khachHang.TichLuy = 0;
                    _db.KhachHangs.Add(khachHang);
                    _db.SaveChanges();
                    return RedirectToAction("Index");
                }
                else
                {
                    ViewBag.error = "Email đã tồn tại";
                    return View();
                }
            }
            return View();
        }
        public ActionResult Detail(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            KhachHang khachhang = _db.KhachHangs.Find(id);
            if (khachhang == null)
            {
                return HttpNotFound();
            }
            return View(khachhang);
        }
        public ActionResult Edit(int id)
        {
            if (id.ToString() == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            KhachHang nhanvien = _db.KhachHangs.Find(id);
            if (nhanvien == null)
            {
                return HttpNotFound();
            }
            return View(nhanvien);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "idUser,FirstName,LastName,Email,Picture,Address,NgaySinh,CMT,Sdt")] KhachHang kh, HttpPostedFileBase file)
        {
            try
            {
                var khachhang = _db.KhachHangs.Find(kh.idUser);
                if (khachhang == null)
                {
                    return HttpNotFound();
                }

                if (ModelState.IsValid)
                {
                    // Update basic information
                    khachhang.FirstName = kh.FirstName;
                    khachhang.LastName = kh.LastName;
                    khachhang.Email = kh.Email;
                    khachhang.Address = kh.Address;
                    khachhang.NgaySinh = kh.NgaySinh;
                    khachhang.CMT = kh.CMT;
                    khachhang.Sdt = kh.Sdt;

                    // Handle file upload
                    if (file != null && file.ContentLength > 0)
                    {
                        var fileName = Path.GetFileName(file.FileName);
                        var path = Path.Combine(Server.MapPath("~/Hinh/KhachHang"), fileName);

                        // Delete old image if exists
                        if (!string.IsNullOrEmpty(khachhang.Picture))
                        {
                            var oldPath = Path.Combine(Server.MapPath("~/Hinh/KhachHang"), khachhang.Picture);
                            if (System.IO.File.Exists(oldPath))
                            {
                                System.IO.File.Delete(oldPath);
                            }
                        }

                        // Save new image
                        file.SaveAs(path);
                        khachhang.Picture = fileName;
                    }

                    _db.Entry(khachhang).State = EntityState.Modified;
                    _db.SaveChanges();

                    return RedirectToAction("Index");
                }

                return View(kh);
            }
            catch (DbEntityValidationException ex)
            {
                foreach (var entityValidationErrors in ex.EntityValidationErrors)
                {
                    foreach (var validationError in entityValidationErrors.ValidationErrors)
                    {
                        ModelState.AddModelError("", validationError.ErrorMessage);
                    }
                }
                return View(kh);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            KhachHang khachhang = _db.KhachHangs.Find(id);
            if (khachhang == null)
            {
                return HttpNotFound();
            }
            return View(khachhang);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                KhachHang khachhang = _db.KhachHangs.Find(id);

                // Xóa ảnh cũ nếu có
                if (!string.IsNullOrEmpty(khachhang.Picture))
                {
                    var imagePath = Path.Combine(Server.MapPath("~/Hinh/KhachHang"), khachhang.Picture);
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                _db.KhachHangs.Remove(khachhang);
                _db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Log error và xử lý exception
                return RedirectToAction("Index");
            }
        }
    }
}

