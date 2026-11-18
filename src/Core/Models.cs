using System.Globalization;

namespace StudentPerformance.Core;

// One record represents one student's result in one discipline.
public sealed record GradeRecord(string Id, string FullName, string Faculty,
    string Department, string Group, string Discipline, int Semester, int Score, string Teacher = "")
{
    public string Grade => Score >= 90 ? "5" : Score >= 75 ? "4" : Score >= 60 ? "3" : "—";

    public static readonly string[] Fields = ["FullName", "Faculty", "Department", "Group", "Discipline", "Semester", "Score", "Teacher"];

    public static GradeRecord From(string id, IReadOnlyDictionary<string, string> values)
    {
        string Required(string key) => values.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v)
            ? v.Trim() : throw new FormatException($"Відсутнє поле {key} у записі {id}.");
        if (string.IsNullOrWhiteSpace(id)) throw new FormatException("Запис не має id.");
        var semester = int.Parse(Required("Semester"), CultureInfo.InvariantCulture);
        var score = int.Parse(Required("Score"), CultureInfo.InvariantCulture);
        if (score is < 0 or > 100 || semester is < 1 or > 12)
            throw new FormatException($"Некоректний бал або семестр у записі {id}.");
        return new(id, Required("FullName"), Required("Faculty"), Required("Department"),
            Required("Group"), Required("Discipline"), semester, score,
            values.TryGetValue("Teacher", out var teacher) ? teacher.Trim() : "");
    }
}

public sealed record SearchCriteria(string Keywords = "", string Faculty = "", string Department = "",
    string Group = "", string Discipline = "", int? Semester = null, int? MinScore = null, int? MaxScore = null);

public sealed class QueryBuilder
{
    public Func<GradeRecord, bool> Build(SearchCriteria c)
    {
        if (c.MinScore is < 0 or > 100 || c.MaxScore is < 0 or > 100 || c.MinScore > c.MaxScore
            || c.Semester is < 1 or > 12) throw new ArgumentException("Перевірте межі балів і семестр.");
        var rules = new List<Func<GradeRecord, bool>>();
        if (c.Semester.HasValue) rules.Add(r => r.Semester == c.Semester);
        if (c.MinScore.HasValue) rules.Add(r => r.Score >= c.MinScore);
        if (c.MaxScore.HasValue) rules.Add(r => r.Score <= c.MaxScore);
        static bool Contains(string text, string value) => text.Contains(value, StringComparison.OrdinalIgnoreCase);
        var faculty = c.Faculty.Trim();
        var department = c.Department.Trim();
        var group = c.Group.Trim();
        var discipline = c.Discipline.Trim();
        if (faculty.Length > 0) rules.Add(r => Contains(r.Faculty, faculty));
        if (department.Length > 0) rules.Add(r => Contains(r.Department, department));
        if (group.Length > 0) rules.Add(r => Contains(r.Group, group));
        if (discipline.Length > 0) rules.Add(r => Contains(r.Discipline, discipline));
        foreach (var word in c.Keywords.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            rules.Add(r => Contains(r.FullName, word) || Contains(r.Faculty, word)
                || Contains(r.Department, word) || Contains(r.Group, word)
                || Contains(r.Discipline, word) || Contains(r.Teacher, word));
        // Build the query from active conditions and combine them with AND.
        return r =>
        {
            foreach (var rule in rules)
                if (!rule(r)) return false;
            return true;
        };
    }
}
