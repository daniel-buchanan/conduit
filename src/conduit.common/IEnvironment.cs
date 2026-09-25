﻿using System.Collections;

namespace conduit.common;

/// <summary>
/// Defines the contract for accessing environment-related information and variables.
/// </summary>
public interface IEnvironment
{
    /// <summary>
    /// Gets the current logging level from the environment.
    /// </summary>
    LoggingLevel LogLevel { get; }
    
    /// <summary>
    /// Gets the current environment name (e.g., Development, Production).
    /// </summary>
    EnvironmentName Environment { get; }
    
    /// <summary>
    /// Retrieves the value of the specified environment variable.
    /// </summary>
    /// <param name="variable">The name of the environment variable.</param>
    /// <returns>The value of the environment variable, or null if it does not exist.</returns>
    string? GetEnvironmentVariable(string variable);
    
    /// <summary>
    /// Retrieves all current environment variables as a dictionary.
    /// </summary>
    /// <returns>A dictionary containing all environment variable names and values.</returns>
    IDictionary<string, string> GetEnvironmentVariables();
}