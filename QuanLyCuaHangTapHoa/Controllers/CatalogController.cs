using QuanLyCuaHangTapHoa.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Windows.Input;

namespace QuanLyCuaHangTapHoa.Controllers
{
    public class CatalogController : Controller
    {
        quantaphoaEntities _db = new quantaphoaEntities();
        public ActionResult Index()
        {
            NhanVien nv = (NhanVien)Session["NV"];
            if (nv.MaChucVu == 1)
            {
                return RedirectToAction("Index", "Home");
            }
            var catalog = (from s
                           in _db.Catalogs
                           select s).ToList();
            ViewBag.catalogs = catalog;
            return View();

        }
        public ActionResult Create()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Catalog model)
        {
            if (ModelState.IsValid)
            {
                var catalogs = (from s in _db.Catalogs select s).ToList();
                model.ID = catalogs.Any() ? catalogs.Last().ID + 1 : 1;

                try
                {
                    _db.Catalogs.Add(model);
                    _db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi khi lưu vào cơ sở dữ liệu: " + ex.Message);
                }
            }
            return View(model);
        }

        public ActionResult Edit(int ID)
        {
            if (ID == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Catalog catalog = _db.Catalogs.Find(ID);
            if (catalog == null)
            {
                return HttpNotFound();
            }
            return View(catalog);
        }
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public ActionResult EditPost([Bind(Include = "ID,CatalogCode,CatalogName")] Catalog catalogs)
        {
            Catalog catalog = _db.Catalogs.Find(catalogs.ID);
            if (ModelState.IsValid)
            {
                catalog.ID = catalogs.ID;
                catalog.CatalogCode = catalogs.CatalogCode;
                catalog.CatalogName = catalogs.CatalogName;
                _db.Entry(catalog).State = EntityState.Modified;
                _db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(catalog);
        }
        public ActionResult Delete(int ID)
        {

            if (ID == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Catalog catalog = _db.Catalogs.Find(ID);
            if (catalog == null)
            {
                return HttpNotFound();
            }
            return View(catalog);
        }
        // POST: KhachHang/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int ID)
        {
            Catalog catalog = _db.Catalogs.Find(ID);
            _db.Catalogs.Remove(catalog);
            _db.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}