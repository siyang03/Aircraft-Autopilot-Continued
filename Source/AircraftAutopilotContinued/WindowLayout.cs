// GPL-3.0-only. IMGUI-independent sizing policy; measured content, no persisted dimensions.
using System;
namespace AircraftAutopilotContinued
{
    internal sealed class WindowLayout
    {
        internal const float PreferredWidth = 460;
        internal const float ScreenMargin = 8;
        // First-layout hints only. Repaint measurements replace these on the next Layout event.
        const float CollapsedHint = 270, ExpandedHint = 550;
        float contentHeight, pendingHeight;
        bool initialized, expanded, hasMeasurement, pendingExpanded;
        internal void ObserveContentHeight(bool showPID, float height)
        {
            if (!Maths.Finite(height) || height <= 0) return;
            pendingHeight = (float)Math.Ceiling(height);
            pendingExpanded = showPID;
            hasMeasurement = true;
        }
        internal float BeginLayout(bool showPID)
        {
            if (!initialized || expanded != showPID) {
                contentHeight = showPID ? ExpandedHint : CollapsedHint;
                initialized = true; expanded = showPID;
            }
            if (hasMeasurement && pendingExpanded == showPID) contentHeight = pendingHeight;
            hasMeasurement = false;
            return contentHeight;
        }
        internal struct Plan
        {
            internal float Width, Height, ViewportHeight;
            internal bool Scroll;
        }
        internal static Plan Fit(float screenWidth, float screenHeight, float naturalContentHeight, float chromeHeight)
        {
            float width = Math.Max(1, Math.Min(PreferredWidth, screenWidth - 2 * ScreenMargin));
            float available = Math.Max(1, screenHeight - 2 * ScreenMargin);
            float height = Math.Max(1, Math.Min(naturalContentHeight + chromeHeight, available));
            return new Plan { Width = width, Height = height, ViewportHeight = Math.Max(1, height - chromeHeight),
                Scroll = naturalContentHeight + chromeHeight > available || width < PreferredWidth };
        }
        internal static float ClampPosition(float position, float extent, float screenExtent)
        {
            float margin = Math.Min(ScreenMargin, Math.Max(0, (screenExtent - extent) / 2));
            return (float)Maths.Clamp(position, margin, Math.Max(margin, screenExtent - extent - margin));
        }
    }
}
