// GPL-3.0-only. Test the sizing policy without claiming native IMGUI rendering coverage.
using System;
using AircraftAutopilotContinued;
static class WindowSizingTests
{
    internal static void Run(Action<bool,string> check)
    {
        var sizing = new WindowLayout();
        float initialCollapsed = sizing.BeginLayout(false);
        var compact = WindowLayout.Fit(1512,982,initialCollapsed,32);
        check(!compact.Scroll && compact.Width==460,"desktop compact window does not scroll");
        sizing.ObserveContentHeight(false,240);
        compact=WindowLayout.Fit(1512,982,sizing.BeginLayout(false),32);
        check(compact.Height==272,"compact height fits measured content without blank estimate");
        var expanded=WindowLayout.Fit(1512,982,sizing.BeginLayout(true),32);
        check(!expanded.Scroll&&expanded.Height>compact.Height,"opening PID expands without desktop scrolling");
        sizing.ObserveContentHeight(true,560.2f);
        expanded=WindowLayout.Fit(1512,982,sizing.BeginLayout(true),32);
        check(expanded.Height==593&&!expanded.Scroll,"measured PID and diagnostics fit desktop");
        for(int i=0;i<5;i++) {
            sizing.ObserveContentHeight(true,560.2f);
            var stable=WindowLayout.Fit(1512,982,sizing.BeginLayout(true),32);
            check(stable.Height==expanded.Height&&!stable.Scroll,"stable repeated measurement without height oscillation");
        }
        sizing.BeginLayout(false);sizing.ObserveContentHeight(false,240);
        var closed=WindowLayout.Fit(1512,982,sizing.BeginLayout(false),32);
        check(closed.Height==compact.Height&&!closed.Scroll,"Hide PID restores compact measured height");
        sizing.ObserveContentHeight(true,560); // A measurement from a prior panel state must not grow the collapsed view.
        check(sizing.BeginLayout(false)==240,"discard stale expanded measurement");
        sizing.ObserveContentHeight(false,float.NaN);check(sizing.BeginLayout(false)==240,"ignore invalid measurement");
        var small=WindowLayout.Fit(640,400,560,32);
        check(small.Scroll&&small.Height==384&&small.ViewportHeight==352,"short screen uses bounded scrolling fallback");
        var restored=WindowLayout.Fit(1512,982,560,32);
        check(!restored.Scroll&&restored.Height==592,"larger screen removes unnecessary scrollbar");
        var narrow=WindowLayout.Fit(360,700,560,32);
        check(narrow.Scroll&&narrow.Width==344,"narrow screen fits bounds with horizontal fallback");
        check(!WindowLayout.Fit(640,400,352,32).Scroll,"exact fit has no unnecessary scrollbar");
        check(WindowLayout.ClampPosition(180,460,1512)==180,"preserve valid horizontal coordinate");
        check(WindowLayout.ClampPosition(100,592,982)==100,"preserve valid vertical coordinate");
        check(WindowLayout.ClampPosition(900,592,982)==382,"move expanded window inside bottom screen edge");
        check(WindowLayout.ClampPosition(-20,460,1512)==8,"keep title inside left screen edge");
        check(new WindowLayout().BeginLayout(true)>new WindowLayout().BeginLayout(false),"restored showpid state starts expanded");
        check(small.Height+2*WindowLayout.ScreenMargin<=400,"fallback fits full window including title");
    }
}
