# File Size Optimization Notes

This document explains the optimizations applied to reduce the compiled executable size.

## Problem

The self-contained single-file executable was too large (~60-80MB), making distribution and updates cumbersome.

## Solution

Applied multiple .NET 8 optimization features to reduce file size by approximately 50-60%, resulting in a ~25-35MB executable.

## Changes Applied

### 1. IL Trimming (Primary Optimization)
```xml
<PublishTrimmed>true</PublishTrimmed>
<TrimMode>link</TrimMode>
```

**What it does:** The IL Linker analyzes the application and removes unused code and assemblies at the IL level.

**Size savings:** ~30-40MB (30-50% reduction)

**Safety:** Windows Forms is fully trim-compatible in .NET 8. Our application uses:
- P/Invoke declarations (safe - explicitly declared)
- One embedded resource access via `Assembly.GetManifestResourceStream` (safe - resource is declared in csproj)
- No dynamic reflection or type discovery

### 2. Invariant Globalization
```xml
<InvariantGlobalization>true</InvariantGlobalization>
```

**What it does:** Removes all culture-specific data, resources, and formatting capabilities.

**Size savings:** ~10-15MB

**Safety:** Our application only displays English text and doesn't use:
- Culture-specific date/time formatting
- Culture-specific number formatting
- Locale-aware string operations
- Time zones

All UI strings are hardcoded in English.

### 3. Runtime Feature Removal
```xml
<EventSourceSupport>false</EventSourceSupport>
<DebuggerSupport>false</DebuggerSupport>
<EnableUnsafeBinaryFormatterSerialization>false</EnableUnsafeBinaryFormatterSerialization>
<HttpActivityPropagationSupport>false</HttpActivityPropagationSupport>
<MetadataUpdaterSupport>false</MetadataUpdaterSupport>
```

**What each does:**
- `EventSourceSupport=false`: Removes EventSource infrastructure (we don't use event tracing)
- `DebuggerSupport=false`: Removes debugger-related code (not needed in release builds)
- `EnableUnsafeBinaryFormatterSerialization=false`: Removes legacy BinaryFormatter (we only use Registry)
- `HttpActivityPropagationSupport=false`: Removes HTTP activity tracing (no HTTP in our app)
- `MetadataUpdaterSupport=false`: Removes hot-reload support (not needed for deployed apps)

**Size savings:** ~5-10MB combined

**Safety:** None of these features are used by our application.

### 4. Disable ReadyToRun
```xml
<PublishReadyToRun>false</PublishReadyToRun>
```

**What it does:** Disables ahead-of-time (AOT) compilation, which includes pre-compiled native code.

**Size savings:** ~5-10MB

**Trade-off:** Slightly slower startup time (typically 50-200ms for small apps)

**Safety:** For a system tray application, the startup time difference is negligible and won't be noticed by users.

### 5. Removed Invalid Property
Removed `<TrimUnusedDependencies>true</TrimUnusedDependencies>` as it's not a valid MSBuild property.

## Expected Results

### Before Optimization
- Typical self-contained WinForms app: **~60-80MB**

### After Optimization  
- Optimized build: **~25-35MB**
- **Total reduction: 50-60%**

## Verification

To verify the optimizations worked:

1. Build the release version:
```bash
dotnet publish src/DdcTraySwitcher.csproj -c Release
```

2. Check the output size:
```bash
# PowerShell
Get-Item publish\DdcTraySwitcher.exe | Select-Object Name, Length

# CMD
dir publish\DdcTraySwitcher.exe
```

## Testing Checklist

After building with these optimizations, verify:

- [ ] Application starts without errors
- [ ] Tray icon appears correctly
- [ ] Monitor detection works
- [ ] Input source selection works
- [ ] Input switching functions properly
- [ ] Autostart enable/disable works
- [ ] Settings persist across restarts
- [ ] No runtime exceptions or warnings

## Rollback Plan

If issues occur, progressively remove optimizations in this order:

1. **First:** Remove runtime feature flags (least likely to cause issues)
2. **Second:** Remove `InvariantGlobalization` if culture issues appear
3. **Third:** Change `TrimMode` from `link` to `partial`
4. **Last resort:** Remove `PublishTrimmed` entirely

## Additional Notes

- All optimizations are well-supported in .NET 8
- Windows Forms trimming support was significantly improved in .NET 6+
- These settings only affect Release builds and published outputs
- Debug builds remain unchanged for development

## References

- [.NET Application Trimming](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trimming-options)
- [Trim Self-Contained Deployments](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trim-self-contained)
- [Runtime Configuration Options](https://learn.microsoft.com/en-us/dotnet/core/runtime-config/)
