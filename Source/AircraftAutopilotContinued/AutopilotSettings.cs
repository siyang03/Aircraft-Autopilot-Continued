// GPL-3.0-only.
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace AircraftAutopilotContinued
{
    internal static class AutopilotSettings
    {
        static string PathName => Path.Combine(KSPUtil.ApplicationRootPath, "GameData/AircraftAutopilotContinued/PluginData/settings.cfg");
        static bool IsSetting(FieldInfo f) => f.FieldType == typeof(EditableDouble) || f.FieldType == typeof(double) &&
            (f.Name.EndsWith("Target") && !f.Name.StartsWith("Real") || f.Name == "BankAngle");
        public static void Load(AircraftAutopilotController c)
        {
            try {
                if (!File.Exists(PathName)) return;
                ConfigNode n = ConfigNode.Load(PathName);
                foreach (FieldInfo f in c.GetType().GetFields()) {
                    double x;
                    if (!IsSetting(f) || !double.TryParse(n.GetValue(f.Name), NumberStyles.Float, CultureInfo.InvariantCulture, out x) || !Maths.Finite(x)) continue;
                    if (f.FieldType == typeof(EditableDouble)) ((EditableDouble)f.GetValue(c)).Val = x;
                    else f.SetValue(c, x);
                }
            } catch (Exception e) { Debug.LogError("[AAC] Settings load: " + e); }
        }
        public static void LoadWindow(AircraftAutopilotAddon host)
        {
            try {
                if (!File.Exists(PathName)) return;
                ConfigNode n = ConfigNode.Load(PathName); float x, y;
                if (float.TryParse(n.GetValue("WindowX"), NumberStyles.Float, CultureInfo.InvariantCulture, out x) && Maths.Finite(x)) host.WindowRect.x = x;
                if (float.TryParse(n.GetValue("WindowY"), NumberStyles.Float, CultureInfo.InvariantCulture, out y) && Maths.Finite(y)) host.WindowRect.y = y;
                host.Window.showpid = n.GetValue("ShowPID") == "True";
            } catch (Exception e) { Debug.LogError("[AAC] Window settings load: " + e); }
        }
        public static void Save(AircraftAutopilotAddon host)
        {
            if (host.Controller == null || host.Window == null) return;
            try {
                ConfigNode n = new ConfigNode("AIRCRAFT_AUTOPILOT_CONTINUED");
                foreach (FieldInfo f in host.Controller.GetType().GetFields()) if (IsSetting(f)) {
                    object o = f.GetValue(host.Controller);
                    double x = o is EditableDouble ? ((EditableDouble)o).Val : (double)o;
                    n.AddValue(f.Name, x.ToString("R", CultureInfo.InvariantCulture));
                }
                n.AddValue("WindowX", host.WindowRect.x.ToString(CultureInfo.InvariantCulture));
                n.AddValue("WindowY", host.WindowRect.y.ToString(CultureInfo.InvariantCulture));
                n.AddValue("ShowPID", host.Window.showpid);
                Directory.CreateDirectory(Path.GetDirectoryName(PathName)); n.Save(PathName);
            } catch (Exception e) { Debug.LogError("[AAC] Settings save: " + e); }
        }
    }
}
