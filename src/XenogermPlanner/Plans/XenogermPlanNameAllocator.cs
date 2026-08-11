using System;
using System.Collections.Generic;
using System.Globalization;

namespace XenogermPlanner.Plans
{
    internal enum XenogermPlanNameValidationFailure
    {
        None,
        InvalidName,
        NameConflict
    }

    internal static class XenogermPlanNameAllocator
    {
        private static readonly StringComparer _nameComparer = StringComparer.OrdinalIgnoreCase;

        internal static bool TryValidate(
            IEnumerable<XenogermPlan> plans,
            string requestedName,
            string excludedPlanId,
            out string normalizedName,
            out XenogermPlanNameValidationFailure failure)
        {
            if (plans == null)
                throw new ArgumentNullException(nameof(plans));

            if (!TryNormalize(requestedName, out normalizedName))
            {
                failure = XenogermPlanNameValidationFailure.InvalidName;
                return false;
            }

            if (IsAvailable(plans, normalizedName, excludedPlanId))
            {
                failure = XenogermPlanNameValidationFailure.None;
                return true;
            }

            failure = XenogermPlanNameValidationFailure.NameConflict;
            return false;
        }

        internal static string Allocate(
            IEnumerable<XenogermPlan> plans,
            string preferredName,
            string excludedPlanId = null)
        {
            if (plans == null)
                throw new ArgumentNullException(nameof(plans));

            if (!TryNormalize(preferredName, out string normalizedName))
            {
                throw new ArgumentException(
                    "Preferred plan name cannot be null, empty or whitespace.",
                    nameof(preferredName));
            }

            HashSet<string> occupiedNames = CreateOccupiedNames(plans, excludedPlanId);

            if (!occupiedNames.Contains(normalizedName))
                return normalizedName;

            string baseName = normalizedName;
            var nextSuffix = 2;

            if (TrySplitNumericSuffix(normalizedName, out string parsedBaseName, out int parsedSuffix))
            {
                if (parsedSuffix == int.MaxValue)
                {
                    throw new InvalidOperationException(
                        "Cannot allocate a unique plan name because the preferred numeric suffix is too large.");
                }

                baseName = parsedBaseName;
                nextSuffix = parsedSuffix + 1;
            }

            for (int suffix = nextSuffix;; suffix++)
            {
                string candidate = baseName + " " + suffix.ToString(CultureInfo.InvariantCulture);

                if (!occupiedNames.Contains(candidate))
                    return candidate;

                if (suffix == int.MaxValue)
                {
                    throw new InvalidOperationException(
                        "Cannot allocate a unique plan name because all supported numeric suffixes are occupied.");
                }
            }
        }

        private static bool IsAvailable(IEnumerable<XenogermPlan> plans, string normalizedName, string excludedPlanId)
        {
            foreach (XenogermPlan plan in plans)
            {
                if (plan == null || IsExcluded(plan, excludedPlanId))
                    continue;

                if (!TryNormalize(plan.Name, out string existingName))
                    continue;

                if (_nameComparer.Equals(existingName, normalizedName))
                    return false;
            }

            return true;
        }

        private static HashSet<string> CreateOccupiedNames(IEnumerable<XenogermPlan> plans, string excludedPlanId)
        {
            var occupiedNames = new HashSet<string>(_nameComparer);

            foreach (XenogermPlan plan in plans)
            {
                if (plan == null || IsExcluded(plan, excludedPlanId))
                    continue;

                if (TryNormalize(plan.Name, out string existingName))
                    occupiedNames.Add(existingName);
            }

            return occupiedNames;
        }

        private static bool IsExcluded(XenogermPlan plan, string excludedPlanId)
        {
            return excludedPlanId != null && string.Equals(plan.Id, excludedPlanId, StringComparison.Ordinal);
        }

        internal static bool TryNormalize(string name, out string normalizedName)
        {
            normalizedName = name?.Trim();

            if (!string.IsNullOrEmpty(normalizedName))
                return true;

            normalizedName = null;
            return false;
        }

        private static bool TrySplitNumericSuffix(string name, out string baseName, out int suffix)
        {
            int separatorIndex = name.LastIndexOf(' ');

            if (separatorIndex <= 0 || separatorIndex == name.Length - 1)
            {
                baseName = null;
                suffix = 0;
                return false;
            }

            string suffixText = name.Substring(separatorIndex + 1);

            if (!int.TryParse(suffixText, NumberStyles.None, CultureInfo.InvariantCulture, out suffix) || suffix < 2)
            {
                baseName = null;
                suffix = 0;
                return false;
            }

            baseName = name.Substring(0, separatorIndex);
            return true;
        }
    }
}