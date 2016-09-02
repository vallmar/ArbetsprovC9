using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ArbetsprovC9.Models
{
    public class StrangeModel
    {
        [StringLength(3000, MinimumLength =65, ErrorMessage ="Bocka i minst 3 och max 20 låtar")]
        public string TrackIdString { get; set; }
    }
}
