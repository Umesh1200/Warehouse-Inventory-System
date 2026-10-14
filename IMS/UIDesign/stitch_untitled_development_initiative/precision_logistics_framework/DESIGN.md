---
name: Precision Logistics Framework
colors:
  surface: '#fbf8fc'
  surface-dim: '#dbd9dc'
  surface-bright: '#fbf8fc'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f5f3f6'
  surface-container: '#efedf0'
  surface-container-high: '#e9e7eb'
  surface-container-highest: '#e4e2e5'
  on-surface: '#1b1b1e'
  on-surface-variant: '#44474e'
  inverse-surface: '#303033'
  inverse-on-surface: '#f2f0f3'
  outline: '#75777f'
  outline-variant: '#c5c6cf'
  surface-tint: '#4e5e81'
  primary: '#031635'
  on-primary: '#ffffff'
  primary-container: '#1a2b4b'
  on-primary-container: '#8293b8'
  inverse-primary: '#b6c6ef'
  secondary: '#0058be'
  on-secondary: '#ffffff'
  secondary-container: '#2170e4'
  on-secondary-container: '#fefcff'
  tertiary: '#141819'
  on-tertiary: '#ffffff'
  tertiary-container: '#292c2e'
  on-tertiary-container: '#909395'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#d8e2ff'
  primary-fixed-dim: '#b6c6ef'
  on-primary-fixed: '#081b3a'
  on-primary-fixed-variant: '#364768'
  secondary-fixed: '#d8e2ff'
  secondary-fixed-dim: '#adc6ff'
  on-secondary-fixed: '#001a42'
  on-secondary-fixed-variant: '#004395'
  tertiary-fixed: '#e0e3e5'
  tertiary-fixed-dim: '#c4c7c9'
  on-tertiary-fixed: '#191c1e'
  on-tertiary-fixed-variant: '#444749'
  background: '#fbf8fc'
  on-background: '#1b1b1e'
  surface-variant: '#e4e2e5'
  status-success: '#10B981'
  status-warning: '#F59E0B'
  status-error: '#EF4444'
  status-info: '#6366F1'
  sidebar-bg: '#0F172A'
  surface-border: '#E2E8F0'
  data-fill: '#DBEAFE'
typography:
  headline-lg:
    fontFamily: Inter
    fontSize: 24px
    fontWeight: '600'
    lineHeight: 32px
  headline-md:
    fontFamily: Inter
    fontSize: 18px
    fontWeight: '600'
    lineHeight: 24px
  body-md:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 20px
  body-sm:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '400'
    lineHeight: 16px
  label-mono:
    fontFamily: JetBrains Mono
    fontSize: 12px
    fontWeight: '500'
    lineHeight: 16px
    letterSpacing: 0.02em
  table-header:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '600'
    lineHeight: 16px
    letterSpacing: 0.05em
rounded:
  sm: 0.125rem
  DEFAULT: 0.25rem
  md: 0.375rem
  lg: 0.5rem
  xl: 0.75rem
  full: 9999px
spacing:
  sidebar-width: 260px
  gutter: 1.5rem
  container-padding: 2rem
  stack-sm: 0.5rem
  stack-md: 1rem
  table-cell-padding: 0.75rem 1rem
---

## Brand & Style

The design system is engineered for high-consequence operational environments where data density and accuracy are paramount. The brand personality is **Technical, Utilitarian, and Authoritative**, designed to instill confidence in logistics managers and warehouse staff. 

The visual style follows a **Corporate / Modern** aesthetic with a strong emphasis on functional hierarchy. It utilizes a deep navy sidebar to anchor the navigation, providing a sophisticated professional foundation. Surfaces are kept clean with light gray backgrounds to reduce eye strain during prolonged data entry sessions. The emotional response is one of controlled efficiency, where every pixel serves a logical purpose in the auditing and movement of inventory.

## Colors

