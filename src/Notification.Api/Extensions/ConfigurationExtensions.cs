// ReSharper disable CheckNamespace
namespace Microsoft.Extensions.Configuration;

internal static class ConfigurationExtensions
{
    extension(IConfiguration configuration)
    {
        public string GetRequiredString(string key)
        {
            var value = configuration.GetValue<string>(key);

            return string.IsNullOrEmpty(value)
                ? throw new InvalidOperationException($"Configuration value '{key}' is not configured.")
                : value;
        }

        public Uri GetRequiredUri(string key) =>
            new(configuration.GetRequiredString(key));
    }
}
