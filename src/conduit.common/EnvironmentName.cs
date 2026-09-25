namespace conduit.common;

/// <summary>
/// Defines the environment names supported by the Conduit system.
/// </summary>
public enum EnvironmentName
{
    /// <summary>
    /// Unknown environment.
    /// </summary>
    Unknown,
    
    /// <summary>
    /// Local development environment.
    /// </summary>
    Local,
    
    /// <summary>
    /// Development environment.
    /// </summary>
    Development,
    
    /// <summary>
    /// Testing environment.
    /// </summary>
    Testing,
    
    /// <summary>
    /// Staging environment.
    /// </summary>
    Staging,
    
    /// <summary>
    /// Production environment.
    /// </summary>
    Production
}