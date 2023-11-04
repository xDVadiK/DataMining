using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataMining
{
    internal class TitanicDataInput
    {
        public int PassengerId { get; set; }
        public int Pclass { get; set; }
        public int Sex { get; set; }
        public float? Age { get; set; }
        public float? Fare { get; set; }
    }
}
