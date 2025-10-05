# Settings Dialog Improvements

## Overview
Enhanced the Library Settings dialog to improve usability and provide granular control over rescanning behavior.

## Improvements Made

### 1. Scrollable Interface
- **Problem**: Settings dialog was not large enough to show all options, some items were inaccessible
- **Solution**: 
  - Added `ScrollViewer` with vertical scrolling
  - Increased dialog width from 640px to 800px
  - Set maximum height to 600px with auto-scrolling
  - Added proper margins and spacing

### 2. Per-Folder Rescan Options
- **Problem**: Global "Rescan after saving" applied to all folders, wasting time when only some folders changed
- **Solution**:
  - Added individual "Rescan this folder" checkbox for each library root
  - Global "Rescan all folders" option overrides individual settings when checked
  - Clear explanation of behavior with helpful tooltips

### 3. Improved User Experience
- **Better Layout**: Rescan checkbox moved to header row of each folder for better visibility
- **Helpful Text**: Added explanatory text about when rescanning occurs
- **Tooltips**: Added tooltips to explain checkbox behavior
- **Logical Grouping**: Separated global options from per-folder settings

## Usage

### Individual Folder Rescanning
1. Open Settings (`Ctrl+S`)
2. For each folder, check/uncheck "Rescan this folder" as needed
3. Leave "Rescan all folders after saving" unchecked
4. Click Save
5. Only folders with "Rescan this folder" checked will be rescanned

### Global Rescanning (Previous Behavior)
1. Open Settings (`Ctrl+S`)
2. Check "Rescan all folders after saving"
3. Click Save
4. All folders will be rescanned regardless of individual settings

## Technical Implementation

### Data Structure Changes
- Added `ShouldRescan` property to `LibraryRootItemViewModel`
- Added `RescanRequested` event with `RescanRequestedEventArgs`
- Enhanced `LibrarySettingsViewModel` to track rescan preferences

### Event-Driven Architecture
- Settings dialog emits `RescanRequested` event with details
- MainWindow subscribes to event and triggers appropriate rescan behavior
- Preserves existing rescan logic while adding selective capability

### UI Layout Changes
```xml
<!-- Before -->
<StackPanel>
  <ListView MinHeight="240" />
  <CheckBox Content="Rescan after saving" />
</StackPanel>

<!-- After -->
<ScrollViewer VerticalScrollBarVisibility="Auto">
  <StackPanel Margin="12">
    <ListView MinHeight="240" MaxHeight="300" />
    <StackPanel>
      <TextBlock Text="Global Options" />
      <CheckBox Content="Rescan all folders..." />
      <TextBlock Text="Explanatory note..." />
    </StackPanel>
  </StackPanel>
</ScrollViewer>
```

### Per-Folder UI
Each folder now shows:
- Folder name with "Rescan this folder" checkbox in header
- All existing configuration options (path, priority, etc.)
- Clear visual separation between folders

## Benefits

1. **Time Savings**: Only rescan folders that actually changed
2. **Better Accessibility**: All options now visible and accessible via scrolling
3. **Clearer Intent**: Users understand exactly what will be rescanned
4. **Flexible Control**: Can mix individual and global rescan strategies
5. **Backward Compatible**: Existing behavior preserved when using global option

## Future Enhancements

- Implement selective rescanning at the ingestion service level
- Add progress indicators for individual folder scans
- Allow saving path/priority changes without rescanning
- Add "Smart Rescan" that detects which folders actually changed