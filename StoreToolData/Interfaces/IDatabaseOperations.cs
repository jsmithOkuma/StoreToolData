using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoreToolData
{
    interface IDatabaseOperations
    {
        void CheckAndCreateTable();
        void InsertTools(List<Tool> tools);
        List<Tool> GetTools();
        bool TestConnection();
        void UpdateTableStructure(ColumnSettings settings);
        List<String> GetTableColumns(string tableName);
    }
}
