namespace conduit.validation;

public interface IShouldBeBuilder<TRequest, in TProperty> where TRequest : class
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    IRuleBuilder<TRequest> Null(string? message = null);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    IRuleBuilder<TRequest> NullOrWhitespace(string? message = null);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    IRuleBuilder<TRequest> EqualTo(TProperty value, string? message = null);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    IRuleBuilder<TRequest> In(IEnumerable<TProperty> values);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="values"></param>
    /// <returns></returns>
    IRuleBuilder<TRequest> In(string message, IEnumerable<TProperty> values);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    IRuleBuilder<TRequest> OneOf(IEnumerable<TProperty> values);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="values"></param>
    /// <returns></returns>
    IRuleBuilder<TRequest> OneOf(string message, IEnumerable<TProperty> values);
}