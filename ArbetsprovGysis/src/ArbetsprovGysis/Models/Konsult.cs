using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ArbetsprovGysis.Models
{
    public class Konsult
    {
        public DateTime ReqruitmentDate { get; set; }
        public String Name { get; set; }
        [Key]
        public int ID { get; set; }
    }
}
