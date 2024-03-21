using BuildHelper.App.Helpers;
using BuildHelper.App.Models;
using MoonCore.Helpers;
using Spectre.Console;

namespace BuildHelper.App.Services;

public class BuildService
{
    private readonly ShellHelper ShellHelper = new();
    private readonly string RootPath;

    public BuildService(string rootPath)
    {
        RootPath = rootPath;
    }

    public async Task Run(Project project)
    {
        AnsiConsole.Clear();
        
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("cyan bold"))
            .StartAsync($"Building {project.Id}", async ctx =>
            {
                // Base variables
                var cacheDir = PathBuilder.Dir(RootPath, "cache", project.Id);
                var artifactsDir = PathBuilder.Dir(RootPath, "artifacts", project.Id);
                var workingDir = Path.GetFullPath(cacheDir);
                
                // Build cache
                AnsiConsole.WriteLine("Ensuring folders");
                Directory.CreateDirectory(cacheDir);
                Directory.CreateDirectory(artifactsDir);
                
                // Git
                ctx.Status("Installing git");
                
                try
                {
                    await ShellHelper.ExecuteWithOutputHandler($"apt-get install git -y", (line, isError) =>
                    {
                        if (isError)
                            AnsiConsole.Markup("[red]ERR [/]");
                        else
                            AnsiConsole.Markup("[cyan]LOG [/]");

                        AnsiConsole.WriteLine(line);

                        return Task.CompletedTask;
                    }, workingDir);
                }
                catch (Exception e)
                {
                    AnsiConsole.MarkupLine($"[red]An error occured while installing git[/]");
                    AnsiConsole.WriteException(e);
                        
                    return;
                }
                
                // Packages
                ctx.Status("Installing packages");

                int i = 1;
                foreach (var package in project.Packages)
                {
                    ctx.Status($"Installing packages ({i}/{project.Packages.Length})");

                    try
                    {
                        await ShellHelper.ExecuteWithOutputHandler($"apt-get install {package} -y", (line, isError) =>
                        {
                            if (isError)
                                AnsiConsole.Markup("[red]ERR [/]");
                            else
                                AnsiConsole.Markup("[cyan]LOG [/]");

                            AnsiConsole.WriteLine(line);

                            return Task.CompletedTask;
                        }, workingDir);
                    }
                    catch (Exception e)
                    {
                        AnsiConsole.MarkupLine($"[red]An error occured while installing package '{package}'[/]");
                        AnsiConsole.WriteException(e);
                        
                        return;
                    }
                    
                    i++;
                }
                
                // Repo cloning
                if (Directory.Exists(PathBuilder.Dir(cacheDir, ".git")))
                {
                    ctx.Status("Pulling git changes");
                    
                    try
                    {
                        await ShellHelper.ExecuteWithOutputHandler("git pull", (line, isError) =>
                        {
                            if (isError)
                                AnsiConsole.Markup("[red]ERR [/]");
                            else
                                AnsiConsole.Markup("[cyan]LOG [/]");

                            AnsiConsole.WriteLine(line);

                            return Task.CompletedTask;
                        }, workingDir);
                    }
                    catch (Exception e)
                    {
                        AnsiConsole.MarkupLine("[red]An error occured while pulling git changes[/]");
                        AnsiConsole.WriteException(e);
                        
                        return;
                    }
                }
                else
                {
                    ctx.Status("Cloning git repository");
                    
                    try
                    {
                        await ShellHelper.ExecuteWithOutputHandler($"git clone {project.Repository} --branch {project.Branch} .", (line, isError) =>
                        {
                            if (isError)
                                AnsiConsole.Markup("[red]ERR [/]");
                            else
                                AnsiConsole.Markup("[cyan]LOG [/]");

                            AnsiConsole.WriteLine(line);

                            return Task.CompletedTask;
                        }, workingDir);
                    }
                    catch (Exception e)
                    {
                        AnsiConsole.MarkupLine("[red]An error occured while cloning git repository[/]");
                        AnsiConsole.WriteException(e);
                        
                        return;
                    }
                }
                
                // Execute build steps
                i = 1;
                foreach (var step in project.Steps)
                {
                    ctx.Status($"Running step '{step.Name}' ({i}/{project.Steps.Length})");

                    try
                    {
                        foreach (var command in step.Commands)
                        {
                            await ShellHelper.ExecuteWithOutputHandler(command, (line, isError) =>
                            {
                                if (isError)
                                    AnsiConsole.Markup("[red]ERR [/]");
                                else
                                    AnsiConsole.Markup("[cyan]LOG [/]");

                                AnsiConsole.WriteLine(line);

                                return Task.CompletedTask;
                            }, workingDir, ignoreErrors: step.IgnoreErrors);
                        }
                    }
                    catch (Exception e)
                    {
                        AnsiConsole.MarkupLine($"[red]An error occured while running step '{step.Name}'[/]");
                        AnsiConsole.WriteException(e);
                        
                        return;
                    }
                    
                    i++;
                }
                
                // Copy artifacts

                i = 0;
                foreach (var artifact in project.Artifacts)
                {
                    ctx.Status($"Collecting artifact '{artifact.Name}' ({i}/{project.Artifacts.Length})");

                    try
                    {
                        var artifactPath = PathBuilder.File(cacheDir, artifact.Path);

                        if (!File.Exists(artifactPath))
                        {
                            AnsiConsole.MarkupLine($"[red]The artifact '{artifact.Name}' could not be collected. File not found '{artifactPath}'[/]");
                            return;
                        }
                        
                        File.Copy(artifactPath, PathBuilder.File(artifactsDir, artifact.Name));
                    }
                    catch (Exception e)
                    {
                        AnsiConsole.MarkupLine($"[red]An error occured while collecting artifact '{artifact.Name}'[/]");
                        AnsiConsole.WriteException(e);
                        
                        return;
                    }
                    
                    i++;
                }
                
                AnsiConsole.MarkupLine($"[green]Successfully built project '{project.Id}'[/]");
            });
    }
}