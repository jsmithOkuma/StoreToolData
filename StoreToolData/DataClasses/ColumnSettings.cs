using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoreToolData
{
    [Serializable]
    public class ColumnSettings
    {
        public bool ToolNumberVisible { get; set; }
        public bool PotNumVisible { get; set; }
        public bool ToolLifeVisible { get; set; }
        public bool ToolLifeRemainingVisible { get; set; }
        public bool ToolLengthOffsetVisible { get; set; }
        public bool ToolIsAttachedVisible { get; set; }
        public bool ToolNameVisible { get; set; }
    }
}
