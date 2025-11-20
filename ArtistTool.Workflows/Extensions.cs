using ArtistTool.Domain;
using Microsoft.Extensions.AI;
using System.Text;
using System.Text.RegularExpressions;

namespace ArtistTool.Workflows
{
    public static class Extensions
    {
        public static DataContent AsSerializedPhoto(this Photograph photo)
        {
            var file = File.ReadAllBytes(photo.Path);
            var dc = new DataContent(new ReadOnlyMemory<byte>(file), photo.ContentType)
            {
                Name = nameof(Photograph)
            };
            return dc;
        }

        public static string ExtractPhotoId(this ChatMessage message)
        {
            var candidate = message.Contents
                .OfType<TextContent>()
                .First(tc => tc.Text.StartsWith("PhotoId: "));
            var photoId = candidate.Text["PhotoId: ".Length..].Trim();
            return photoId;
        }

        public static DataContent AsSerializedChatParameter<T>(this T obj)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(obj);
            var bytes = Encoding.UTF8.GetBytes(json);
            return new DataContent(new ReadOnlyMemory<byte>(bytes), $"application/json+{typeof(T).Name.ToLowerInvariant()}");
        }

        public static string AsDebugString(this ChatResponse resp)
        {
            var sb = new StringBuilder($"Text: {resp.Text}{Environment.NewLine}");
            if (resp.Messages != null && resp.Messages.Count > 0)
            {
                sb.AppendLine("Messages:");
                foreach (var message in resp.Messages)
                {
                    sb.AppendLine(message.AsDebugString());
                }
            }
            else
            {
                sb.AppendLine("No messages.");
            }
            return sb.ToString();
        }

        public static string AsDebugString(this ChatMessage cm)
        {
            var sb = new StringBuilder($"Role: {cm.Role}{Environment.NewLine}");
            foreach (var content in cm.Contents)
            {
                if (content is DataContent dc)
                {
                    sb.AppendLine($"DataContent - MediaType: {dc.MediaType}, Size: {dc.Data.Length} bytes");
                }
                else if (content is TextContent tc)
                {
                    sb.AppendLine($"TextContent - Text: {tc.Text}");
                }
                else
                {
                    sb.AppendLine($"Unknown content type: {content.GetType().Name}");
                }
            }

            return sb.ToString();
        }

        public static string CompressCssForLlm(this string cssContent)
        {
            var sb = new StringBuilder();

            // Remove comments
            cssContent = Regex.Replace(cssContent, @"/\*.*?\*/", "", RegexOptions.Singleline);

            // Extract CSS rules (selector { properties })
            var rulePattern = @"([^{]+)\{([^}]+)\}";
            var matches = Regex.Matches(cssContent, rulePattern);

            foreach (Match match in matches)
            {
                var selector = match.Groups[1].Value.Trim();
                var properties = match.Groups[2].Value.Trim();

                // Compress whitespace in properties
                properties = Regex.Replace(properties, @"\s+", " ");

                // Only keep key layout/visual properties
                var relevantProps = new List<string>();
                var propPattern = @"([a-z\-]+)\s*:\s*([^;]+);?";
                var propMatches = Regex.Matches(properties, propPattern);

                foreach (Match prop in propMatches)
                {
                    var propName = prop.Groups[1].Value;
                    var propValue = prop.Groups[2].Value.Trim();

                    // Filter to most relevant properties for layout/styling
                    if (IsRelevantCssProperty(propName))
                    {
                        relevantProps.Add($"{propName}: {propValue}");
                    }
                }

                if (relevantProps.Count > 0)
                {
                    sb.AppendLine($"{selector} {{ {string.Join("; ", relevantProps)} }}");
                }
            }

            return sb.ToString();
        }

        private static bool IsRelevantCssProperty(string propertyName)
        {
            // Include properties most relevant for understanding layout and appearance
            var relevant = new HashSet<string>
            {
                "display", "position", "flex", "grid", "width", "height",
                "margin", "padding", "background", "color", "border",
                "font-size", "font-weight", "text-align", "gap", "justify-content",
                "align-items", "flex-direction", "overflow", "opacity"
            };

            return relevant.Contains(propertyName) ||
                   relevant.Any(propertyName.StartsWith);
        }

    }
}
