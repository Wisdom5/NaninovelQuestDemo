// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Naninovel.Csv;

namespace Naninovel.Spreadsheet
{
    public class SheetReader
    {
        protected virtual List<SheetColumn> Columns { get; } = new List<SheetColumn>();
        protected virtual List<string[]> Rows { get; } = new List<string[]>();
        protected virtual List<string> Cells { get; } = new List<string>();
        protected virtual int ColumnCount { get; private set; }

        public virtual Sheet Read (string path)
        {
            Reset();
            using (var file = new FileStream(path, FileMode.Open))
            using (var stream = new StreamReader(file))
                return Read(new CsvReader(stream));
        }

        protected virtual void Reset ()
        {
            Columns.Clear();
            Rows.Clear();
            ColumnCount = -1;
        }

        protected virtual Sheet Read (CsvReader csv)
        {
            while (csv.Read()) ReadRow(csv);
            return new Sheet(CreateColumns());
        }

        protected virtual void ReadRow (CsvReader csv)
        {
            Cells.Clear();
            if (ColumnCount < 0) ColumnCount = csv.FieldsCount;
            for (int i = 0; i < csv.FieldsCount; i++)
                Cells.Add(csv[i]);
            Rows.Add(Cells.ToArray());
        }

        protected virtual SheetColumn[] CreateColumns ()
        {
            for (int rowIdx = 0; rowIdx < ColumnCount; rowIdx++)
            {
                Cells.Clear();
                foreach (var cells in Rows)
                    Cells.Add(cells.ElementAtOrDefault(rowIdx) ?? "");
                Columns.Add(new SheetColumn(Cells.ToArray()));
            }
            return Columns.ToArray();
        }
    }
}
