using ArtistTool.Domain.Agents;
using ArtistTool.Services;
using ArtistTool.Workflows.Messages;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Text;

namespace ArtistTool.Workflows
{
    public class ReportService(IProjectManager pm, ILoggerFactory factory, ClientFactory clientFactory, IMessageHub hub)
    {
        private double TotalTasks = 0;
        private double CompletedTasks = 0;
        private string css = string.Empty;
        private string compressedCss = string.Empty;
        private readonly Lock _mutex = new();
        private ILogger logger = factory.CreateLogger<ReportService>();
        private ReportSection? root;

        private void UpdateReportStatus(MarketReport report, string message)
        {
            var reportPct = (int)((CompletedTasks / TotalTasks) * 100);
            report.ReportWritingPct = reportPct;
            report.Status = message;
            hub.Publish(new ReportUpdate(report.Id, report));
        }

        private DataContent GetCssAsFile(bool compressed = true) => new (Encoding.UTF8.GetBytes(compressed ? compressedCss : css), "text/css");
        
        private void AppendCss(string newCss)
        {
            _mutex.Enter();
            if (string.IsNullOrEmpty(css))
            {
                css = newCss;
            }
            else
            {
                var oldCss = css;
                var cssReplace = $"{oldCss}{Environment.NewLine}{newCss}";
                css = cssReplace;
            }
            compressedCss = css.CompressCssForLlm();
            _mutex.Exit();
        }

        /// <summary>
        /// Aggregates nodes and builds the table of contents.
        /// </summary>
        /// <param name="node">Node being processed</param>
        /// <param name="link">Internal link</param>
        /// <param name="toc">Table of contents buffer</param>
        /// <returns>The aggregated html and css.</returns>
        private (string html, string css) BuildReport(ReportSection? node = null, string? link = "_", StringBuilder? toc = null)
        {
            toc ??= new StringBuilder("<section class='toc'><ul class='toc-list'>");
            if (node is null)
            {
                node = root;
                link = "top";
            }
            else
            {
                var nameLink = link!.Replace(' ', '_');
                link = $"{link}_{nameLink}";
                toc.AppendLine($"<li class='toc-item'><a href='#{link}'>{node!.Name}</a></li>");
            }

            var thisHtml = $"<div id='{link}'>&nbsp;</div>{node!.Snippet!.Html}";
            var thisCss = node.Snippet.NewCss;

            if (node.Children.Count > 0)
            {
                toc.AppendLine("<ul>");
                foreach (var child in node.Children)
                {
                    var (childHtml, childCss) = BuildReport(child, link, toc);
                    var old = thisHtml;
                    thisHtml = $"{old}{Environment.NewLine}{childHtml}";
                    var oldCss = thisCss;
                    thisCss = $"{oldCss}{Environment.NewLine}{childCss}";
                }
                toc.AppendLine("</ul>");
            }
            return (thisHtml, thisCss);
        }

