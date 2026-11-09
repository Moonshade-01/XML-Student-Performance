using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;

namespace StudentPerformance.Core;

public sealed class XmlFileStore
{
    // Google Drive for desktop надає синхронізований шлях як звичайний файл.
    public void Save(string path, IEnumerable<GradeRecord> rows)
    {
        var document = new XDocument(new XDeclaration("1.0", "utf-8", null),
            new XElement("performance", rows.Select(r => new XElement("record", new XAttribute("id", r.Id),
                new XElement("FullName", r.FullName), new XElement("Faculty", r.Faculty),
                new XElement("Department", r.Department), new XElement("Group", r.Group),
                new XElement("Discipline", r.Discipline), new XElement("Semester", r.Semester), new XElement("Score", r.Score)))));
        document.Save(path);
    }
}

public sealed class HtmlTransformService
{
    public void Transform(string xmlPath, string xslPath, string htmlPath)
    {
        var transform = new XslCompiledTransform();
        using var stylesheet = XmlInput.Open(xslPath);
        transform.Load(stylesheet, XsltSettings.Default, null);
        using var input = XmlInput.Open(xmlPath);
        using var output = XmlWriter.Create(htmlPath, transform.OutputSettings);
        transform.Transform(input, null, output);
    }
}

// Контекст Strategy: інтерфейс не залежить від конкретного алгоритму аналізу.
public sealed class PerformanceService(QueryBuilder queries, XmlFileStore store, HtmlTransformService html)
{
    public IXmlAnalysisStrategy Strategy { get; set; } = new SaxAnalysisStrategy();
    public IReadOnlyList<GradeRecord> Search(string path, SearchCriteria criteria) => Strategy.Analyze(path, queries.Build(criteria));
    public void Save(string path, IEnumerable<GradeRecord> rows) => store.Save(path, rows);
    public void ExportHtml(string xmlPath, string xslPath, string htmlPath) => html.Transform(xmlPath, xslPath, htmlPath);
}
