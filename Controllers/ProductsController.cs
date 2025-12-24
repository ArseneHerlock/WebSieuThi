using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using DoAn.Models;
using PagedList;
using PagedList.Mvc;

namespace DoAn.Controllers
{
    public class ProductsController : Controller
    {
        private QLSieuThiEntities db = new QLSieuThiEntities();

        // GET: Products
        public ActionResult Index(int? id,int? sorted,int? page,string kw=null)
        {
            var products = db.Products.AsQueryable();
            products = products.Where(x => x.Status ==true);
            if (id.HasValue)
            {
                products = products.Where(x => x.CategoryID == id);
            }
            if (!string.IsNullOrEmpty(kw))
            {
                products = products.Where(x => x.ProductName.Contains(kw));
            }
            if (sorted.HasValue)
            {
                switch (sorted.Value)
                {
                    case 1:
                        products = products.OrderBy(x => x.Price);
                        break;
                    case 2:
                        products = products.OrderByDescending(x => x.Price);
                        break;
                    case 3:
                        products = products.OrderByDescending(x => x.ProductName);
                        break;
                    case 4:
                        products = products.OrderBy(x => x.ProductName);
                        break;
                }
            }
            else
            {
                products = products.OrderBy(x => x.ProductID);
            }
            int pageSize = 9;
            int pageNumber= page ?? 1;
            var pagedProducts = products.ToPagedList(pageNumber, pageSize);


            return View(pagedProducts);
        }

        public ActionResult DanhMuc()
        {
            var dm = db.CategoryGroups.Include("Categories").ToList();
            return PartialView(dm);
        }
        
        public ActionResult DangNhap(UserAccount model)
        {
            var user = db.UserAccounts.FirstOrDefault(x => x.Email == model.Email && x.PasswordHash == model.PasswordHash && x.Status == true);
            if (user != null)
            {
                if (user.Email == "admin@gmail.com")
                {
                    Session["admin"] = user;
                    return RedirectToAction("Index_Admin");
                }
                Session["user"] = user;
                return RedirectToAction("Index");
            }
            ViewBag.ThongBaoDangNhap = "Tài Khoản Không Hợp Lệ";
            return View();
        }
        public ActionResult DangKy(UserAccount model)
        {
            if (!string.IsNullOrEmpty(model.Email))
            {
                var user = db.UserAccounts.FirstOrDefault(x => x.Email != model.Email);
                if (user != null)
                {
                    model.CreatedAt = DateTime.Now;
                    model.Status = true;
                    model.RoleAccount = false;
                    db.UserAccounts.Add(model);
                    db.SaveChanges();
                    return RedirectToAction("DangNhap");
                }
            }
            
            ViewBag.ThongBaoDangKy = "Tài Khoản Không Hợp Lệ";
            return View();
        }
        [HttpPost]
        public ActionResult Cart(AddToCart model)
        {
            var giohang = new Dictionary<int, AddToCart>();
            if (Session["giohang"] != null)
            {
                giohang = (Dictionary<int, AddToCart>)Session["giohang"];
            }
            if (model.ProductID.HasValue&&giohang.Keys.Contains(model.ProductID.Value))
            {
                giohang[model.ProductID.Value] = model;
            }
            else
            {
                giohang.Add(model.ProductID.Value, model);
            }

                Session["giohang"] = giohang;
            return RedirectToAction("Index");
        }
        public ActionResult DetailCart()
        {
            var giohang = new Dictionary<int, AddToCart>();
            if (Session["giohang"] != null)
            {
                giohang = (Dictionary<int, AddToCart>)Session["giohang"];
            }
            var products = db.Products.Where(x => giohang.Keys.Contains(x.ProductID));
            return View(products.ToList());
        }
        public ActionResult Index_Admin()
        {
            ViewBag.TongSP = db.Products.Count();
            ViewBag.TongTonKho = db.Products.Sum(x => x.SoLuongTon);
            var daGiao = db.Orders.Where(x => x.OrderStatus == "Đã nhận hàng").ToList();
            if (daGiao.Count > 0)
            {
                ViewBag.DoanhThuThang = daGiao.Sum(x => x.TotalAmount);
            }
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var order = db.Orders.Include("UserAccount").Where(x=>x.OrderDate>=today&&x.OrderDate<=tomorrow).ToList();
            return View(order);
        }
        public ActionResult Create_SP()
        {
            var dm = db.CategoryGroups.Include("Categories").ToList();
            return View(dm);
        }
        
        [HttpPost]
        public ActionResult Create_SP(Product sp)
        {
            sp.CreatedAt = DateTime.Now;
            db.Products.Add(sp);
            db.SaveChanges();
            return RedirectToAction("Index_Admin");
        }
        public ActionResult SanPham_Admin(int? page)
        {
            
            var sp = db.Products.OrderBy(x=>x.ProductID);
            var pageSize = 10;
            var pageNumber = page ?? 1;
            var productPage = sp.ToPagedList(pageNumber, pageSize);
            return View(productPage);
        }

        [HttpPost]
        public ActionResult DelSanPham_Admin(int? id)
        {
            if (id == null)
                return HttpNotFound(); // hoặc redirect về danh sách sản phẩm

            var sp = db.Products.Find(id);
            if (sp == null)
                return HttpNotFound();

            db.Products.Remove(sp);
            db.SaveChanges();

            return RedirectToAction("SanPham_Admin"); // redirect về trang danh sách sản phẩm
        }
        public ActionResult Order_Admin()
        {
            var order = db.Orders.Where(x=>x.OrderStatus== "Chờ Xử Lý").ToList();
            return View(order);
        }

        [HttpPost]
        public ActionResult Order_Admin(int? id,int? OrderStatus)
        {
            if (id == null) return HttpNotFound();
            var order = db.Orders.Find(id);
            if (OrderStatus.HasValue)
            {
                switch (OrderStatus)
                {
                    case 1:
                        order.OrderStatus = "Chờ Xử Lý";
                        break;

                    case 2:
                        order.OrderStatus = "Đã nhận hàng";
                        break;

                }
                
            }
            db.Entry(order).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Order_Admin");
        }


        // GET: Products/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            ViewBag.SPLQ = db.Products.Where(x => x.ProductID != id&&x.CategoryID==product.CategoryID).ToList();
            return View(product);
        }

        // GET: Products/Create
        public ActionResult Create()
        {
            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName");
            return View();
        }

        // POST: Products/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ProductID,ProductName,CategoryID,Price,Discount,Description,ImageURL,Status,CreatedAt,AvgRating,RatingCount")] Product product)
        {
            if (ModelState.IsValid)
            {
                db.Products.Add(product);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", product.CategoryID);
            return View(product);
        }

        // GET: Products/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", product.CategoryID);
            return View(product);
        }

        // POST: Products/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ProductID,ProductName,CategoryID,Price,Discount,Description,ImageURL,Status,CreatedAt,AvgRating,RatingCount")] Product product)
        {
            if (ModelState.IsValid)
            {
                db.Entry(product).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", product.CategoryID);
            return View(product);
        }

        // GET: Products/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Product product = db.Products.Find(id);
            db.Products.Remove(product);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
