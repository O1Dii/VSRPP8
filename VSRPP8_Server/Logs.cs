using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSRPP8
{
    class Logs
    {
        public Logs(string name)
        {
            this.Name = name;
        }
        public string Name { get; set; }
    }
}
