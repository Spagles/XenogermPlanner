using System;
using System.Collections.Generic;

namespace XenogermPlanner.UI
{
    internal enum XenogermPlanSourceListRowKind
    {
        Group,
        Source
    }

    internal readonly struct XenogermPlanSourceListRow
    {
        private XenogermPlanSourceListRow(
            XenogermPlanSourceListRowKind kind,
            XenogermPlanSourceGroup group,
            XenogermPlanSourceEntry source,
            bool isGroupExpanded)
        {
            Kind = kind;
            Group = group;
            Source = source;
            IsGroupExpanded = isGroupExpanded;
        }

        internal XenogermPlanSourceListRowKind Kind { get; }
        internal XenogermPlanSourceGroup Group { get; }
        internal XenogermPlanSourceEntry Source { get; }
        internal bool IsGroupExpanded { get; }

        internal static XenogermPlanSourceListRow CreateGroup(XenogermPlanSourceGroup group, bool isExpanded)
        {
            if (group == null)
                throw new ArgumentNullException(nameof(group));

            return new XenogermPlanSourceListRow(XenogermPlanSourceListRowKind.Group, group, null, isExpanded);
        }

        internal static XenogermPlanSourceListRow CreateSource(
            XenogermPlanSourceGroup group,
            XenogermPlanSourceEntry source)
        {
            if (group == null)
                throw new ArgumentNullException(nameof(group));

            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return new XenogermPlanSourceListRow(
                XenogermPlanSourceListRowKind.Source,
                group,
                source,
                isGroupExpanded: false);
        }
    }

    internal static class XenogermPlanSourceListProjection
    {
        internal static List<XenogermPlanSourceListRow> Build(
            IReadOnlyList<XenogermPlanSourceGroup> groups,
            ISet<string> collapsedGroupKeys)
        {
            if (groups == null)
                throw new ArgumentNullException(nameof(groups));

            if (collapsedGroupKeys == null)
                throw new ArgumentNullException(nameof(collapsedGroupKeys));

            var rows = new List<XenogermPlanSourceListRow>();

            foreach (XenogermPlanSourceGroup group in groups)
            {
                if (group == null)
                    throw new ArgumentException("Source group collection cannot contain null entries.", nameof(groups));

                bool expanded = !group.IsCollapsible || !collapsedGroupKeys.Contains(group.Key);

                if (group.IsCollapsible)
                    rows.Add(XenogermPlanSourceListRow.CreateGroup(group, expanded));

                if (!expanded)
                    continue;

                foreach (XenogermPlanSourceEntry source in group.Sources)
                    rows.Add(XenogermPlanSourceListRow.CreateSource(group, source));
            }

            return rows;
        }
    }
}