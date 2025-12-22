using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
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
