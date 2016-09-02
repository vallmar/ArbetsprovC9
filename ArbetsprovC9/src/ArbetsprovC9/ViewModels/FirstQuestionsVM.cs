using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ArbetsprovC9.ViewModels
{
    public class FirstQuestionsVM
    {
        [Display(Name ="Fyll i din favoritartist")]
        public string FavoriteArtist { get; set; }
    }
}
