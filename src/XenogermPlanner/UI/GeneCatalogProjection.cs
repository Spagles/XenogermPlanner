using System;
using System.Collections.Generic;
using Verse;

namespace XenogermPlanner.UI
{
    internal enum GeneCatalogRowKind
    {
        Category,
        Gene
    }

    internal readonly struct GeneCatalogRow
    {
        private GeneCatalogRow(GeneCatalogRowKind kind, GeneCategoryDef category, GeneDef gene, bool isCategoryExpanded)
        {
            Kind = kind;
            Category = category;
            Gene = gene;
            IsCategoryExpanded = isCategoryExpanded;
        }

        internal GeneCatalogRowKind Kind { get; }

        internal GeneCategoryDef Category { get; }

        internal GeneDef Gene { get; }

        internal bool IsCategoryExpanded { get; }

        internal static GeneCatalogRow CreateCategory(GeneCategoryDef category, bool isExpanded)
        {
            if (category == null)
                throw new ArgumentNullException(nameof(category));

            return new GeneCatalogRow(GeneCatalogRowKind.Category, category, null, isExpanded);
        }

        internal static GeneCatalogRow CreateGene(GeneDef gene)
        {
            if (gene == null)
                throw new ArgumentNullException(nameof(gene));

            return new GeneCatalogRow(GeneCatalogRowKind.Gene, null, gene, isCategoryExpanded: false);
        }
    }

    internal static class GeneCatalogProjection
    {
        internal static List<GeneCatalogRow> Build(
            IReadOnlyList<GeneDef> genes,
            IReadOnlyDictionary<GeneCategoryDef, bool> collapsedCategories,
            bool forceExpanded)
        {
            if (genes == null)
                throw new ArgumentNullException(nameof(genes));

            if (collapsedCategories == null)
                throw new ArgumentNullException(nameof(collapsedCategories));

            int capacity = genes.Count + Math.Min(genes.Count, collapsedCategories.Count);
            var rows = new List<GeneCatalogRow>(capacity);
            GeneCategoryDef currentCategory = null;
            var currentCategoryExpanded = false;

            foreach (GeneDef gene in genes)
            {
                if (gene == null)
                    throw new ArgumentException("Gene catalog cannot contain null genes.", nameof(genes));

                GeneCategoryDef category = gene.displayCategory;

                if (category != currentCategory)
                {
                    currentCategory = category;
                    currentCategoryExpanded = forceExpanded ||
                                              !collapsedCategories.TryGetValue(category, out bool collapsed) ||
                                              !collapsed;

                    rows.Add(GeneCatalogRow.CreateCategory(category, currentCategoryExpanded));
                }

                if (currentCategoryExpanded)
                    rows.Add(GeneCatalogRow.CreateGene(gene));
            }

            return rows;
        }
    }
}