namespace BuildHelper.App.Models;

public class Project
{
    public string Id { get; set; }
    public string Repository { get; set; }
    public string Branch { get; set; } = "main";
    public string[] Packages { get; set; }
    public ProjectArtifact[] Artifacts { get; set; }
    public ProjectStep[] Steps { get; set; }
}