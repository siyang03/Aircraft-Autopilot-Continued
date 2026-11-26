// GPL-3.0-only. Optional-mod ownership inspection; no MechJeb/Kramax references.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
namespace AircraftAutopilotContinued
{
    // Discovery is refreshed periodically by the host; live flags are checked each control tick.
    internal sealed class ConflictDetector
    {
        readonly List<object> mechJeb = new List<object>();
        readonly List<object> george = new List<object>();
        readonly HashSet<string> reportedHooks = new HashSet<string>();
        readonly Dictionary<Type, Dictionary<string, MemberInfo>> members = new Dictionary<Type, Dictionary<string, MemberInfo>>();
        readonly Dictionary<Type, MethodInfo> moduleGetters = new Dictionary<Type, MethodInfo>();
        readonly Action<string> diagnostic;
        internal ConflictDetector(Action<string> diagnostic) { this.diagnostic = diagnostic; }

        // Existing public-field/property reflection helper, with metadata cached per type/name.
        object Member(object obj, string name)
        {
            if (obj == null) return null;
            Type t = obj.GetType();
            Dictionary<string, MemberInfo> map;
            if (!members.TryGetValue(t, out map)) { map = new Dictionary<string, MemberInfo>(); members.Add(t, map); }
            MemberInfo member;
            if (!map.TryGetValue(name, out member)) {
                member = (MemberInfo)t.GetField(name, BindingFlags.Instance | BindingFlags.Public) ??
                    t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
                map.Add(name, member);
            }
            FieldInfo field = member as FieldInfo;
            if (field != null) return field.GetValue(obj);
            PropertyInfo property = member as PropertyInfo;
            return property == null ? null : property.GetValue(obj, null);
        }
        bool Enabled(object obj) => obj != null && Equals(Member(obj, "Enabled"), true);
        static bool HasType(object obj, string fullName)
        {
            for (Type t = obj == null ? null : obj.GetType(); t != null; t = t.BaseType)
                if (t.FullName == fullName) return true;
            return false;
        }
        internal void Refresh(IEnumerable<object> modules, IEnumerable<Delegate> callbacks)
        {
            mechJeb.Clear(); george.Clear();
            foreach (object module in modules)
                if (HasType(module, "MuMech.MechJebCore")) mechJeb.Add(module);
            foreach (Delegate callback in callbacks) {
                if (HasType(callback.Target, "Kramax.George")) {
                    if (!george.Contains(callback.Target)) george.Add(callback.Target);
                    continue;
                }
                string type = callback.Method.DeclaringType == null ? "<unknown>" : callback.Method.DeclaringType.FullName;
                if (type == "AircraftAutopilotContinued.AircraftAutopilotAddon" ||
                    HasType(callback.Target, "MuMech.MechJebCore") || type == "MuMech.MechJebCore" ||
                    type.StartsWith("FerramAerospaceResearch.", StringComparison.Ordinal)) continue;
                if (reportedHooks.Add(type) && diagnostic != null)
                    diagnostic("Flight-control hook registered: " + type +
                        ". Registration alone does not establish active control; keep other autopilots OFF.");
            }
        }
        internal string Check(bool sas)
        {
            if (sas) return "SAS is ON: disable SAS before using Auto.";
            foreach (object module in mechJeb) {
                try {
                    if (Enabled(Member(module, "Attitude"))) return "MechJeb attitude control is active. Disengage it first.";
                    Type t = module.GetType(); MethodInfo get;
                    if (!moduleGetters.TryGetValue(t, out get)) {
                        get = t.GetMethod("GetComputerModule", new Type[] { typeof(string) });
                        moduleGetters.Add(t, get);
                    }
                    if (get == null) return "Cannot inspect MechJeb aircraft ownership: GetComputerModule(string) is unavailable.";
                    if (Enabled(get.Invoke(module, new object[] { "MechJebModuleAirplaneAutopilot" })))
                        return "MechJeb aircraft autopilot is active. Disengage it first.";
                    string throttle = CheckThrust(Member(module, "Thrust"));
                    if (throttle != null) return throttle;
                } catch (Exception) { return "Cannot inspect MechJeb control ownership: an ownership accessor failed."; }
            }
            foreach (object controller in george) {
                try {
                    object h = Member(controller, "HrztActive"), v = Member(controller, "VertActive"), t = Member(controller, "ThrtActive");
                    if (Equals(h, true) || Equals(v, true) || Equals(t, true))
                        return "Kramax flight control is active. Disengage its horizontal, vertical and throttle modes first.";
                    if (!(h is bool) || !(v is bool) || !(t is bool))
                        return "Cannot inspect Kramax ownership: HrztActive, VertActive or ThrtActive is unavailable.";
                } catch (Exception) { return "Cannot inspect Kramax control ownership: an activation accessor failed."; }
            }
            return null;
        }
        internal string CheckThrust(object thrust)
        {
            if (thrust == null) return "Cannot inspect MechJeb throttle ownership: Thrust is unavailable.";
            if (!(Member(thrust, "Enabled") is bool)) return "Cannot inspect MechJeb throttle ownership: Thrust.Enabled is unavailable.";
            ICollection users = Member(thrust, "Users") as ICollection;
            if (users == null) return "Cannot inspect MechJeb throttle ownership: Thrust.Users.Count is unavailable.";
            if (users.Count > 1) return "MechJeb throttle control has an additional user. Disengage it first.";
            object mode = Member(thrust, "Tmode");
            if (!(mode is Enum)) return "Cannot inspect MechJeb throttle ownership: Thrust.Tmode is unavailable or unrecognized.";
            if (!string.Equals(Enum.GetName(mode.GetType(), mode), "OFF", StringComparison.OrdinalIgnoreCase))
                return "MechJeb throttle control mode is not OFF. Disengage it first.";
            // Enabled with its sole self-user and OFF mode is the normal idle state.
            return null;
        }
    }
}
