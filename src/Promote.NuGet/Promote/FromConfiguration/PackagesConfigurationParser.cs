using CSharpFunctionalExtensions;
using FluentValidation;
using NuGet.Versioning;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Promote.NuGet.Promote.FromConfiguration;

public static class PackagesConfigurationParser
{
    public static Result<PackagesConfiguration> TryParse(string input)
    {
        try
        {
            return Parse(input);
        }
        catch (Exception ex)
        {
            return Result.Failure<PackagesConfiguration>($"Failed to parse configuration: {ex}");
        }
    }

    public static PackagesConfiguration Parse(string input)
    {
        var deserializer = new DeserializerBuilder()
                           .WithNamingConvention(UnderscoredNamingConvention.Instance)
                           .WithTypeConverter(VersionRangeConverter.Instance)
                           .Build();

        var configuration = deserializer.Deserialize<PackagesConfiguration>(input);

        var validator = new PackagesConfigurationValidator();
        validator.ValidateAndThrow(configuration);

        return configuration;
    }

    public class VersionRangeConverter : IYamlTypeConverter
    {
        public static VersionRangeConverter Instance { get; } = new();

        public bool Accepts(Type type)
        {
            return type == typeof(VersionRange[]);
        }

        public object ReadYaml(IParser parser, Type type)
         {
            if (parser.TryConsume<Scalar>(out var scalar))
            {
                return new [] { ParseScalarAsVersionRange(scalar) };
            }

            if (parser.TryConsume<SequenceStart>(out _))
            {
                var items = new List<VersionRange>();

                while (parser.TryConsume<Scalar>(out var scalarItem))
                {
                    items.Add(ParseScalarAsVersionRange(scalarItem));
                }

                parser.Consume<SequenceEnd>();
                return items.ToArray();
            }

            throw new YamlException($"Unexpected token: {parser.Current?.GetType()}");
        }

        private static VersionRange ParseScalarAsVersionRange(Scalar scalar)
        {
            if (scalar.IsKey)
            {
                throw new YamlException($"Expected a version or version range, but got a key '{scalar.Value}'.");
            }

            if (NuGetVersion.TryParse(scalar.Value, out var version))
            {
                return new VersionRange(version, true, version, true);
            }

            if (VersionRange.TryParse(scalar.Value, out var versionRange))
            {
                return versionRange;
            }

            throw new YamlException($"Expected a valid version or version range, but got '{scalar.Value}'.");
        }

        public void WriteYaml(IEmitter emitter, object? value, Type type)
        {
            throw new NotImplementedException();
        }
    }

}
