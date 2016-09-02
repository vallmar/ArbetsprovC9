using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ArbetsprovGysis.Models;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace ArbetsprovGysis.Controllers
{
    public class ConsultController : Controller
    {
        // GET: /<controller>/

        KonsultContext context;
        public ConsultController(KonsultContext context)
        {
            this.context = context;
        }

        public IActionResult Index()
        {
            context.Database.EnsureCreatedAsync();
            var allConsults = context.Konsult.ToList();
            context.Dispose();
            return View(allConsults);
        }
        [HttpPost]
        public IActionResult AddConsult(Konsult consultToAdd)
        {
            context.Konsult.Add(consultToAdd);
            context.SaveChanges();
            return RedirectToAction("index");
        }
        public IActionResult RemoveConsult(int consultIdToRemove)
        {
            var consult = context.Konsult.Where(o => o.ID == consultIdToRemove).SingleOrDefault();
            context.Konsult.Remove(consult);
            context.SaveChanges();
            return RedirectToAction("index");
        }
        [HttpPost]
        public IActionResult EditConsult(Konsult consultToEdit)
        {
            var consultToChange = context.Konsult.Where(o => o.ID == consultToEdit.ID).SingleOrDefault();
            consultToChange.Name = consultToEdit.Name;
            consultToChange.ReqruitmentDate = consultToEdit.ReqruitmentDate;

            context.SaveChanges();
           
            return RedirectToAction("index");
        }

    }
}
