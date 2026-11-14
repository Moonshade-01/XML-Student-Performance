# Student-Performance

**Object-Oriented Programming — Laboratory Work 2, Variant 6: Student Performance.**

A C# / .NET MAUI desktop application for Windows with a dark interface. It searches student performance records in XML and exports the selected records to XML or HTML using an XSL stylesheet.

## Requirements

- Windows 10 version 1809 or later, or Windows 11 (x64).
- .NET 10 SDK with the `maui-windows` workload.
- Windows App SDK Runtime, provided by the MAUI development setup.
- Access to NuGet for the initial package restore, unless the packages are already cached.

Install the workload if needed:

```powershell
dotnet workload install maui-windows
```

Alternatively, install the .NET MAUI development workload through Visual Studio Installer.

## Run

Open `Student-Performance.sln` in Visual Studio with .NET 10 / MAUI support and select the Windows target, or run these commands from the project directory:

```powershell
dotnet restore Student-Performance.csproj
dotnet build Student-Performance.csproj -t:Run
```

The application starts with an empty result list. Click **Відкрити XML** to load your own XML document. An extended fictional dataset is available in `Examples/Student-Performance.xml`; open it manually to explore the application. The HTML stylesheet is embedded in the application.

## Project structure

- `Student-Performance.csproj`: the MAUI project.
- `Examples/Student-Performance.xml`: 96 grade records, 32 students, 8 groups, 3 faculties, 4 departments, 12 disciplines and 12 teachers. All personal data is fictional.
- `src/Core/`: models, query construction, XML strategies and services.
- `src/Platforms/Windows/`: Windows startup, manifest and file dialogs.
- `src/Resources/Student-Performance.xsl`: embedded HTML transformation stylesheet.
- `src/`: XAML interface, dark theme, ViewModel and commands.

## XML format

Use a `performance` root without a namespace and direct `record` children with unique `id` attributes. Each record requires nonempty `FullName`, `Faculty`, `Department`, `Group`, `Discipline`, `Semester` and `Score` elements. One record describes one student's result in one discipline. Scores must be 0–100 and semesters 1–12. The optional `Teacher` element specifies the teacher; older files without it remain supported. Teacher names are included in keyword search, displayed in the results, and preserved in XML and HTML exports.

## Features

- Three interchangeable XML analysis strategies: streaming `XmlReader`, DOM `XmlDocument`, and LINQ to XML `XDocument`.
- The Strategy pattern for switching the XML analysis algorithm.
- Dynamic combinations of keyword, faculty, department, group, discipline, semester and score filters.
- XML export of the current selection.
- XSLT-based HTML export using the embedded XSL stylesheet.
- A dark MAUI interface with MVVM, record count and the average score of the current selection.

The streaming implementation is labelled SAX to follow the supplied laboratory materials. Technically, `XmlReader` uses a pull model, while classic SAX uses a push/event model.

## Usage

The interface is in Ukrainian.

1. Use **Відкрити XML** to open `Examples/Student-Performance.xml` or your own XML file using the format above.
2. Select an analysis strategy and enter any combination of filters.
3. Click **Знайти** to apply the query. Empty fields do not restrict the results. All active conditions are combined with AND. Text matching ignores case; each keyword must occur in a text field of the record.
4. Use **Скинути фільтри** to display all records again. The filter panel can be scrolled on smaller windows.
5. Use **Зберегти XML** to save the current selection, or **Експорт HTML** to transform it using the supplied stylesheet.

The ECTS mapping is A: 90–100, B: 82–89, C: 75–81, D: 64–74, E: 60–63, FX: 35–59, F: 0–34.

## Google Drive

Open or save XML files in a folder mounted or synchronized by **Google Drive for desktop**. The desktop client must be installed and signed in. The application uses normal file access; it does not integrate with Google Drive API or OAuth. Saving a file confirms the local write. Check cloud synchronization in your own Google Drive client.
