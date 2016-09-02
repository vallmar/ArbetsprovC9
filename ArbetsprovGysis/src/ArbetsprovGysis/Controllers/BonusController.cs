using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ArbetsprovGysis.Models;
using Microsoft.EntityFrameworkCore;
using ArbetsprovGysis.ViewModels;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace ArbetsprovGysis.Controllers
{
    public class BonusController : Controller
    {
        KonsultContext context;
        public BonusController(KonsultContext context)
        {
            this.context = context;
        }
        // GET: /<controller>/
        public IActionResult Index()
        {
            var allConsults = context.Konsult.Select(o => new KonsultBonusVM
            {
                Name = o.Name,
                ReqruitmentDate = o.ReqruitmentDate,
                ID = o.ID
            }).ToArray();
            return View(allConsults);
        }
    }
}
