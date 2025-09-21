// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Naninovel.ManagedText;
using static Naninovel.Spreadsheet.Constants;

namespace Naninovel.Spreadsheet
{
    public class SheetImporter
    {
        protected virtual string L10nFolder { get; }
        protected virtual string SourceLocale { get; }
        protected virtual SheetReader SheetReader { get; } = new SheetReader();
        protected virtual string CsvPath { get; private set; }
        protected virtual Sheet Sheet { get; private set; }
        protected virtual string Category { get; private set; }
        protected virtual Dictionary<int, string> IndexToId { get; } = new Dictionary<int, string>();
        protected virtual List<ManagedTextRecord> Records { get; } = new List<ManagedTextRecord>();

        public SheetImporter (string l10nFolder, string sourceLocale)
        {
            L10nFolder = l10nFolder;
            SourceLocale = sourceLocale;
        }

        public virtual void Import (string csvPath, string category)
        {
            Reset(csvPath, category);
            if (!TryGetIdColumn(out var idColumn)) return;
            IndexIds(idColumn);
            foreach (var column in Sheet.Columns)
                if (!IsIdColumn(column) && !IsSourceColumn(column) && !IsAnnotationColumn(column))
                    ImportLocaleColumn(column);
        }

        protected virtual void Reset (string csvPath, string category)
        {
            CsvPath = csvPath;
            Sheet = SheetReader.Read(csvPath);
            Category = category;
            IndexToId.Clear();
        }

        protected virtual bool TryGetIdColumn (out SheetColumn column)
        {
            column = Sheet.Columns.FirstOrDefault(IsIdColumn);
            if (column.Cells is null) Engine.Warn($"Failed to import {CsvPath}: sheet is missing ID column.");
            return column.Cells != null;
        }

        protected virtual void IndexIds (SheetColumn idColumn)
        {
            for (int i = 1; i < idColumn.Cells.Count; i++)
                IndexToId[i] = idColumn.Cells[i];
        }

        protected virtual bool IsIdColumn (SheetColumn column)
        {
            return column.Header == IdColumnHeader;
        }

        protected virtual bool IsAnnotationColumn (SheetColumn column)
        {
            return column.Header == AnnotationColumnHeader;
        }

        protected virtual bool IsSourceColumn (SheetColumn column)
        {
            return column.Header == SourceLocale;
        }

        protected virtual void ImportLocaleColumn (SheetColumn column)
        {
            var locale = column.Header;
            if (string.IsNullOrWhiteSpace(locale)) return;
            if (!TryLoadL10nDocument(locale, out var doc, out var path)) return;
            Records.Clear();
            Records.AddRange(doc.Records);
            for (int i = 1; i < column.Cells.Count; i++)
            {
                IndexToId.TryGetValue(i, out var id);
                var index = Records.FindIndex(r => r.Key == id);
                if (index >= 0) Records[index] = new ManagedTextRecord(id, column.Cells[i], Records[index].Comment);
                else Engine.Warn($"Failed to import '{id}' cell at '{column}' of {CsvPath}: {path} localization document is missing the key.");
            }
            File.WriteAllText(path, ManagedTextUtils.Serialize(new ManagedTextDocument(Records, doc.Header), Category));
        }

        protected virtual bool TryLoadL10nDocument (string locale, out ManagedTextDocument doc, out string path)
        {
            path = PathUtils.Combine(L10nFolder, locale, TextFolderName, Category + TextExtension);
            doc = File.Exists(path) ? ManagedTextUtils.Parse(File.ReadAllText(path), Category) : null;
            if (doc is null) Engine.Warn($"Failed to import '{locale}' column of {CsvPath}: missing localization document at {path}.");
            return doc != null;
        }
    }
}
