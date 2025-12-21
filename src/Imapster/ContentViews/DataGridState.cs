using System;
using System.Collections.Generic;

namespace Imapster.ContentViews
{
    public class DataGridState
    {
        public List<string> ColumnOrder { get; set; } = new();
        public Dictionary<string, bool> ColumnVisibility { get; set; } = new();
        public string? SortColumn { get; set; }
        public bool SortAscending { get; set; } = true;
        public Dictionary<string, List<object?>> Filters { get; set; } = new();
    }
}