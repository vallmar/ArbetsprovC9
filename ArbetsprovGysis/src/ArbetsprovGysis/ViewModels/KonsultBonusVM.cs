using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArbetsprovGysis.ViewModels
{
    public class KonsultBonusVM
    {
        public DateTime ReqruitmentDate { get; set; }
        public String Name { get; set; }
        public int ID { get; set; }
        public double LojayltyPoints { get; set; }

        public KonsultBonusVM()
        {
            int workedYears = ((DateTime.Now - ReqruitmentDate).Days)/365;

            if (workedYears < 6)
            {
                LojayltyPoints = 1 + (workedYears / 10);
            }
            else if(workedYears<1)
            {
                LojayltyPoints = 1;
            }
            else
            {
                LojayltyPoints=1.5;
            }
        }
    }
}