This design system uses a logic-driven color palette. The **Primary Navy (#1A2B4B)** is reserved for structural navigation and high-level headers, providing the "source of truth" weight. The **Secondary Blue (#3B82F6)** acts as the action color for primary buttons and interactive states.

The **Semantic Status Palette** is critical for the IMS:
- **Success Green**: Indicates "In Stock," "Accepted," or "Completed."
- **Warning Amber**: Used for "Low Stock," "QC Hold," or "Pending Approval."
- **Error Red**: Signals "Out of Stock," "Damaged," or "System Alerts."
- **Data Fill**: A very light blue used to highlight active table rows or specific visualization bins (e.g., Rack Visualization).

## Typography

The system utilizes **Inter** as the primary typeface for its exceptional legibility in data-heavy dashboards and forms. For technical identifiers such as SKUs, Barcodes, and SQL-driven transaction IDs, **JetBrains Mono** is used to ensure character distinction (e.g., distinguishing '0' from 'O').

- **Headlines**: Use a semi-bold weight to anchor sections without overwhelming the data.
- **Data Tables**: Use 12px Inter with 600 weight for headers, always in all-caps with light tracking.
- **Forms**: Input labels use 14px body text, while helper text uses the 12px small variant.

## Layout & Spacing

The layout utilizes a **Fixed Sidebar / Fluid Content** model. The sidebar remains at 260px to ensure the navigation labels are always readable, while the main content area expands to accommodate complex inventory tables and warehouse visualizations.

- **Grid**: A standard 12-column system is used for dashboard widgets, allowing cards to span 3, 4, 6, or 12 columns.
- **Information Density**: The layout prioritizes a "High Density" rhythm. Margins between cards are kept at 1.5rem, but internal component padding is tighter to ensure more data is visible above the fold.
- **Breakpoints**: 
  - **Mobile (<768px)**: Sidebar collapses to an icon-only rail or drawer; cards stack vertically.
  - **Desktop (>1280px)**: Multi-column form layouts (e.g., Supplier Info and Order Details side-by-side) are preferred.

## Elevation & Depth

This design system avoids heavy shadows to maintain a clean, "Audit-First" aesthetic. Depth is achieved through **Tonal Layers** and **Low-Contrast Outlines**.

- **Level 0 (Background)**: The light gray `#F8FAFC` surface.
- **Level 1 (Cards/Containers)**: White background with a 1px `#E2E8F0` border. This is the primary container for all data.
- **Level 2 (Modals/Popovers)**: White background with a subtle ambient shadow (0px 4px 12px rgba(0,0,0,0.05)) to separate temporary interactions from the underlying ledger.
- **Visual Cues**: Active states (like a selected rack in the Warehouse Visualization) use a secondary blue border rather than a shadow to indicate focus.

## Shapes

The shape language is **Soft (0.25rem)**. This provides a modern touch while maintaining the structural rigidity required for a professional enterprise tool.

- **Inputs & Buttons**: 0.25rem (4px) corner radius.
- **Cards**: 0.5rem (8px) corner radius to create a clear visual distinction from smaller UI elements.
- **Status Pills**: Use a fully rounded (pill) shape to differentiate status indicators from interactive buttons.

## Components

### Buttons
- **Primary**: Solid `#3B82F6` with white text. Used for "Create," "Save," and "Submit."
- **Secondary**: Ghost style with `#3B82F6` border and text. Used for "Cancel" or "Draft."
- **Critical**: Solid `#EF4444`. Used for "Delete" or "Reject."
- **Icon Buttons**: Used within tables for "Edit" or "View Details," utilizing `#64748B` neutral icons.

### Tables
- **Header**: Light gray background (`#F1F5F9`) with bold uppercase text.
- **Rows**: 1px bottom border. Hover state should apply a `#F8FAFC` background tint.
- **Numeric Data**: All quantity and currency columns must be right-aligned for easy scanning.

### Input Fields
- **Standard**: 1px border (`#E2E8F0`). Focus state transitions border to primary blue with a subtle 2px glow.
- **Read-Only**: Filled with `#F1F5F9` to indicate system-generated data (e.g., "PO Number").
- **Required Fields**: Indicated by a red asterisk next to the label.

### Cards & Widgets
- **Dashboard Cards**: Must include a title, optional icon, and a consistent padding of 1.5rem.
- **Status Badges**: Small, pill-shaped components with low-opacity background fills matching the semantic color (e.g., Light Green fill with Dark Green text for "In Stock").

### Specialized Components
- **Bin Visualization**: Rectangular nodes representing rack locations. Color-coded based on utilization percentage (Empty = Gray, Full = Blue).
- **Barcode Scanner Input**: A text input field with a trailing barcode icon, visually distinguishing it as a scan-ready field.