// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;
using System.IO;
using Naninovel.ManagedText;
using static Naninovel.Spreadsheet.Constants;

namespace Naninovel.Spreadsheet
{
    public class TextExporter
    {
        protected virtual string L10nFolder { get; }
        protected virtual string OutputFolder { get; }
        protected virtual string SourceLocale { get; }
        protected virtual bool Annotate { get; }
        protected virtual List<SheetColumn> Columns { get; } = new List<SheetColumn>();
        protected virtual Dictionary<string, int> IdToIndex { get; } = new Dictionary<string, int>();
        protected virtual List<string> Cells { get; } = new List<string>();
        protected virtual SheetWriter SheetWriter { get; } = new SheetWriter();

        public TextExporter (string l10nFolder, string outputFolder, string sourceLocale, bool annotate)
        {
            L10nFolder = l10nFolder;
            OutputFolder = outputFolder;
            SourceLocale = sourceLocale;
            Annotate = annotate;
        }

        public virtual void Export (string docPath)
        {
            Reset();
            var category = Path.GetFileNameWithoutExtension(docPath);
            var doc = ManagedTextUtils.Parse(File.ReadAllText(docPath), category);
            var sheet = BuildSheet(doc, category);
            WriteSheet(sheet, category);
        }

        protected virtual void Reset ()
        {
            Columns.Clear();
            IdToIndex.Clear();
            Cells.Clear();
        }

        protected virtual Sheet BuildSheet (ManagedTextDocument doc, string category)
        {
            AppendIdColumnAndIndexIds(doc);
            if (Annotate) AppendAnnotationsColumn(doc);
            AppendSourceColumn(doc);
            foreach (var localeFolder in Directory.EnumerateDirectories(L10nFolder))
                AppendLocaleColumn(PathUtils.FormatPath(localeFolder), category);
            return new Sheet(Columns.ToArray());
        }

        protected virtual void WriteSheet (Sheet sheet, string category)
        {
            var path = PathUtils.Combine(OutputFolder, TextFolderName, category + CsvExtension);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            SheetWriter.Write(sheet, path);
        }

        protected virtual void AppendIdColumnAndIndexIds (ManagedTextDocument doc)
        {
            Cells.Clear();
            Cells.Add(IdColumnHeader);
            foreach (var record in doc.Records)
            {
                IdToIndex[record.Key] = Cells.Count;
                Cells.Add(record.Key);
            }
            Columns.Add(new SheetColumn(Cells.ToArray()));
        }

        protected virtual void AppendAnnotationsColumn (ManagedTextDocument doc)
        {
            Cells.Clear();
            Cells.Add(AnnotationColumnHeader);
            foreach (var record in doc.Records)
                AssignCell(record.Key, record.Comment ?? "");
            Columns.Add(new SheetColumn(Cells.ToArray()));
        }

        protected virtual void AppendSourceColumn (ManagedTextDocument doc)
        {
            Cells.Clear();
            Cells.Add(SourceLocale);
            foreach (var record in doc.Records)
                AssignCell(record.Key, record.Value);
            Columns.Add(new SheetColumn(Cells.ToArray()));
        }

        protected virtual void AppendLocaleColumn (string localeFolder, string category)
        {
            if (!TryLoadL10nDocument(localeFolder, category, out var doc)) return;
            Cells.Clear();
            Cells.Add(localeFolder.GetAfter("/"));
            foreach (var record in doc.Records)
                AssignCell(record.Key, record.Value);
            Columns.Add(new SheetColumn(Cells.ToArray()));
        }

        protected virtual void AssignCell (string id, string value)
        {
            if (IdToIndex.TryGetValue(id, out var index))
                Cells.Insert(index, value);
            else Engine.Warn($"Failed to assign {value} to {id} while exporting script.");
        }

        protected virtual bool TryLoadL10nDocument (string localeFolder, string category, out ManagedTextDocument doc)
        {
            var path = PathUtils.Combine(localeFolder, TextFolderName, category + TextExtension);
            if (!File.Exists(path))
            {
                Engine.Warn($"Failed to load {path}: make sure localization is generated.");
                doc = null;
                return false;
            }
            doc = ManagedTextUtils.Parse(File.ReadAllText(path), category);
            return true;
        }
    }
}
