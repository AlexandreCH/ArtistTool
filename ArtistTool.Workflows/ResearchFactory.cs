using ArtistTool.Domain.Agents;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Logging;

namespace ArtistTool.Workflows
{
    public class ResearchFactory(ClientFactory clientFactory, ILoggerFactory factory)
    {
        const string researchAgent = "Research specialist";
        const string researchInstructions = @"Role
You are a Research Specialist for art and photography. Your job is to gather accurate, decision-ready information about a specific photograph, options for printing and finishing, and market pricing to inform a go-to-market strategy.
Objectives

Artwork Profile: Identify the photograph’s provenance (artist, date, series, subject, licensing status), stylistic context, notable exhibitions/publications, and similar works.
Production Options: Recommend print mediums, sizes, substrates, finishes, and mounting/framing options optimized for the work’s aesthetic and intended environment.
Vendors & Costs: Compare reputable labs/vendors with price ranges, lead times, archival ratings, ICC/color management notes, and shipping/packaging practices.
Market Research & Pricing Strategy: Provide comps and a tiered pricing strategy with rationale (editioning, scarcity, brand/artist position, size multipliers, cost-based and value-based pricing).
Compliance & Risk: Clarify copyright/licensing requirements, fair use limits, model/property releases if applicable, and regional tax/shipping implications.";

        const string mediumResearchPrompt = @"
TASK:
- Analyze how this medium complements or conflicts with the photograph.
- Provide:
   1) Pros (specific to photo traits)
   2) Cons (specific to photo traits)
   3) Overall recommendation (rating + rationale)
   4) Alternatives (if needed)

CONSIDER:
- Archival quality
- Color fidelity
- Texture & finish
- Durability & handling
- Display environment (lighting, humidity)

Here are the photograph and medium details: ";

        const string priceResearchPrompt = @"Assuming this is a limited edition print targeting collectors in the United States...
TASK:
- Provide:
   1) Executive Summary (price bands)
   2) Market Comps (5–10 examples with links)
   3) Pricing Table (cost breakdown, retail)
   4) Rationale (cost + value-based)
   5) Risks & Sensitivity Analysis
   6) Alternatives if needed

CONSIDER:
- Medium prestige and archival quality
- Audience willingness-to-pay
- Channel commissions and fees
Here are the photo and medium details: ";

        const string marketingAgent = "Marketing Expert";

        const string marketingInstructions = @"As a marketing expert you will be asked to provide your expert opinion about fine art photographs printed on a specific medium. You will help market the product with a powerful, engaging, and compelling pitch, and will design a marketing campaign to bring the fine art to market.";

        const string productSalesPagePrompt = @"Task
Craft a compelling product description that:

Highlights the unique features of the photograph (subject, mood, color palette).
Explains the benefits of the chosen medium (texture, archival quality, finish).
Uses sensory and emotional language to connect with the buyer.
Ends with a strong call to action (e.g., “Own this limited edition today” or “Bring timeless elegance to your space”).

Output Requirements

Length: 120–180 words.
Structure:

Hook (1 sentence to grab attention)
Story + Features (photo + medium synergy)
Benefits (why it matters for the buyer)
Call to Action (urgent, inspiring)


Tone: Aspirational, art-focused, persuasive.
Optional: Include keywords for SEO (e.g., fine art print, archival quality, limited edition).";

        const string marketingCampaignPrompt = @"ROLE: Marketing Campaign Strategist

Assuming a limited edition fine art photograph that is being marketed to potential collectors in the United States. Design a comprehensive marketing campaign that includes the following components:

AUDIENCE & INSIGHT
- Primary Segment: [who they are; need/occasion; objections]
- Secondary Segment: [optional]
- Key Insight (JTBD): [single motivating truth]

POSITIONING & KEY MESSAGES
- One‑line Positioning: [value + who + why it’s different]
- Message Pillars (2–3) + Reasons to Believe:
  1) [Pillar] — RTB: [specific photo+medium proof]
  2) [Pillar] — RTB: [proof]
  3) [Pillar] — RTB: [proof]
- Tone/Voice: [on‑brand notes]

CHANNEL PLAN & ASSETS
- Owned: [PDP update, email announce, blog/gallery page]
- Earned: [gallery partners, PR pitch, influencer seeding]
- Paid: [social/display/retargeting; flighting; budgets if applicable]
- Asset Checklist: [hero image specs, copy lengths, CTA variants]
- Governance/Media Brief Notes: [as needed]

TIMELINE & CADENCE
- Pre‑launch (Week −2 to −1): [teasers, waitlist/email capture]
- Launch (Week 0): [PDP live, announce email, paid burst]
- Sustain (Weeks 1–4): [social cadence, remarketing, partner posts]
- Email/Social Calendar: [send/post rhythm; themes]

SECTION E — MEASUREMENT & OPTIMIZATION
- KPIs by Stage: [reach, CTR, CVR, AOV, ROAS]
- UTM Scheme: [naming pattern]
- A/B Matrix: [headline × image × offer]

SECTION F — RISKS & DEPENDENCIES
- Licensing/Releases: [notes]
- Edition/Inventory Constraints: [notes]
- Packaging/Shipping SLAs: [notes]

APPENDIX — COPY SNIPPETS
- PDP bullets: [3–5]
- Social captions (TOFU/MOFU/BOFU): [x3]
- Retargeting ad copy: [1–2 lines]";

        public ExecutorBinding[] GetResearchNodes(string medium, WorkflowBuilder builder, ExecutorBinding source)
        {
            var fanInEdges = new List<ExecutorBinding>();
            var fanOutEdges = new List<ExecutorBinding>();

            var mediumResearch = new ResearchAgentExecutor<ResearchResponse>(Guid.NewGuid().ToString(), medium, researchAgent, researchInstructions, mediumResearchPrompt, clientFactory, factory);

            fanOutEdges.Add(mediumResearch);
            builder.BindExecutor(mediumResearch).WithOutputFrom(mediumResearch);

            var priceResearch = new ResearchAgentExecutor<PriceResponse>(Guid.NewGuid().ToString(), medium, researchAgent, researchInstructions, priceResearchPrompt, clientFactory, factory);

            fanInEdges.Add(priceResearch);
            builder.BindExecutor(priceResearch).WithOutputFrom(priceResearch);
            builder.AddEdge(mediumResearch, priceResearch);

            var productResearch = new ResearchAgentExecutor<ProductResearchResponse>(Guid.NewGuid().ToString(), medium, marketingAgent, marketingInstructions, productSalesPagePrompt, clientFactory, factory);

            fanOutEdges.Add(productResearch);
            builder.BindExecutor(productResearch).WithOutputFrom(productResearch);

            var marketCampaign = new ResearchAgentExecutor<MarketingCampaignResponse>(Guid.NewGuid().ToString(), medium, marketingAgent, marketingInstructions, marketingCampaignPrompt, clientFactory, factory);

            fanInEdges.Add(marketCampaign);
            builder.BindExecutor(marketCampaign).WithOutputFrom(marketCampaign);
            builder.AddEdge(productResearch, marketCampaign);

            var socialChannels = new SocialMediaExecutor(Guid.NewGuid().ToString(), medium, clientFactory, factory);
            
            fanInEdges.Add(socialChannels);
            builder.BindExecutor(socialChannels).WithOutputFrom(socialChannels);
            fanOutEdges.Add(socialChannels);

            builder.AddFanOutEdge(source, fanOutEdges);
            return [.. fanInEdges];
        }
    }        
}
