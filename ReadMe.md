# Avalonia.Controls.Extras

Reusable Avalonia controls with no dependency on Tripous.

The first control is `GroupGrid`, a general-purpose grid for data-entry and business applications. It is designed as a clean Avalonia control that can later be adapted by external frameworks, but the library itself remains framework-neutral.

## GroupGrid Features

Current implemented or planned feature map for `GroupGrid`.

### Architecture

- Avalonia `Control` with custom rendering.
- Separate non-visual `GroupGridEngine`.
- Framework-neutral core, with no Tripous dependency.
- Adapter-based data access through `IGroupGridDataAdapter`.
- Convenience `ItemsSource` support for POCO/list sources.
- Optional auto-generation of columns from public POCO properties.
- Planned `ItemsSource` support for `DataTable` and `DataView`.

### Columns

- Base `GroupGridColumn` descriptor.
- Specialized column types:
  - `GroupGridTextColumn`
  - `GroupGridNumberColumn`
  - `GroupGridDateColumn`
  - `GroupGridCheckBoxColumn`
- Column width and minimum width.
- Column visibility.
- Column display formatting.
- Column horizontal text alignment.
- User flags for resize, reorder, grouping, and hiding.
- Query methods for all, visible ordered, hidden, and grouped columns.

### Layout

- Toolbar band.
- Group panel.
- Column header band.
- Filter row band.
- Virtual body viewport.
- Footer total summary band.
- Vertical scrollbar.
- Horizontal scrollbar.

### Grouping

- Always root-node based projection.
- Group columns kept as an ordered list of column references.
- Group header rows.
- Group summary rows.
- Expand/collapse per group node.
- Collapsed groups show summary values in the group header.
- Header drag to group panel groups a column.
- Header drag outside the grid hides the column.
- Group panel drag supports grouped-column reorder.
- Grouped-column drag back to header band ungroups and inserts in column order.
- Grouped-column drag outside the grid removes it from grouping.

### Rows And Summaries

- Data rows.
- Group header rows.
- Group summary rows.
- Footer total summary row.
- Summary aggregate kinds:
  - Count
  - Sum
  - Min
  - Max
  - Average

### Interaction

- Single current cell.
- Single selected cell/row.
- Keyboard navigation.
- Mouse selection.
- Group expand/collapse by mouse.
- Column resize by dragging header edge.
- Column reorder by dragging headers.
- Floating header ghost while dragging columns.
- Drop insertion guide for column drag/drop.
- Vertical and horizontal scroll thumb dragging.
- Scrollbar track page scrolling.

### Editing

- Edit state tracking.
- Begin edit.
- Commit edit.
- Cancel edit.
- Validation and commit events.

### Hit Testing

- Hit testing returns band/kind information.
- Cell hit results include direct `GroupGridColumn` references.
- Hit testing supports body cells, group expanders, headers, resize handles, group panel, footer summary, and scroll-aware coordinates.

## GroupGrid Roadmap

- Column management dialog:
  - show/hide columns
  - reorder columns
  - manage grouped columns
  - restore/reset column layout
- `DataTable` and `DataView` support through `ItemsSource`.
- Sorting.
- Filtering.
- Toolbar commands.
- Column context menu.
- Checkbox click toggle.
- In-place editor controls.
- Export:
  - CSV
  - JSON
  - Excel/OpenXML
  - clipboard copy
  - printable/report-friendly output
- Import or load layout settings.
- Save/restore user layout:
  - column order
  - column widths
  - hidden columns
  - grouped columns
  - sorting
  - filtering
  - summary settings
