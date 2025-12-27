namespace conduit.validation;

public static class ConduitPipeBuilderExtensions
{
    extension<TRequest, TResponse>(IConduitPipeBuilder<TRequest, TResponse> self) where TRequest : class, IRequest<TResponse> where TResponse : class
    {
        /// <summary>
        /// Explicitly adds validation to this pipeline for the request model.
        /// </summary>
        /// <returns>The current pipe builder instance.</returns>
        public IConduitPipeBuilder<TRequest, TResponse> WithValidation()
        {
            self.AddStage<ValidationStage<TRequest, TResponse>>();
            return self;
        }

        /// <summary>
        /// Specifically excludes this pipeline from any global model validation.
        /// </summary>
        /// <returns>The current pipe builder instance.</returns>
        public IConduitPipeBuilder<TRequest, TResponse> ExcludeValidation()
        {
            self.ExcludeValidation();
            return self;
        }
    }
}