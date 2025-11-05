var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.ArtistTool>("artisttool");

builder.Build().Run();
