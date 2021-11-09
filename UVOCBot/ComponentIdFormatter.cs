namespace UVOCBot;

public enum ComponentAction
{
    /// <summary>
    /// This button sets alternative roles for the welcome message feature
    /// </summary>
    WelcomeMessageSetAlternate = 0,

    /// <summary>
    /// This button sets a users nickname, based on a guess made on recent in-game outfit joins.
    /// </summary>
    WelcomeMessageNicknameGuess = 1,

    /// <summary>
    /// This button tells the user to set their own nickname, as none of the provided guesses matched.
    /// </summary>
    WelcomeMessageNicknameNoMatch = 2,

    /// <summary>
    /// This originates from a select menu and stores information about roles that should be toggled.
    /// </summary>
    RoleMenuToggleRole = 4,

    /// <summary>
    /// This originates from a button and contains information about a role removal confirmation.
    /// </summary>
    RoleMenuConfirmRemoveRole = 5
}

public static class ComponentIdFormatter
{
    public const char SEPARATOR = ':';

    public static string GetId(ComponentAction action, string payload) => $"{(int)action}{SEPARATOR}{payload}";

    public static void Parse(string id, out ComponentAction action, out string payload)
    {
        string[] parts = id.Split(SEPARATOR);
        action = (ComponentAction)int.Parse(parts[0]);
        payload = parts[1];
    }
}
