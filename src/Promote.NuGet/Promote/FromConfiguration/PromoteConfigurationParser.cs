using CSharpFunctionalExtensions;
using FluentValidation;
using NuGet.Versioning;
using Promote.NuGet.Commands.Requests;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Promote.NuGet.Promote.FromConfiguration;

public static class PromoteConfigurationParser
{
    public static Result<PromoteConfiguration> TryParse(string input)
    {
        try
        {
            return Parse(input);
        }
        catch (Exception ex)
        {
            return Result.Failure<PromoteConfiguration>($"Failed to parse configuration: {ex}");
        }
    }

    public static PromoteConfiguration Parse(string input)
    {
        var deserializer = new DeserializerBuilder()
                           .WithNamingConvention(HyphenatedNamingConvention.Instance)
                           .WithTypeConverter(VersionRangeConverter.Instance)
                           .Build();

        var configuration = deserializer.Deserialize<PromoteConfiguration>(input);

        var validator = new PackagesConfigurationValidator();
        validator.ValidateAndThrow(configuration);

        return configuration;
    }

    public class VersionRangeConverter : IYamlTypeConverter
    {
        public static VersionRangeConverter Instance { get; } = new();

        public bool Accepts(Type type)
        {
            return type == typeof(IPackageVersionPolicy[]);
        }

        public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            if (parser.TryConsume<Scalar>(out var scalar))
            {
                return new [] { ParseScalarAsVersionRange(scalar) };
            }

            if (parser.TryConsume<SequenceStart>(out _))
            {
                var items = new List<IPackageVersionPolicy>();

                while (parser.TryConsume<Scalar>(out var scalarItem))
                {
                    items.Add(ParseScalarAsVersionRange(scalarItem));
                }

                parser.Consume<SequenceEnd>();
                return items.ToArray();
            }

            throw new YamlException($"Unexpected token: {parser.Current?.GetType()}");
        }

        private static IPackageVersionPolicy ParseScalarAsVersionRange(Scalar scalar)
        {
            if (scalar.IsKey)
            {
                throw new YamlException($"Expected a version or version range, but got a key '{scalar.Value}'.");
            }

            if (scalar.Value == "latest")
            {
                return new LatestPackageVersionPolicy();
            }

            if (NuGetVersion.TryParse(scalar.Value, out var version))
            {
                return new ExactPackageVersionPolicy(version);
            }

            if (VersionRange.TryParse(scalar.Value, out var versionRange))
            {
                return new VersionRangePackageVersionPolicy(versionRange);
            }

            throw new YamlException($"Expected a valid version or version range, but got '{scalar.Value}'.");
        }

        public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

}
