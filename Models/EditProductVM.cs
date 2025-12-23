using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DoAn.Models
{
    public class EditProductVM
    {
        public Product Product { get; set; }
        public List<CategoryGroup> CategoryGroup { get; set; }
    }
}