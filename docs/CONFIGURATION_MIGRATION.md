# Configuration Migration Guide

## Migrating from Previous Versions

If you're upgrading ArtistTool and have existing configuration files, follow this guide to update your settings.

## What Changed

### New Configuration Option

A new `UseWorkflows` option has been added to enable AI agent workflows:

```json
{
  "Options": {
    "UseAI": false,
    "MarketingMode": false,
    "UseWorkflows": false  // ? NEW
  }
}
```

### New Project Reference

The main ArtistTool project now references `ArtistTool.Workflows`:

```xml
<ProjectReference Include="..\ArtistTool.Workflows\ArtistTool.Workflows.csproj" />
```

## Migration Steps

### Step 1: Update appsettings.json

Add the `UseWorkflows` property to your `Options` section:

**Before:**
```json
{
  "Options": {
    "UseAI": true,
    "MarketingMode": true
  }
}
```

**After:**
```json
{
  "Options": {
    "UseAI": true,
    "MarketingMode": true,
    "UseWorkflows": false
  }
}
```

### Step 2: Update appsettings.Development.json

Similarly, update your development settings:

**Before:**
```json
{
  "Options": {
    "UseAI": false,
    "MarketingMode": true
  }
}
```

**After:**
```json
{
  "Options": {
    "UseAI": false,
    "MarketingMode": true,
    "UseWorkflows": false
  }
}
```

### Step 3: Rebuild the Solution

```bash
dotnet build
```

## Backwards Compatibility

? **Fully Backwards Compatible**

- If `UseWorkflows` is not specified, it defaults to `false`
- Existing functionality works exactly as before
- No breaking changes to existing features

## Default Values

| Option | Default Value | Fallback Behavior |
|--------|--------------|-------------------|
| `UseAI` | `false` | Application runs in basic mode |
| `MarketingMode` | `false` | Marketing button hidden |
| `UseWorkflows` | `false` | Workflows not initialized |

## Common Migration Scenarios

### Scenario 1: Using Basic Mode

**Current Config:**
```json
{
  "Options": {
    "UseAI": false
  }
}
```

**Updated Config:**
```json
{
  "Options": {
    "UseAI": false,
    "MarketingMode": false,
    "UseWorkflows": false
  }
}
```

**Impact**: None - application continues to work as before

### Scenario 2: Using AI Features

**Current Config:**
```json
{
  "Options": {
    "UseAI": true,
    "MarketingMode": true
  }
}
```

**Updated Config:**
```json
{
  "Options": {
    "UseAI": true,
    "MarketingMode": true,
    "UseWorkflows": false
  }
}
```

**Impact**: None - AI features work as before, workflows not enabled

### Scenario 3: Enabling Workflows (New)

**Updated Config:**
```json
{
  "Options": {
    "UseAI": true,
    "MarketingMode": true,
    "UseWorkflows": true
  }
}
```

**Requirements:**
- `Agents.md` file must exist in the application directory
- Azure OpenAI must be configured
- Both conversational and vision models required

**Impact**: Enables new workflow features for photo critique

## Validation Checklist

After migration, verify:

- [ ] Application builds successfully
- [ ] Application starts without errors
- [ ] Existing features work as expected
- [ ] New workflows initialize (if enabled)
- [ ] Configuration options are correctly applied

## Troubleshooting

### Missing UseWorkflows Key

**Symptom**: Configuration warning or default value used

**Solution**: Add the key to all relevant `appsettings.*.json` files

### Project Reference Error

**Symptom**: `The type or namespace name 'Workflows' does not exist`

**Solution**: 
```bash
dotnet add ArtistTool/ArtistTool.csproj reference ArtistTool.Workflows/ArtistTool.Workflows.csproj
```

### Workflow Initialization Failed

**Symptom**: `Agents configuration file not found`

**Solution**: Ensure `Agents.md` has `CopyToOutputDirectory` set to `Always`

## Rollback Instructions

If you need to revert to the previous version:

1. Remove `UseWorkflows` from configuration files
2. Remove the project reference to `ArtistTool.Workflows`
3. Remove `using ArtistTool.Workflows;` from `Program.cs`
4. Remove workflow-related code from `Program.cs`

## Getting Help

If you encounter issues during migration:

1. Check the [AI Configuration Guide](AI_CONFIGURATION.md)
2. Review [Workflow Documentation](../ArtistTool.Workflows/README.md)
3. Check application logs for specific error messages
4. Verify Azure OpenAI configuration (if using AI features)

## Version History

| Version | Changes |
|---------|---------|
| v1.0 | Initial release with `UseAI` |
| v1.1 | Added `MarketingMode` |
| v1.2 | Added `UseWorkflows` (current) |
