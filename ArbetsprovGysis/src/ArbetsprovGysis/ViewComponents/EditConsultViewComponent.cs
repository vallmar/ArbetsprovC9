﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ArbetsprovGysis.Models;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace ArbetsprovGysis.ViewComponents
{
    public class EditConsultViewComponent : ViewComponent
    {
        // GET: /<controller>/
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}
