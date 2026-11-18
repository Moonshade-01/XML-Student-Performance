using System.Xml;
using System.Xml.Linq;
using System.Text;

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

    public static void CheckNamespace(string namespaceUri)
    {
        if (namespaceUri.Length != 0)
            throw new FormatException("Елементи record та їхні поля повинні бути без простору імен.");
    }

    public static string ReadTextField(XmlReader reader)
    {
        CheckNamespace(reader.NamespaceURI);
        if (reader.IsEmptyElement) return "";
        var depth = reader.Depth;
        var text = new StringBuilder();
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth) break;
            if (reader.NodeType == XmlNodeType.Element)
                throw new FormatException("Поля запису повинні містити лише текст, без вкладених тегів.");
            if (reader.NodeType is XmlNodeType.Text or XmlNodeType.CDATA or XmlNodeType.Whitespace or XmlNodeType.SignificantWhitespace)
                text.Append(reader.Value);
        }
        return text.ToString();
    }

    public static string ReadTextField(XmlElement element)
    {
        CheckNamespace(element.NamespaceURI);
        if (element.ChildNodes.OfType<XmlElement>().Any())
            throw new FormatException("Поля запису повинні містити лише текст, без вкладених тегів.");
        return element.InnerText;
    }

    public static string ReadTextField(XElement element)
    {
        CheckNamespace(element.Name.NamespaceName);
        if (element.HasElements)
            throw new FormatException("Поля запису повинні містити лише текст, без вкладених тегів.");
        return element.Value;
    }
}

// Stream XML without creating a DOM/XDocument tree; the lab calls this SAX.
// XmlReader uses a pull model, whereas classic SAX uses push events.
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
            if (reader.NodeType != XmlNodeType.Element || reader.Depth != 1 || reader.LocalName != "record") continue;
            XmlInput.CheckNamespace(reader.NamespaceURI);
            var id = reader.GetAttribute("id") ?? "";
            XmlInput.CheckId(ids, id);
            var fields = new Dictionary<string, string>();
            using (var sub = reader.ReadSubtree())
                while (sub.Read())
                    if (sub.NodeType == XmlNodeType.Element && sub.Depth == 1 && GradeRecord.Fields.Contains(sub.LocalName))
                    {
                        var key = sub.LocalName;
                        fields.Add(key, XmlInput.ReadTextField(sub));
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
        foreach (var node in document.DocumentElement!.ChildNodes.OfType<XmlElement>().Where(n => n.LocalName == "record"))
        {
            XmlInput.CheckNamespace(node.NamespaceURI);
            var id = node.GetAttribute("id");
            XmlInput.CheckId(ids, id);
            var fields = node.ChildNodes.OfType<XmlElement>().Where(x => GradeRecord.Fields.Contains(x.LocalName))
                .ToDictionary(x => x.LocalName, XmlInput.ReadTextField);
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
        return document.Root!.Elements().Where(n => n.Name.LocalName == "record").Select(node =>
        {
            XmlInput.CheckNamespace(node.Name.NamespaceName);
            var id = (string?)node.Attribute("id") ?? "";
            XmlInput.CheckId(ids, id);
            return GradeRecord.From(id, node.Elements().Where(x => GradeRecord.Fields.Contains(x.Name.LocalName))
                .ToDictionary(x => x.Name.LocalName, XmlInput.ReadTextField));
        }).Where(predicate).ToList();
    }
}
