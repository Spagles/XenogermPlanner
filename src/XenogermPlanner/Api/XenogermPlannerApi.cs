using System.Collections.Generic;
using XenogermPlanner.Api.Internal;

namespace XenogermPlanner.Api
{
    public static class XenogermPlannerApi
    {
        public static int ApiVersion => 1;

        public static GenepackRelevanceBatchResult QueryGenepackRelevance(
            IReadOnlyList<GenepackRelevanceRequest> requests)
        {
            return XenogermPlannerRelevanceQuery.Query(requests);
        }
    }
}