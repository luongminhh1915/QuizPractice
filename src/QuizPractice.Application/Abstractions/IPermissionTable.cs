namespace QuizPractice.Application.Abstractions;

/// <summary>
/// The permission table of the configuration file (F#4, BR-03, D23): which System Menu
/// items each role may open. The Api builds it from appsettings.json and also guards
/// its controllers with it, so the menu and the URL check never disagree.
/// </summary>
public interface IPermissionTable
{
    bool Allows(string role, string menuItem);
}
