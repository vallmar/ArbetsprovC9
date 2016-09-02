using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace ArbetsprovGysis.ViewComponents
{
    public class AddConsultViewComponent : ViewComponent
    {
        // GET: /<controller>/
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}
