namespace BuildHelper.App.Models;

public class ProjectStep
{
    public string Name { get; set; }
    public string[] Commands { get; set; }
    public bool IgnoreErrors { get; set; }
}