using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
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
        public ActionResult Index(int? id, int? sorted, int? page, string kw = null)
        {
            var products = db.Products.AsQueryable();
            products = products.Where(x => x.Status == true);
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
            int pageNumber = page ?? 1;
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
            if (model.ProductID.HasValue && giohang.Keys.Contains(model.ProductID.Value))
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

        public ActionResult CheckOut()
        {
            if (Session["user"] == null) return RedirectToAction("DangNhap");
            if (Session["giohang"] == null) return RedirectToAction("Index");
            var giohang = new Dictionary<int, AddToCart>();
            if (Session["giohang"] != null)
            {
                giohang = (Dictionary<int, AddToCart>)Session["giohang"];
            }
            if (giohang.Count() == 0) return RedirectToAction("Index");

            var products = db.Products.Where(x => giohang.Keys.Contains(x.ProductID));
            return View(products.ToList());
        }
        [HttpPost]
        public ActionResult CheckOut(Order model)
        {
            var user = (UserAccount)Session["user"];
            model.UserID = user.UserID;
            model.Status = true;
            model.OrderDate = DateTime.Now;
            model.OrderStatus = "Chờ Xử Lý";
            var giohang = new Dictionary<int, AddToCart>();
            if (Session["giohang"] != null)
            {
                giohang = (Dictionary<int, AddToCart>)Session["giohang"];
            }
            var product = db.Products.Where(x => giohang.Keys.Contains(x.ProductID)).ToList();
            model.TotalAmount = product.Sum(x => x.Price * giohang[x.ProductID].Quantity);
            model.OrderDetails = new List<OrderDetail>();
            foreach(var pro in product)
            {
                pro.SoLuongBan =(pro.SoLuongBan??0)+ giohang[pro.ProductID].Quantity;
                pro.SoLuongTon -= giohang[pro.ProductID].Quantity;
                var item = new OrderDetail();
                item.ProductID = pro.ProductID;
                item.Quantity = giohang[pro.ProductID].Quantity;
                item.Price = pro.Price;
                model.OrderDetails.Add(item);
            }
            
            db.Orders.Add(model);
            db.SaveChanges();
            var lichsu = new Dictionary<int, Order>();

            if (Session["lichsudat"] != null)
            {
                lichsu = (Dictionary<int, Order>)Session["lichsudat"];
            }
            lichsu.Add(model.OrderID, model);
            Session["lichsudat"] = lichsu;
            return RedirectToAction("Success", new { id = model.OrderID });
        }

        public ActionResult Success(int? id)
        {
            var order = db.Orders.FirstOrDefault(x => x.OrderID == id);
            return View(order);
        }
        public ActionResult LichSu()
        {
            var lichsu = new Dictionary<int, Order>();
            
            if (Session["lichsudat"] != null)
            {
                lichsu = (Dictionary<int, Order>)Session["lichsudat"];
            }
            
            var ls = db.Orders.Where(x => lichsu.Keys.Contains(x.OrderID)).ToList();
            return View(ls);
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
            var order = db.Orders.Include("UserAccount").Where(x => x.OrderDate >= today && x.OrderDate <= tomorrow).ToList();
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

            var sp = db.Products.OrderBy(x => x.ProductID);
            var pageSize = 10;
            var pageNumber = page ?? 1;
            var productPage = sp.ToPagedList(pageNumber, pageSize);
            return View(productPage);
        }

        [HttpPost]
        public ActionResult DelSanPham_Admin(int? id)
        {
            if (id == null)
                return HttpNotFound();

            var sp = db.Products.Find(id);
            if (sp == null)
                return HttpNotFound();
            sp.Status = false;
            db.SaveChanges();

            return RedirectToAction("SanPham_Admin"); 
        }
        public ActionResult Order_Admin()
        {
            var order = db.Orders.Where(x => x.OrderStatus == "Chờ Xử Lý").ToList();
            return View(order);
        }

        [HttpPost]
        public ActionResult Order_Admin(int? id, int? OrderStatus)
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
        // --- QUẢN LÝ DANH MỤC (LOẠI) ---

        // 1. Trang danh sách loại
        public ActionResult DanhMuc_Admin()
        {
            var categories = db.Categories.ToList();
            return View(categories);
        }

        // 2. Thêm mới loại - POST
        [HttpPost]
        public ActionResult Create_Cate(string NameCate)
        {
            if (!string.IsNullOrEmpty(NameCate))
            {
                try
                {
                    Category cat = new Category();
                    cat.CategoryName = NameCate;

                    // QUAN TRỌNG: Gán GroupID mặc định để không bị lỗi Database
                    // Bạn hãy kiểm tra trong bảng CategoryGroups xem có mã ID nào (ví dụ: 1) rồi điền vào đây
                    db.Categories.Add(cat);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    // Nếu vẫn lỗi, dòng này sẽ giúp bạn biết lỗi cụ thể là gì
                    TempData["Error"] = "Lỗi hệ thống: " + ex.Message;
                }
            }
            return RedirectToAction("DanhMuc_Admin");
        }

        // 3. Sửa loại - POST
        [HttpPost]
        public ActionResult Edit_Cate(int IDCate, string NameCate)
        {
            var category = db.Categories.Find(IDCate);
            if (category != null && !string.IsNullOrEmpty(NameCate))
            {
                category.CategoryName = NameCate;
                db.Entry(category).State = EntityState.Modified;
                db.SaveChanges();
            }
            return RedirectToAction("DanhMuc_Admin");
        }

        // 4. Xóa loại
        [HttpPost]
        public ActionResult Delete_Cate(int id)
        {
            var category = db.Categories.Find(id);
            if (category != null)
            {
                if (db.Products.Any(p => p.CategoryID == id))
                {
                    TempData["Error"] = "Không thể xóa loại này vì đang có sản phẩm thuộc danh mục này!";
                }
                else
                {
                    db.Categories.Remove(category);
                    db.SaveChanges();
                }
            }
            return RedirectToAction("DanhMuc_Admin");
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
            ViewBag.SPLQ = db.Products.Where(x => x.ProductID != id && x.CategoryID == product.CategoryID).ToList();
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
        public ActionResult Edit([Bind(Include = "ProductID,ProductName,CategoryID,Price,Discount,Description,ImageURL,Status,CreatedAt,AvgRating,RatingCount,SoLuongTon,SoLuongBan")] Product product,HttpPostedFileBase ImgFile)
        {
            if(ImgFile!=null&& ImgFile.ContentLength > 0)
            {
                string filename = Path.GetFileName(ImgFile.FileName);
                string path = Path.Combine(Server.MapPath("~/img/product"), filename);
                ImgFile.SaveAs(path);
                product.ImageURL = filename;

            }
            var sp = db.Products.Find(product.ProductID);
            if (sp == null) return HttpNotFound();
            sp.ProductName = product.ProductName;
            sp.CategoryID = product.CategoryID;
            sp.Price = product.Price;
            sp.Discount = product.Discount;
            sp.Description = product.Description;
            sp.Status = product.Status;
            sp.SoLuongTon = product.SoLuongTon;
            sp.SoLuongBan = product.SoLuongBan;
            if (ModelState.IsValid)
            {
                
                db.SaveChanges();
                return RedirectToAction("SanPham_Admin");
            }
            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", sp.CategoryID);
            return View(sp);
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
