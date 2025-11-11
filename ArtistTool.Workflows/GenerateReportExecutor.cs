using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.Logging;
using System.Text;

namespace ArtistTool.Workflows
{
    public class GenerateReportExecutor(string id, ILogger<GenerateReportExecutor> logger) :
        ReflectingExecutor<GenerateReportExecutor>(id), IMessageHandler<MarketingWorkflowContext, MarketingWorkflowContext>
    {

        private static int received;
        
public ValueTask<MarketingWorkflowContext> HandleAsync(MarketingWorkflowContext message, IWorkflowContext context, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref received);

            logger.LogInformation("Fan-in to report generator, iteration: {iteration} if {total}", received, message.FanInNodes);

            if (received < message.FanInNodes)
            {
                logger.LogInformation("Waiting for additional nodes to report in.");
                return ValueTask.FromResult(message);
            }

            var mermaidPath = Path.Combine(message.BaseDirectory, "workflow.md");
            var mermaid = $"# Workflow diagram\r\n\r\n```mermaid\r\n{message.WorkflowDiagram}\r\n```\r\n";
            File.WriteAllText(mermaidPath, mermaid);

            var templateCss = Path.Combine(AppContext.BaseDirectory, "index.css");
            var targetCss = Path.Combine(message.BaseDirectory, "index.css");

            File.Copy(templateCss, targetCss);

            var sourcePhoto = message.Photo!.Path;
            var targetPhoto = Path.Combine(message.BaseDirectory, Path.GetFileName(sourcePhoto));

            File.Copy(sourcePhoto, targetPhoto);

            var sb = new StringBuilder(@$"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Marketing report for {message.Photo.Title}</title>
    <link rel=""stylesheet"" href=""index.css"">
</head>
<body>
    <header>
        <h1>Marketing report for {message.Photo.Title}</h1>
        <p class=""subtitle"">Generated market research</p>
    </header>
");
            sb.AppendLine(@$"<main>
        <article>
            <section>
                <h2>Introduction</h2>
                <p>The photograph titled, ""{message.Photo.Title}"" is being considered as the basis for a new work of art and possibly several different styles of print.</p>
                <img src=""{Path.GetFileName(targetPhoto)}"" alt=""{message.Photo.Title}"">
                <p>Here is a description that was generated for the photograph.</p>
                <blockquote>
                    <p>{message.Photo.Description}</p>
                </blockquote>
            </section>");

            sb.AppendLine($@"<section>
                <h2>Photo critique</h2>
                <ul>");
            
            foreach (var critique in message.Critique!.Result!.Critiques)
            {
                sb.AppendLine($"<li><strong>{critique.Area} (Rating: {critique.Rating}/10)</strong>");
                sb.AppendLine("<ul>");
                sb.AppendLine("<li><em>Praise:</em> " + critique.Praise + "</li>");
                sb.AppendLine(value: "<li><em>Improvement Suggestion:</em> " + critique.ImprovementSuggestion + "</li>");
                sb.AppendLine("</ul>");
            }

            sb.AppendLine("</ul>");
            sb.AppendLine("</section>");

            sb.AppendLine("<section><h2>Market research</h2>");

            foreach (var medium in message.MediumPreviews.Keys)
            {
                var researchNode = message.MediumPreviews[medium];
                sb.AppendLine($@"<section>
                    <h3>Research for medium: {medium}</h3>");
                sb.AppendLine($@"<figure>
                    <img src=""{Path.GetFileName(researchNode.Result!.ImagePath)}"" alt=""{medium}"">
                    <figcaption>This is an example of the photograph as a {medium}</figcaption>
                </figure>");
                string area = string.Empty;
                string topic = string.Empty;
                foreach (var result in message.Research.Where(r => r.Result!.Medium == medium))
                {
                    if (result.Result!.Area != area)
                    {
                        if (!string.IsNullOrWhiteSpace(area))
                        {
                            sb.AppendLine("</section>");
                        }
                        sb.AppendLine($@"<section>
                            <h4>{result.Result!.Area!}</h4>");
                        area = result.Result!.Area!;
                    }

                    sb.AppendLine($@"<p><strong>{result.Result!.Topic}</strong></p>");
                    var html = Markdig.Markdown.ToHtml(result.Result!.Content);
                    sb.AppendLine(html);
                }
                sb.AppendLine("</section>");
            }

            sb.AppendLine("</section>");

            sb.AppendLine($@" </article>
    </main>

    <footer>
        <p>Generated on <time datetime=""{DateTime.Now.Year:0000}-{DateTime.Now.Month:00}-{DateTime.Now.Day:00}"">{DateTime.Now.ToShortDateString()}</time></p>
        <p>ArtistTool</p>
    </footer>
</body>
</html>
");
            var targetFile = Path.Combine(message.BaseDirectory, "index.html");

            File.WriteAllText(targetFile, sb.ToString());

            logger.LogDebug("Wrote final report to {targetPath}", targetFile);

            return ValueTask.FromResult(message);
        }
    }
}
