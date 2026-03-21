// ReSharper disable CheckNamespace

namespace Microsoft.Extensions.Configuration;

public static class ConfigurationExtensions
{
    extension(IConfiguration configuration)
    {
        public string RequireConnectionString(string name)
        {
            var connectionString = configuration.GetConnectionString(name);

            return string.IsNullOrWhiteSpace(connectionString)
                ? throw new InvalidOperationException($"Connection string '{name}' is not configured.")
                : connectionString;
        }

        public string GetString(string key)
        {
            var value = configuration.GetValue<string>(key);

            return string.IsNullOrWhiteSpace(value)
                ? throw new InvalidOperationException($"Configuration value '{key}' is not configured.")
                : value;
        }

        public Uri GetUri(string key)
        {
            var value = configuration.GetString(key);

            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
                throw new InvalidOperationException($"Configuration value '{key}' must be a valid absolute URI.");

            return uri;
        }

        public Uri GetUri(string key, string path)
        {
            var url = configuration.GetUri(key);

            return new Uri(url, path);
        }

        public T[] GetArray<T>(string key)
        {
            var values = configuration
                .GetSection(key)
                .Get<T[]>();

            if (values is null || values.Length == 0)
                throw new InvalidOperationException($"Configuration section '{key}' is not configured.");

            return values;
        }
    }
}
