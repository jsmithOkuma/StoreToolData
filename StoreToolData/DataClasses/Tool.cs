using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoreToolData
{
    class Tool
    {
        public int ToolNumber { get; set; }
        public int PotNum { get; set; }
        public int ToolLife { get; set; }
        public int ToolLifeRem { get; set; }
        public double ToolLengthOffset { get; set; }
        public string ToolIsAttached { get; set; }
        public string ToolName { get; set; }

        public Tool(int toolNum, int potNum, int toolLife, int toolLifeRem, double toolLengthOffset, string toolIsAttached, string toolName)
        {
            this.ToolNumber = toolNum;
            this.PotNum = potNum;
            this.ToolLife = toolLife;
            this.ToolLifeRem = toolLifeRem;
            this.ToolLengthOffset = toolLengthOffset;
            this.ToolIsAttached = toolIsAttached;
            this.ToolName = toolName;
        }

        public Tool()
        {
        }
    }
}
