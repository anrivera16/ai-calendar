# UI/UX Redesign: Sleek & Modern AI Calendar

## 1. Current UI Critique
The current UI is functional but feels a bit dated due to:
- **Heavy Gradients:** The use of `#667eea` to `#764ba2` gradients on headers makes the app feel compartmentalized and heavy.
- **Disjointed Layout:** The chat and calendar are two separate boxes with their own distinct headers, lacking a unified "app" feel.
- **Chat Bubbles:** The chat bubbles use strong background colors which can be visually overwhelming.
- **Calendar Grid:** The calendar uses full-cell background colors for selection and today's date, which feels heavy.

## 2. The "Sleek" Vision (Linear / Vercel Aesthetic)
We will move to a highly polished, minimalist design that focuses on content and typography, reducing visual noise.

### Color Palette
- **Background:** `#F9FAFB` (Very light gray) for the app background.
- **Surfaces:** `#FFFFFF` (Pure white) for panels, with subtle `1px solid #E5E7EB` borders instead of heavy drop shadows.
- **Primary Text:** `#111827` (Near black) for high contrast and readability.
- **Secondary Text:** `#6B7280` (Medium gray) for timestamps, hints, and less important info.
- **Accent Color:** `#000000` (Black) or a sleek `#2563EB` (Modern Blue) for primary actions.

### Typography
- Use a clean system font stack (Inter, San Francisco, Segoe UI).
- Increase contrast in font weights (e.g., `600` for headers, `400` for body).

## 3. Layout Redesign

### The App Shell
Instead of a floating dashboard container, the app will take up the full viewport with a seamless split view.
- **Top Navigation:** A very minimal, borderless header. Just the app name (sleek typography, no emojis) and user profile/actions on the right.
- **Left Panel (Calendar - 60%):** The calendar becomes the main canvas. It feels open and integrated into the background.
- **Right Panel (Chat - 40%):** The chat acts as a sidebar assistant. It has a white background, separated from the calendar by a single subtle vertical border.

### Calendar Refinements
- **Header:** Large, clean typography for the Month/Year (e.g., "February 2026"). Minimalist chevron icons for navigation.
- **Grid:** Remove the gray background and gaps. Use a clean, borderless grid or very subtle 1px lines.
- **Today/Selected State:** Instead of filling the whole cell with color, use a sleek circle behind the date number (e.g., black circle with white text for today, light gray circle for selected).
- **Events:** Clean, rounded pills or subtle dots for events.

### Chat Refinements
- **Header:** Remove the gradient. Just a clean title "Assistant" with a subtle status indicator.
- **Messages:** Move away from traditional "chat bubbles". Use a feed style:
  - **User:** Right-aligned, subtle gray background (`#F3F4F6`), black text.
  - **AI:** Left-aligned, no background, just clean text with a minimalist AI avatar (e.g., a simple sparkle icon ✨).
- **Input:** A floating, pill-shaped input area at the bottom of the chat panel, with a sleek send arrow.

## 4. Implementation Steps

1. **Global Styles & Variables:** Update `styles.scss` with the new color palette, typography, and reset rules.
2. **Dashboard Layout:** Update `dashboard.html` and `dashboard.scss` to use the full-screen, seamless split layout. Remove the gradient header.
3. **Calendar Component:** Redesign `calendar-view.scss` to use the minimalist grid, circular date highlights, and clean typography.
4. **Chat Component:** Redesign `chat-panel.scss` to use the modern feed style, remove gradients, and implement the floating input pill.
5. **Animations:** Add subtle, buttery-smooth transitions (e.g., `transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1)`).
