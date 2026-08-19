using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RimWorld;
using Verse;
using XenogermPlanner.Genes;

namespace XenogermPlanner.Trade
{
    internal sealed class PlanTraderAdvisoryOfferSnapshot
    {
        private readonly ReadOnlyCollection<GeneDef> _genes;

        internal PlanTraderAdvisoryOfferSnapshot(Genepack genepack, IEnumerable<GeneDef> genes)
        {
            Genepack = genepack ?? throw new ArgumentNullException(nameof(genepack));

            HashSet<GeneDef> distinctGenes = GenepackCompositionUtility.CopyDistinctGenes(genes, nameof(genes));

            if (distinctGenes.Count == 0)
                throw new ArgumentException("Trader genepack composition cannot be empty.", nameof(genes));

            _genes = new List<GeneDef>(distinctGenes).AsReadOnly();
        }

        internal Genepack Genepack { get; }
        internal IReadOnlyCollection<GeneDef> Genes => _genes;
    }
}