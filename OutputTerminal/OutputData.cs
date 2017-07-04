using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

namespace StalkerProject.OutputTerminal
{
    class OutputData
    {
        public ObjectId Id { get; set; }
        public string RelatedAddress { get; set; }
        public string Summary { get; set; }
        public string Content { get; set; }
        public string RelatedVar { get; set; }
        //public DateTime OutputTime { get; set; }
    }
}
