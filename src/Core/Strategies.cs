using System.Xml;
using System.Xml.Linq;

namespace StudentPerformance.Core;

public interface IXmlAnalysisStrategy
{
    string Name { get; }
    IReadOnlyList<GradeRecord> Analyze(string path, Func<GradeRecord, bool> predicate);
}

public static class XmlInput
{
    public static XmlReader Open(string path) => XmlReader.Create(path, new XmlReaderSettings
    { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, IgnoreComments = true });

    public static void CheckRoot(XmlReader reader)
    {
        reader.MoveToContent();
        if (reader.Name != "performance" || reader.NamespaceURI != "")
            throw new FormatException("Очікується кореневий елемент <performance> без простору імен.");
    }

    public static void CheckId(HashSet<string> ids, string id)
    {
        if (!ids.Add(id)) throw new FormatException($"Повторний id: {id}.");
    }
}

// Потокове читання без DOM/XDocument. У методичці цей підхід названо SAX.
// XmlReader реалізує pull-модель, тоді як класичний SAX використовує події (push).
public sealed class SaxAnalysisStrategy : IXmlAnalysisStrategy
{
    public string Name => "SAX / XmlReader";
    public IReadOnlyList<GradeRecord> Analyze(string path, Func<GradeRecord, bool> predicate)
    {
        using var reader = XmlInput.Open(path);
        XmlInput.CheckRoot(reader);
        var result = new List<GradeRecord>();
        var ids = new HashSet<string>();
        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element || reader.Depth != 1 || reader.Name != "record") continue;
            var id = reader.GetAttribute("id") ?? "";
            XmlInput.CheckId(ids, id);
            var fields = new Dictionary<string, string>();
            using (var sub = reader.ReadSubtree())
                while (sub.Read())
                    if (sub.NodeType == XmlNodeType.Element && sub.Depth == 1 && GradeRecord.Fields.Contains(sub.Name))
                    {
                        var key = sub.Name;
                        fields.Add(key, sub.ReadString());
                    }
            var record = GradeRecord.From(id, fields);
            if (predicate(record)) result.Add(record);
        }
        return result;
    }
}

public sealed class DomAnalysisStrategy : IXmlAnalysisStrategy
{
    public string Name => "DOM / XmlDocument";
    public IReadOnlyList<GradeRecord> Analyze(string path, Func<GradeRecord, bool> predicate)
    {
        using var reader = XmlInput.Open(path);
        XmlInput.CheckRoot(reader);
        var document = new XmlDocument { XmlResolver = null };
        document.Load(reader);
        var result = new List<GradeRecord>();
        var ids = new HashSet<string>();
        foreach (XmlElement node in document.SelectNodes("/performance/record")!)
        {
            var id = node.GetAttribute("id");
            XmlInput.CheckId(ids, id);
            var fields = node.ChildNodes.OfType<XmlElement>().Where(x => GradeRecord.Fields.Contains(x.Name))
                .ToDictionary(x => x.Name, x => x.InnerText);
            var record = GradeRecord.From(id, fields);
            if (predicate(record)) result.Add(record);
        }
        return result;
    }
}

public sealed class LinqAnalysisStrategy : IXmlAnalysisStrategy
{
    public string Name => "LINQ to XML / XDocument";
    public IReadOnlyList<GradeRecord> Analyze(string path, Func<GradeRecord, bool> predicate)
    {
        using var reader = XmlInput.Open(path);
        XmlInput.CheckRoot(reader);
        var document = XDocument.Load(reader);
        var ids = new HashSet<string>();
        return document.Root!.Elements("record").Select(node =>
        {
            var id = (string?)node.Attribute("id") ?? "";
            XmlInput.CheckId(ids, id);
            return GradeRecord.From(id, node.Elements().Where(x => GradeRecord.Fields.Contains(x.Name.ToString()))
                .ToDictionary(x => x.Name.ToString(), x => x.Value));
        }).Where(predicate).ToList();
    }
}
