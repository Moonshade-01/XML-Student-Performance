# Student-Performance

**OOP — Laboratory Work 2, Variant 6: Student Performance.**

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

Open `Student-Performance.csproj` in Visual Studio with .NET 10 / MAUI support and select the Windows target, or run these commands from the project directory:

```powershell
dotnet restore Student-Performance.csproj
dotnet build Student-Performance.csproj -t:Run
```

The sample XML loads automatically. The application reads its default XML and XSL files from the `Examples` directory beside the built application. Both files are copied there automatically during build and publish. Keep this directory when distributing the application.

## Project structure

```text
Student-Performance/
├── Student-Performance.csproj      # Single MAUI project
├── README.md
├── global.json            # .NET 10 SDK selection
├── .gitignore
├── Examples/
│   ├── Student-Performance.xml    # Sample student records
│   └── Student-Performance.xsl    # HTML transformation stylesheet
├── src/
│   ├── App.xaml[.cs]     # Application and dark theme
│   ├── MauiProgram.cs    # Startup and dependency injection
│   ├── MainPage.xaml[.cs]
│   ├── PerformanceViewModel.cs
│   ├── AsyncCommand.cs
│   ├── IDocumentPicker.cs
│   └── Core/
│       ├── Models.cs
│       ├── Strategies.cs
│       └── Services.cs
└── Platforms/
    └── Windows/          # Required Windows startup and file dialogs
```

The export contains the application source and its examples. Reports, verification logs, separate test projects and generated build files are kept outside this runtime project. Standard framework filenames such as `App.xaml`, `app.manifest`, `README.md` and `global.json` retain their conventional names. The project, application, executable and sample files use the **Student-Performance** name. C# namespaces use `StudentPerformance` because a hyphen cannot be used in a namespace.

## Features

- Three interchangeable XML analysis strategies: streaming `XmlReader`, DOM `XmlDocument`, and LINQ to XML `XDocument`.
- The Strategy pattern for switching the XML analysis algorithm.
- Dynamic combinations of keyword, faculty, department, group, discipline, semester and score filters.
- XML export of the current selection.
- XSLT-based HTML export using `Examples/Student-Performance.xsl`.
- A dark MAUI interface with MVVM, record count and the average score of the current selection.

The streaming implementation is labelled SAX to follow the supplied laboratory materials. Technically, `XmlReader` uses a pull model, while classic SAX uses a push/event model.

## Usage

The interface is in Ukrainian.

1. Use **Відкрити XML** to open another XML file with the sample structure.
2. Select an analysis strategy and enter any combination of filters.
3. Click **Знайти** to apply the query. Empty fields do not restrict the results. All active conditions are combined with AND. Text matching ignores case; each keyword must occur in a text field of the record.
4. Use **Скинути фільтри** to display all records again. The filter panel can be scrolled on smaller windows.
5. Use **Зберегти XML** to save the current selection, or **Експорт HTML** to transform it using the supplied stylesheet.

The sample contains 12 fictional grade records for six students. Each record describes one student's result in one discipline. Scores must be between 0 and 100; semesters must be between 1 and 12. Record IDs must be unique. The sample ECTS mapping is A: 90–100, B: 82–89, C: 75–81, D: 64–74, E: 60–63, FX: 35–59, F: 0–34.

## Google Drive

Open or save XML files in a folder mounted or synchronized by **Google Drive for desktop**. The desktop client must be installed and signed in. The application uses normal file access; it does not integrate with Google Drive API or OAuth. Saving a file confirms the local write. Check cloud synchronization in your own Google Drive client.
