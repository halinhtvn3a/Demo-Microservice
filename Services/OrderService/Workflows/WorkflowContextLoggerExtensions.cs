using Dapr.Workflow;
using Microsoft.Extensions.Logging;

namespace OrderService.Workflows;

public static class WorkflowContextLoggerExtensions
{
	public static ILogger<T> CreateReplaySafeLogger<T>(this WorkflowContext _)
	{
		// NOTE: This is a fallback implementation used only if the Dapr.Workflow extension method is missing.
		// It creates a simple console logger to keep the workflow compilable.
		var factory = LoggerFactory.Create(builder => builder.AddSimpleConsole(options =>
		{
			options.SingleLine = true;
			options.TimestampFormat = "HH:mm:ss ";
		}));

		return factory.CreateLogger<T>();
	}
}