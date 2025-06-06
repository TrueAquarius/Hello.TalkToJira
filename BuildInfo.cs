namespace TrueAquarius.TalkToJira;

/// <summary>
/// Represents the build information for this application.
/// BuildDate and BuildNumber are automatically updated by the build process.
/// </summary>
public static class BuildInfo
{
    /// <summary>
    /// The date and time when the build was created.
    /// Automatically updated by the build process.
    /// </summary>
    public const string BuildDate = "06.06.2025 16:18 +02:00"; // DO NOT CHANGE THIS MANUALLY! This is automatically updated by the build process.

    /// <summary>
    /// The build number of the application.
    /// Automatically updated by the build process.
    /// </summary>
    public const int BuildNumber = 19; // DO NOT CHANGE THIS MANUALLY! This is automatically updated by the build process.

    /// <summary>
    /// Version number of the application.
    /// Must be updates manually when a new version is released.
    /// </summary>
    public const string Version = "V0.1.0";

    /// <summary>
    /// Combines the version and build number into a single string for display purposes.
    /// </summary>
    public static string All { get { return Version + " (Build " + BuildNumber + ")"; } }
}
