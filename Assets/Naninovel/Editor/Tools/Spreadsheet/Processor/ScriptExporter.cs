// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;
using System.IO;
using Naninovel.ManagedText;
using static Naninovel.Spreadsheet.Constants;

namespace Naninovel.Spreadsheet
{
    public class ScriptExporter
    {
        protected virtual string ScriptFolder { get; }
        protected virtual string L10nFolder { get; }
        protected virtual string OutputFolder { get; }
        protected virtual string SourceLocale { get; }
        protected virtual bool Annotate { get; }
        protected virtual MultilineManagedTextParser TextParser { get; } = new MultilineManagedTextParser();
        protected virtual List<SheetColumn> Columns { get; } = new List<SheetColumn>();
        protected virtual Dictionary<string, int> IdToIndex { get; } = new Dictionary<string, int>();
        protected virtual List<string> Cells { get; } = new List<string>();
        protected virtual SheetWriter SheetWriter { get; } = new SheetWriter();

        public ScriptExporter (string scriptFolder, string l10nFolder,
            string outputFolder, string sourceLocale, bool annotate)
        {
            ScriptFolder = scriptFolder;
            L10nFolder = l10nFolder;
            OutputFolder = outputFolder;
            SourceLocale = sourceLocale;
            Annotate = annotate;
        }

        public virtual void Export (string scriptPath)
        {
            Reset();
            var script = LoadScript(scriptPath);
            var sheet = BuildSheet(script);
            WriteSheet(sheet, scriptPath);
        }

        protected virtual void Reset ()
        {
            Columns.Clear();
            IdToIndex.Clear();
            Cells.Clear();
        }

        protected virtual Script LoadScript (string scriptPath)
        {
            var name = Path.GetFileNameWithoutExtension(scriptPath);
            var text = File.ReadAllText(scriptPath);
            return Script.FromText(name, text, scriptPath);
        }

        protected virtual Sheet BuildSheet (Script script)
        {
            AppendIdColumnAndIndexIds(script.TextMap);
            if (Annotate) AppendAnnotationsColumn(script);
            AppendSourceColumn(script.TextMap);
            foreach (var localeFolder in Directory.EnumerateDirectories(L10nFolder))
                AppendLocaleColumn(PathUtils.FormatPath(localeFolder), script.Name);
            return new Sheet(Columns.ToArray());
        }

        protected virtual void WriteSheet (Sheet sheet, string scriptPath)
        {
            var localPath = scriptPath.GetBetween(ScriptFolder + '/', ScriptExtension) + CsvExtension;
            var path = PathUtils.Combine(OutputFolder, ScriptFolderName, localPath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            SheetWriter.Write(sheet, path);
        }

        protected virtual void AppendIdColumnAndIndexIds (ScriptTextMap map)
        {
            Cells.Clear();
            Cells.Add(IdColumnHeader);
            foreach (var id in map.Map.Keys)
            {
                IdToIndex[id] = Cells.Count;
                Cells.Add(id);
            }
            Columns.Add(new SheetColumn(Cells.ToArray()));
        }

        protected virtual void AppendAnnotationsColumn (Script script)
        {
            Cells.Clear();
            var annotations = LocalizableTextAnnotations.FromScript(script);
            Cells.Add(AnnotationColumnHeader);
            foreach (var key in script.TextMap.Map.Keys)
                AssignCell(key, annotations.TryGet(key, out var annotation) ? annotation : "");
            Columns.Add(new SheetColumn(Cells.ToArray()));
        }

        protected virtual void AppendSourceColumn (ScriptTextMap map)
        {
            Cells.Clear();
            Cells.Add(SourceLocale);
            foreach (var kv in map.Map)
                AssignCell(kv.Key, kv.Value);
            Columns.Add(new SheetColumn(Cells.ToArray()));
        }

        protected virtual void AppendLocaleColumn (string localeFolder, string scriptName)
        {
            if (!TryLoadL10nDocument(localeFolder, scriptName, out var doc)) return;
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

        protected virtual bool TryLoadL10nDocument (string localeFolder, string scriptName, out ManagedTextDocument doc)
        {
            var path = PathUtils.Combine(localeFolder, TextFolderName, ScriptFolderName, scriptName + TextExtension);
            if (!File.Exists(path))
            {
                Engine.Warn($"Failed to load {path}: make sure localization is generated.");
                doc = null;
                return false;
            }
            doc = TextParser.Parse(File.ReadAllText(path));
            return true;
        }
    }
}
