using BuildHelper.App.Models;
using BuildHelper.App.Services;
using MoonCore.Helpers;
using Newtonsoft.Json;
using Spectre.Console;

var rootPath = PathBuilder.Dir("storage");
Project project;

var argsAsList = args.ToList();

if (argsAsList.Count == 0)
{
    AnsiConsole.WriteLine("No startup flags provided. Starting in interactive mode");
    
    var projects = new List<Project>();

    foreach (var projectFile in Directory.GetFiles(PathBuilder.Dir("storage", "projects")))
    {
        var projectText = await File.ReadAllTextAsync(projectFile);
        var parsedProject = JsonConvert.DeserializeObject<Project>(projectText);

        projects.Add(parsedProject!);
    }

    if (!projects.Any())
    {
        AnsiConsole.MarkupLine("[red]No projects found. Please place your project configs into the 'projects' folder[/]");
        return;
    }

    var projectId = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .AddChoices(projects.Select(x => x.Id))
            .Title("Select the project you want to build")
    );

    project = projects.First(x => x.Id == projectId);
}
else
{
    if (!argsAsList.Contains("--project"))
    {
        AnsiConsole.MarkupLine("[red]No project specified. Specify a project with --project path/to/json[/]");
        return;
    }

    var projectIndex = argsAsList.IndexOf("--project");
    var projectPath = argsAsList[projectIndex + 1];

    project = JsonConvert.DeserializeObject<Project>(await File.ReadAllTextAsync(projectPath))!;

    if (argsAsList.Contains("--root-path"))
    {
        var rootPathIndex = argsAsList.IndexOf("--root-path");
        rootPath = argsAsList[rootPathIndex + 1];
    }
}

// Setup base directories
Directory.CreateDirectory(PathBuilder.Dir(rootPath, "projects"));
Directory.CreateDirectory(PathBuilder.Dir(rootPath, "cache"));
Directory.CreateDirectory(PathBuilder.Dir(rootPath, "artifacts"));

AnsiConsole.MarkupLine("[cyan]Build Helper[/]");
AnsiConsole.WriteLine("Created by MasuOwO");
AnsiConsole.WriteLine();

var buildService = new BuildService(rootPath);

await buildService.Run(project);