using System;
using System.Collections.Generic;
using System.Reflection;

namespace Sol
{
    /// <summary>
    /// Looks up the role of a stat. The reflection read happens once per stat and is cached, so it is cheap to call from
    /// the stats screen and from the write checks.
    /// </summary>
    public static class StatRoles
    {
        private static readonly Dictionary<StatTypeEnum, StatRole> Cache = new();

        /// <summary>The role of a stat. A stat with no [Stat] attribute is treated as Tuning, so a missing role is
        /// never settable by accident.</summary>
        public static StatRole RoleOf(StatTypeEnum stat)
        {
            if (Cache.TryGetValue(stat, out StatRole role)) return role;

            FieldInfo field = typeof(StatTypeEnum).GetField(stat.ToString(), BindingFlags.Public | BindingFlags.Static);
            var attribute = field?.GetCustomAttribute<StatAttribute>();
            role = attribute != null ? attribute.Role : StatRole.Tuning;

            Cache[stat] = role;
            return role;
        }

        public static bool IsAttribute(StatTypeEnum stat) => RoleOf(stat) == StatRole.Attribute;

        public static bool IsDerived(StatTypeEnum stat) => RoleOf(stat) == StatRole.Derived;

        /// <summary>True for stats that appear on the stats screen. Tuning stats never do.</summary>
        public static bool IsVisible(StatTypeEnum stat) => RoleOf(stat) != StatRole.Tuning;
    }
}