        public async Task<string> PublishReportAsync(MarketReport report)
        {
            logger.LogDebug("Report requested for photo with id {PhotoId} and report {ReportId}", report.Id, report.ReportId);

            if (report.ReportWritingPct > 0 || !report.AnalysisDone)
            {
                logger.LogDebug("Report already in progress or analysis is not yet finished, skipping.");
                return string.Empty;
            }

            report.ReportWritingPct = 1;
            UpdateReportStatus(report, "Starting report writing...");

            TotalTasks = 2 + report.MediumReports.Length * 4 + report.MediumReports.Select(mr => mr.Socials.Length).Sum(); // intro + critique + (medium intro, pricing, campaign, socials) per medium

            const string agentName = "HTML and CSS site wizard";
            
            const string instructions = @"
Input Assumptions

You will receive structured data or descriptive instructions for report content (e.g., KPIs, charts, summaries).
Use a soft, muted/neutral palette, with light theme preferred. The grid should be responsive. Prefer neutral defaults (e.g., system fonts, light background, dark text).

Output Requirements:

HTML and CSS only—no additional commentary unless explicitly requested.
Wrap HTML and CSS in separate code blocks:

<!-- HTML --> followed by <html>...</html>
/* CSS */ followed by body { ... }

**Only include CSS that is new, and try to use existing classes from the existing CSS file that will be included in prompts**

Include:

Semantic structure (<header>, <main>, <section>, <footer>).
Placeholder text for the table of contents (<toc/>).
Responsive layout using flexbox or grid.

Validate:

No broken tags.
CSS selectors scoped to avoid conflicts.
Mobile-friendly design (use relative units, media queries).

Constraints

Do not include JavaScript unless explicitly requested.
Do not embed external libraries (Bootstrap, Tailwind) unless specified.
Keep CSS minimal and performant (avoid unnecessary animations or heavy effects).
Ensure WCAG 2.1 AA accessibility where possible.

Tone & Style

Code should be clean, commented where necessary, and easy to print.

If you are presented with data you are welcome to create charts and graphs if they help emphasize the point and convey the information.

Use consistent naming conventions (e.g., report-header, metric-card).";
            var baseMessage = new ChatMessage(ChatRole.System, instructions);
 
            var client = new HtmlChatClient(await clientFactory.GetChatClientAsync(ChatClientType.Conversational, instructions, true),
                () => [GetCssAsFile()], factory);
            
            var agent = new ChatClientAgent(client, instructions, agentName, tools: [AIFunctionFactory.Create(TransformMarkdownToHtml)],
              loggerFactory: factory);

            UpdateReportStatus(report, "Starting report generation...");

            logger.LogDebug("Writing intro to report...");
            UpdateReportStatus(report, "Writing report introduction...");

            var response = await agent.RunAsync<HtmlSnippetResponse>(string.Format(@"Produce a partial HTML page to start the marketing report about a photograph.

Do not close the <html> or <body> tags.
Output HTML only (no narrative text) and initial CSS based on your desire design. Try to accommodate future requests by producing a through and easy to understand set of CSS classes.

Follow the structure, semantics, and accessibility rules below.

Inputs 

{0:Title} → photograph title
{1:Description} → photograph description
{2:Categories} → comma‑separated category names
{3:Tags} → comma‑separated tags

Required Output Contract

Head area at top of a typical page:
<title> must be: Marketing report for the title.
Make sure there is a reference to index.css (CSS reference)

Header block:

Visible heading text: Marketing report for title (use <header> + <h1>)

Table of contents placeholder:
A standalone line containing exactly: <toc/>
To style the toc, assume TOC will render inside <section class='toc'> and will contain only <ul class='toc-list'> with <li class='toc-item'>. Do not populate it—leave it empty in this partial.

Introduction section immediately after <toc/>:

Show the image: photo.jpg (file is at root; use exact filename photo.jpg)
Include short prose containers for description and tags (semantic elements).

Photo details section:

Render Title, Description, Categories, Tags as structured HTML (definition list or table).

Accessibility & semantics:

Use landmark elements: <header>, <main>, <section>
Provide alt text on the image using {{Title}}.
Use descriptive labels for data.


No closing tags: leave <html> and <body> open.

Constraints

No inline CSS; styling is external (index.css).
No JavaScript.
Keep IDs/classes predictable (report-header, intro, photo-meta, etc.).
Validate: no unclosed elements; attributes quoted.

Section Order

<!DOCTYPE html> + <html lang='en'> + <head> (title + CSS)
<body>
<header>
<main>
<toc/> (on its own line)
<section class='intro'> (image + summary)
<section class='photo-details'> (structured metadata)
(Do not add closing </body></html>.) 
Use H tags where appropriate. You will be informed of which level  you are working in.",
                report.Photo!.Title,
                report.Photo.Description,
                string.Join(", ", report.Photo.Categories),
                string.Join(", ", report.Photo.Tags)));

            CompletedTasks++;
            UpdateReportStatus(report, "Wrote report introduction...");
   
            var result = response.Result;
            AppendCss(response.Result.NewCss);
            
            root = new ReportSection(1, report.Photo!.Title)
            {
                Snippet = result
            };

            UpdateReportStatus(report, "Writing photo critique...");

            var critique = new StringBuilder(@"Render an HTML snippet for a photo critique section. Requirements:

Start with <h2>Photo critique</h2>.
Display the critique scores, praises, and improvements and comment in a clear format (e.g., 8/10 – <span class='success'>Great composition</span>, <span class='danger'>could be better</span>).
Include a visual element if appropriate (e.g., a simple bar chart or rating graph using HTML/CSS).
Wrap content in a semantic container (<section class='photo-critique'>).
Assume CSS will be linked externally (index.css). Try to use existing css if possible and only generate new if absolutely needed.
Do not close <html> or <body> tags.

Keep code clean and accessible (use ARIA roles if needed). Here is the data:
");

            foreach (var cr in report.Critique!.Critiques.OrderBy(c => c.Rating))
            {
                critique.AppendLine($"{cr.Rating}/10 [{cr.Area}] The good: {cr.Praise} The opportunity: {cr.ImprovementSuggestion}");
            }
  
            var critiqueSection = await agent.RunAsync<HtmlSnippetResponse>(critique.ToString());
            logger.LogInformation("Critique agent returned with this to say {AgentReply}", critiqueSection.Result.Commentary);

            CompletedTasks++;
            AppendCss(critiqueSection.Result.NewCss);

            var critiqueReportSection = new ReportSection(2, "Critique")
            {
                Snippet = critiqueSection.Result
            };
            UpdateReportStatus(report, "Writing medium reports...");
            root.AddChild(critiqueReportSection);

            List<Task> mediums = [];

            foreach (var mediumReport in report.MediumReports.OrderBy(mr => mr.Medium))
            {
                UpdateReportStatus(report, $"Writing report for medium {mediumReport.Medium}...");
                mediums.Add(BuildSubReportForMediumAsync(report, agent, result, mediumReport));
            }

            await Task.WhenAll(mediums);

            logger.LogDebug("Finished parsing results, building report output...");
            UpdateReportStatus(report, "Done parsing results, writing output to disk...");
            var sb = new StringBuilder("<section class='toc'><ul class='toc-list'>");
            var (html, css) = BuildReport(toc: sb);
            sb.AppendLine("</ul></section>");
            if (html.Contains("<toc/>"))
            {
                html = html.Replace("<toc/>", sb.ToString());
            }
            else
            {
                html = html.Replace("<body>", $"<body>{sb}");
            }
                var oldHtml = html;
            html = $"{oldHtml}</body></html>";

            await pm.WriteReportInfoAsync(report.Id, report.ReportId, html, css);

            var reportDir = pm.GetReportDirectory(report.Id, report.ReportId);
            logger.LogDebug("Report published to directory {ReportDir}", reportDir);

            var indexFile = Path.Combine(reportDir, "index.html");
            report.Done = true;
            UpdateReportStatus(report, "Report published.");
            hub.Publish(new ReportFinished(report.Id, report, indexFile));
            return indexFile;
        }

        private async Task BuildSubReportForMediumAsync(MarketReport report, ChatClientAgent agent, HtmlSnippetResponse result, MediumReport mediumReport)
        {
            var mediumSection = new ReportSection(2, $"Medium: {mediumReport.Medium}");
           
            root!.AddChild(mediumSection);

            var mediumFilename = mediumReport.Medium.Replace(", ", "_");

            UpdateReportStatus(report, $"Writing preview to disk for medium {mediumReport.Medium}");
            // write the preview for the medium
            var fileName = pm.GetFilenameForPhoto(mediumFilename);
            string file = await pm.WriteReportAssetAsync(report.Id, report.ReportId, fileName, [..mediumReport.MediumPreview!.Data.Span]);
            
            var pricing = mediumReport.PriceResponse;
            
            var productInfo = mediumReport.ProductResearchResponse;
            
            var preliminaryInfoBoard = $@"
Product information, suggested title: {productInfo!.ProductTitle}.
Sizzle phrase: {productInfo!.Sizzle}
SEO keywords: {string.Join('|', productInfo!.SearchEngineKeywords)}
Ad copy: {productInfo!.DetailedAdCopy}";

            var research = mediumReport.ResearchResponses[0];
            var researchInfo = $@"
{research.Summary}{Environment.NewLine}{research.DetailedAnswer}";
            UpdateReportStatus(report, $"Writing medium section for medium {mediumReport.Medium}");
            var snippet = await agent.RunAsync<HtmlSnippetResponse>(@$"Render an HTML snippet for an H2 section introducing the medium. Requirements:

Start with <h2> containing: Introducing the medium '{mediumReport.Medium}'.
Include 2–3 paragraphs describing the medium and its relevance to the photograph.
Insert an image referencing '{fileName}' (assume root path - this is a preview of the photograph rendered to the medium).

Add a subsection for preliminary research:

Heading: <h3>Preliminary Research</h3>
Render {researchInfo} in a visually appealing format. Try to use as much of the existing CSS as possible.

Wrap everything in <section class='medium-intro'>.
Do not close <html> or <body> tags.
Use semantic HTML and accessibility best practices.");

            logger.LogInformation("Medium researcher had this to say: {AgentCommentary}", snippet.Result.Commentary);


            mediumSection.Snippet = snippet.Result;
            AppendCss(snippet.Result.NewCss);
            CompletedTasks++;
            UpdateReportStatus(report, $"Writing pricing section for medium {mediumReport.Medium}");

            var priceSnipper = await agent.RunAsync<HtmlSnippetResponse>($@" Produce a partial HTML snippet for a Product & Pricing section.

Output HTML only (no narrative outside of tags).
Do not close the <html> or <body> tags.
Use semantic, accessible markup.
CSS is injected separately; use the provided base stylesheet as much as possible.

Required Output Contract

A <section> wrapper. Preliminary info: 
{preliminaryInfoBoard}

An H3 header with clear label and key info:

An executive Summary: {pricing!.ExecutiveSummary}

Subsequent sections in H4: 

A description and visual of the market comps: {pricing.MarketComps}

Small (8 inch x 10 inch) estimated cost ${pricing.SmallSizeEstimatedCost} and suggested retail ${pricing.SmallSizeRecommendedPrice}
Medium (16 inch x 24 inch) estimated cost ${pricing.MediumSizeEstimatedCost} and suggested retail ${pricing.MediumSizeRecommendedPrice}
Large (30 inch x 40 inch) estimated cost ${pricing.LargeSizeEstimatedCost} and suggested retail ${pricing.LargeSizeRecommendedPrice}

Preliminary info block, Market comps block:
Pricing table with Size / Estimated Cost / Suggested Retail / Profit Margin columns.
Compute profit margin.
Visual margin indicator (icons or an inline SVG bar) per row.
Use emojis (e.g., 💰) or embed a simple, accessible SVG bar with width proportional to margin percentage (no JS).

Constraints

No inline styles except for safe, minimal width attributes on SVG rects.
No JavaScript.
Keep IDs/classes predictable: product-pricing, executive-summary, pricing-table, margin-bar.
Quote all attributes; ensure valid, well‑formed HTML.
");
            CompletedTasks++;
            UpdateReportStatus(report, $"Added pricing for medium {mediumReport.Medium}");
            var pricingSection = new ReportSection(3, $"Pricing for {mediumReport.Medium}")
            { 
                Snippet = priceSnipper.Result
            };
            AppendCss(priceSnipper.Result.NewCss);
            mediumSection.AddChild(pricingSection);
            
            var campaign = mediumReport.MarketingCampaignResponse;
            
            UpdateReportStatus(report, $"Writing marketing campaign section for medium {mediumReport.Medium}");
            
            var marketingSnippet = await agent.RunAsync<HtmlSnippetResponse>(@$"
Render a partial HTML snippet for an H3 section titled Marketing Campaign. Requirements:

Start with <h3>Marketing Campaign</h3>.
Include:

Primary and Secondary Segments
Position Statement

As <h4> subsections:
Message Pillars (as a bullet list)
Channels (as a bullet list or tag-style chips)
Campaign Steps (ordered list or timeline visualization)

Add a visual element:
A sample calendar or Gantt-style timeline showing campaign phases (Pre-launch, Launch, Sustain).
Use semantic HTML and accessible markup

Wrap everything in its own section. Try to use the CSS already provided.
Do not close <html> or <body> tags.
Keep code clean and responsive-ready.

Input data: 

Primary and secondary segments: {campaign!.PrimarySegment} {campaign!.SecondarySegment} 
Position statement: {campaign!.PositionStatement}
Pillars: {string.Join(',', campaign!.MessagePillars)}
Channels: {string.Join(',', campaign!.Channels)}
Campaign: {string.Join(Environment.NewLine, campaign!.Campaign.Order())}. Existing CSS: {compressedCss}");

            CompletedTasks++;
            UpdateReportStatus(report, $"Added marketing campaign for medium {mediumReport.Medium}");

            AppendCss(marketingSnippet.Result.NewCss);

            var marketingSection = new ReportSection(3, $"Marketing campaign for {mediumReport.Medium}")
            { 
                Snippet = marketingSnippet.Result
            };
            
            mediumSection.AddChild(marketingSection);

            UpdateReportStatus(report, "Generating social media channels...");
            var introChannels = await agent.RunAsync<HtmlSnippetResponse>($@"
Render a partial HTML snippet for a social media section at H3 level introducing the medium '{mediumReport.Medium}. Requirements:

Start with <h3> containing: Social Media Strategy for {{{{mediumReport.Medium}}}}.

Add brief introductory text explaining that this section will outline the content strategy for each channel in subsequent subsections.

Wrap everything in <section>.
Keep HTML clean and semantic.
Do not close <html> or <body> tags.
Use as much of the existing css as possible.");

            logger.LogInformation("Social media agent returned with this to say {AgentReply}", introChannels.Result.Commentary);
            CompletedTasks++;
            UpdateReportStatus(report, $"Wrote social media intro for medium {mediumReport.Medium}");
            var socialSection = new ReportSection(3, $"Social media content strategy for {mediumReport.Medium}")
                {
                    Snippet = introChannels.Result
                };
            
            mediumSection.AddChild(socialSection);
            AppendCss(introChannels.Result.NewCss);

            var socialTasks = mediumReport.Socials.OrderByDescending(s => s.Rank).Select(async channel =>
            {
                UpdateReportStatus(report, $"Writing social media channel section for {channel.ChannelName} on medium {mediumReport.Medium}");
                var channelResponse = await agent.RunAsync<HtmlSnippetResponse>($@"
Generate a partial HTML snippet for a social media channel campaign section at H4 level. Requirements:

Start with <h4> containing the channel name:
Social Media Campaign: {channel.ChannelName}
Wrap everything in <section> elements.

Include the following elements in order:

Channel Name & Type

Display as a short intro paragraph:
Channel: {channel.ChannelName} | Type: {channel.ChannelType}

Relative Rank
Show rank visually (e.g., badge or styled span):
Rank: {channel.Rank}

Pros and Cons

Use two separate <ul> lists with headings:
<h5>Pros</h5> and <h5>Cons</h5>.

Best Practices

Render as a bullet list under <h5>Best Practices</h5>.

Recommended Hashtags

Display as styled tags or inline chips under <h5>Recommended Hashtags</h5>.

Example Posts

Use a section with each post in its own <blockquote> or <p> for clarity.

{string.Join(Environment.NewLine, channel.Posts)}

Ensure semantic HTML and accessibility (use headings, lists, ARIA roles if needed).
Do not close <html> or <body> tags.
Try to use the existing CSS as much as possible.");

                UpdateReportStatus(report, $"Added social media channel section for {channel.ChannelName} on medium {mediumReport.Medium}");
                var reportSection = new ReportSection(4, $"{channel.ChannelName} content strategy for medium {mediumReport.Medium}")
                {
                    Snippet = channelResponse.Result
                };
                logger.LogInformation("Social media agent for {Channel} regarding medium {Medium} had this to say {AgentReply}",
                    channel.ChannelName, 
                    mediumReport.Medium, 
                    channelResponse.Result.Commentary);

                CompletedTasks++;
                AppendCss(channelResponse.Result.NewCss);
                socialSection.AddChild(reportSection);
            });
            await Task.WhenAll(socialTasks);
        }

        [Description("Takes markdown content and transforms it into HTML.")]
        public string TransformMarkdownToHtml(string markdown)
        {
            var html = Markdig.Markdown.ToHtml(markdown);
            logger.LogDebug("Tool call: translated markdown to html.");
            return html;
        }
    }
}
