using System;
using System.Collections.Generic;
using Escarval.RimWorld.UI;
using RimWorld;
using UnityEngine;
using Verse;
using XenogermPlanner.Analysis;
using XenogermPlanner.Plans;

namespace XenogermPlanner.UI
{
    public sealed class XenogermPlanImportDialog : Window
    {
        private sealed class ImportSourceEntry
        {
            internal CustomXenogerm Source { get; }
            internal int SourceIndex { get; }
            internal CustomXenogermPlanImportData ImportData { get; }
            internal CustomXenogermPlanImportFailure Failure { get; }
            internal List<GeneDef> PreviewGenes { get; }
            internal PlanGeneTargetAnalysisResult TargetAnalysis { get; }

            internal bool IsValid =>
                Failure == CustomXenogermPlanImportFailure.None && ImportData != null;

            internal ImportSourceEntry(
                CustomXenogerm source,
                int sourceIndex,
                CustomXenogermPlanImportData importData,
                CustomXenogermPlanImportFailure failure)
            {
                Source = source;
                SourceIndex = sourceIndex;
                ImportData = importData;
                Failure = failure;

                PreviewGenes = IsValid
                    ? XenogermPlannerPresentation.GetSortedGenes(importData.DesiredGenes)
                    : new List<GeneDef>();
                TargetAnalysis = IsValid
                    ? PlanGeneTargetAnalyzer.Analyze(importData.DesiredGenes)
                    : new PlanGeneTargetAnalysisResult(
                        Array.Empty<PlanGeneConflictDiagnostic>(),
                        Array.Empty<PlanGeneRandomChoiceGroupDiagnostic>(),
                        Array.Empty<PlanGenePrerequisiteDiagnostic>());
            }
        }

        private const float DialogWidth = 1000f;
        private const float DialogHeight = 700f;
        private const float TitleHeight = 34f;
        private const float ColumnGap = RimWorldUiStyle.Metrics.SectionGap;
        private const float LeftColumnWidth = 340f;
        private const float SectionPadding = RimWorldUiStyle.Metrics.PanelPadding;
        private const float SectionTitleHeight = RimWorldUiStyle.Metrics.HeaderHeight;
        private const float SourceRowHeight = RimWorldUiStyle.Metrics.TwoLineRowHeight;
        private const float MetadataLineHeight = 24f;
        private const float GeneRowHeight = RimWorldUiStyle.Metrics.CompactRowHeight;
        private const float ReadinessRowHeight = 30f;
        private const float ReadinessLabelWidth = 150f;
        private const float ReadinessOptionsStartGap = 12f;
        private const float FooterHeight = 40f;
        private const float FooterButtonWidth = 120f;
        private const float FooterButtonHeight = 35f;
        private const float FooterButtonGap = 8f;
        private const float ContentGap = RimWorldUiStyle.Metrics.SectionGap;

        private readonly XenogermPlanGameComponent _component;
        private readonly Action<XenogermPlan> _onImported;
        private readonly List<ImportSourceEntry> _sources = new List<ImportSourceEntry>();

        private ImportSourceEntry _selectedSource;
        private PlanReadinessMode _readinessMode;
        private bool _readinessNotificationsEnabled;
        private CustomXenogermPlanImportFailure _operationFailure;
        private Vector2 _sourceListScrollPosition;
        private Vector2 _previewScrollPosition;
        private Vector2 _geneDiagnosticsScrollPosition;

        private readonly VariableHeightScrollListLayoutCache _previewLayoutCache =
            new VariableHeightScrollListLayoutCache();

        private readonly VariableHeightScrollListLayoutCache _geneDiagnosticsLayoutCache =
            new VariableHeightScrollListLayoutCache();

        private GeneTargetDiagnosticsProjection _targetDiagnosticsProjection;
        private object _presentationLanguageKey;

        public override Vector2 InitialSize =>
            new Vector2(DialogWidth, DialogHeight);

        public XenogermPlanImportDialog(XenogermPlanGameComponent component, Action<XenogermPlan> onImported)
        {
            _component = component ?? throw new ArgumentNullException(nameof(component));

            _onImported = onImported;

            _readinessMode = PlanReadinessMode.Coverage;
            _readinessNotificationsEnabled = true;
            _operationFailure = CustomXenogermPlanImportFailure.None;

            RefreshSources(null);

            doCloseX = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            forcePause = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            using (ImGuiStateScope.Capture())
            {
                RefreshLanguageDependentPresentationIfNeeded();

                Rect windowContentRect = inRect;
                DrawTitle(new Rect(windowContentRect.x, windowContentRect.y, windowContentRect.width, TitleHeight));

                var footerRect = new Rect(
                    windowContentRect.x,
                    windowContentRect.yMax - FooterHeight,
                    windowContentRect.width,
                    FooterHeight);

                var notificationRect = new Rect(
                    windowContentRect.x,
                    footerRect.y - ContentGap - ReadinessRowHeight,
                    windowContentRect.width,
                    ReadinessRowHeight);

                var readinessRect = new Rect(
                    windowContentRect.x,
                    notificationRect.y - ContentGap - ReadinessRowHeight,
                    windowContentRect.width,
                    ReadinessRowHeight);

                var contentRect = new Rect(
                    windowContentRect.x,
                    windowContentRect.y + TitleHeight,
                    windowContentRect.width,
                    readinessRect.y - windowContentRect.y - TitleHeight - ContentGap);

                float leftColumnWidth = Mathf.Min(LeftColumnWidth, contentRect.width * 0.4f);

                var sourcesRect = new Rect(contentRect.x, contentRect.y, leftColumnWidth, contentRect.height);

                var previewRect = new Rect(
                    sourcesRect.xMax + ColumnGap,
                    contentRect.y,
                    contentRect.width - leftColumnWidth - ColumnGap,
                    contentRect.height);

                DrawSourceList(sourcesRect);
                DrawPreviewWorkspace(previewRect);
                DrawReadinessModeField(readinessRect);
                DrawReadinessNotificationsField(notificationRect);
                DrawFooter(footerRect);
            }
        }

        private static void DrawTitle(Rect rect)
        {
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;

            Widgets.Label(rect, "XenogermPlanner.PlanImport.Title".Translate());
        }

        private void DrawSourceList(Rect rect)
        {
            RimWorldUiWidgets.DrawPanel(rect);

            Rect innerRect = rect.ContractedBy(SectionPadding);

            var headerRect = new Rect(innerRect.x, innerRect.y, innerRect.width, SectionTitleHeight);

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;

            Widgets.Label(headerRect, "XenogermPlanner.PlanImport.SourcesTitle".Translate());

            var listRect = new Rect(
                innerRect.x,
                headerRect.yMax,
                innerRect.width,
                innerRect.height - SectionTitleHeight);

            if (_sources.Count == 0)
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;

                Widgets.Label(listRect, "XenogermPlanner.PlanImport.EmptySources".Translate());

                return;
            }

            FixedHeightScrollListLayout layout = RimWorldUiWidgets.BeginFixedHeightScrollView(
                listRect,
                ref _sourceListScrollPosition,
                _sources.Count,
                SourceRowHeight,
                out float viewWidth);

            for (int index = layout.FirstVisibleIndex; index < layout.LastVisibleIndexExclusive; index++)
            {
                ImportSourceEntry source = _sources[index];

                var rowRect = new Rect(0f, index * SourceRowHeight, viewWidth, SourceRowHeight);

                DrawSourceRow(rowRect, source, ReferenceEquals(_selectedSource, source), index);
            }

            Widgets.EndScrollView();
        }

        private void DrawSourceRow(Rect rect, ImportSourceEntry source, bool selected, int rowIndex)
        {
            RimWorldUiWidgets.DrawSelectableRowBackground(rect, rowIndex, selected, Mouse.IsOver(rect));

            Rect contentRect = rect.ContractedBy(RimWorldUiStyle.Metrics.ControlGap);
            float nameHeight = contentRect.height * 0.56f;
            var nameRect = new Rect(contentRect.x, contentRect.y, contentRect.width, nameHeight);
            var metadataRect = new Rect(
                contentRect.x,
                nameRect.yMax,
                contentRect.width,
                Mathf.Max(0f, contentRect.yMax - nameRect.yMax));

            RimWorldUiWidgets.DrawTruncatedLabelWithTooltip(
                nameRect,
                GetSourceDisplayName(source.Source),
                GameFont.Small,
                TextAnchor.MiddleLeft);

            using (ImGuiStateScope.Capture())
            {
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleLeft;
                GUI.color = source.IsValid ? RimWorldUiStyle.Colors.MutedText : RimWorldUiStyle.Colors.Warning;

                Widgets.Label(
                    metadataRect,
                    source.IsValid
                        ? "XenogermPlanner.Planner.DesiredGeneCount".Translate(source.ImportData.DesiredGenes.Count)
                        : "XenogermPlanner.PlanImport.InvalidSourceMarker".Translate());
            }

            if (Event.current.button == 0 && Widgets.ButtonInvisible(rect))
                SelectSource(source);
        }

        private void DrawPreviewWorkspace(Rect rect)
        {
            bool hasDiagnostics = _selectedSource != null && _selectedSource.IsValid &&
                                  _selectedSource.TargetAnalysis.HasDiagnostics;

            if (!hasDiagnostics)
            {
                DrawPreview(rect);
                return;
            }

            float diagnosticsWidth = XenogermPlannerWidgets.CalculateGeneTargetDiagnosticsPanelWidth(rect.width);
            float previewWidth = Mathf.Max(0f, rect.width - diagnosticsWidth - ColumnGap);

            var previewRect = new Rect(rect.x, rect.y, previewWidth, rect.height);
            var diagnosticsRect = new Rect(
                previewRect.xMax + ColumnGap,
                rect.y,
                Mathf.Max(0f, rect.xMax - previewRect.xMax - ColumnGap),
                rect.height);

            DrawPreview(previewRect);

            XenogermPlannerWidgets.DrawGeneTargetDiagnosticsPanel(
                diagnosticsRect,
                _targetDiagnosticsProjection,
                _geneDiagnosticsLayoutCache,
                ref _geneDiagnosticsScrollPosition);
        }

        private void DrawPreview(Rect rect)
        {
            RimWorldUiWidgets.DrawPanel(rect);

            Rect innerRect = rect.ContractedBy(SectionPadding);

            var headerRect = new Rect(innerRect.x, innerRect.y, innerRect.width, SectionTitleHeight);

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;

            Widgets.Label(headerRect, "XenogermPlanner.PlanImport.PreviewTitle".Translate());

            var previewRect = new Rect(
                innerRect.x,
                headerRect.yMax,
                innerRect.width,
                innerRect.height - SectionTitleHeight);

            float viewWidth = Mathf.Max(0f, previewRect.width - RimWorldUiStyle.Metrics.ScrollbarWidth);

            VariableHeightScrollListLayout layout = GetPreviewLayout(viewWidth);
            VariableHeightScrollListVisibleRange visibleRange = RimWorldUiWidgets.BeginVariableHeightScrollView(
                previewRect,
                ref _previewScrollPosition,
                layout,
                out Rect viewRect);

            DrawPreviewContent(viewRect, layout, visibleRange);

            Widgets.EndScrollView();
        }

        private VariableHeightScrollListLayout GetPreviewLayout(float viewWidth)
        {
            if (_previewLayoutCache.TryGet(
                    _selectedSource,
                    (int)_operationFailure,
                    viewWidth,
                    out VariableHeightScrollListLayout layout))
            {
                return layout;
            }

            var rows = new List<VariableHeightScrollListRowMeasurement>();

            if (_operationFailure != CustomXenogermPlanImportFailure.None)
            {
                rows.Add(
                    new VariableHeightScrollListRowMeasurement(
                        GetTextHeight(GetImportFailureMessage(_operationFailure), viewWidth, GameFont.Small),
                        ContentGap));
            }

            if (_selectedSource != null)
            {
                rows.Add(
                    new VariableHeightScrollListRowMeasurement(SectionTitleHeight, RimWorldUiStyle.Metrics.SmallGap));

                if (!_selectedSource.IsValid)
                {
                    rows.Add(
                        new VariableHeightScrollListRowMeasurement(
                            GetTextHeight(
                                GetImportFailureMessage(_selectedSource.Failure),
                                viewWidth,
                                GameFont.Small)));
                }
                else
                {
                    rows.Add(new VariableHeightScrollListRowMeasurement(MetadataLineHeight, ContentGap));

                    rows.Add(
                        new VariableHeightScrollListRowMeasurement(
                            SectionTitleHeight,
                            _selectedSource.PreviewGenes.Count == 0 ? ContentGap : 0f));

                    for (var geneIndex = 0; geneIndex < _selectedSource.PreviewGenes.Count; geneIndex++)
                    {
                        rows.Add(
                            new VariableHeightScrollListRowMeasurement(
                                GeneRowHeight,
                                geneIndex + 1 == _selectedSource.PreviewGenes.Count ? ContentGap : 0f));
                    }
                }
            }

            layout = VariableHeightScrollListLayout.Create(viewWidth, rows);
            _previewLayoutCache.Store(_selectedSource, (int)_operationFailure, layout);
            return layout;
        }

        private void DrawPreviewContent(
            Rect rect,
            VariableHeightScrollListLayout layout,
            VariableHeightScrollListVisibleRange visibleRange)
        {
            var rowIndex = 0;

            if (_operationFailure != CustomXenogermPlanImportFailure.None)
            {
                int failureRowIndex = rowIndex++;

                if (visibleRange.Contains(failureRowIndex))
                {
                    string operationFailureMessage = GetImportFailureMessage(_operationFailure);
                    Rect failureRect = GetPreviewRowRect(rect, layout, failureRowIndex);
                    DrawWarningText(failureRect, operationFailureMessage);
                }
            }

            if (_selectedSource == null)
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;

                float y = rect.y + layout.ContentHeight;

                Widgets.Label(
                    new Rect(rect.x, y, rect.width, Mathf.Max(0f, rect.yMax - y)),
                    "XenogermPlanner.PlanImport.NoSelectedSource".Translate());

                return;
            }

            int sourceTitleRowIndex = rowIndex++;

            if (visibleRange.Contains(sourceTitleRowIndex))
            {
                Rect sourceTitleRect = GetPreviewRowRect(rect, layout, sourceTitleRowIndex);
                RimWorldUiWidgets.DrawTruncatedLabelWithTooltip(
                    sourceTitleRect,
                    GetSourceDisplayName(_selectedSource.Source),
                    GameFont.Medium,
                    TextAnchor.MiddleLeft);
            }

            if (!_selectedSource.IsValid)
            {
                int failureRowIndex = rowIndex;

                if (visibleRange.Contains(failureRowIndex))
                {
                    string failureMessage = GetImportFailureMessage(_selectedSource.Failure);
                    Rect failureRect = GetPreviewRowRect(rect, layout, failureRowIndex);
                    DrawWarningText(failureRect, failureMessage);
                }

                return;
            }

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;

            int metadataRowIndex = rowIndex++;

            if (visibleRange.Contains(metadataRowIndex))
            {
                Rect metadataRect = GetPreviewRowRect(rect, layout, metadataRowIndex);

                Widgets.Label(
                    metadataRect,
                    "XenogermPlanner.Planner.DesiredGeneCount".Translate(
                        _selectedSource.ImportData.DesiredGenes.Count));
            }

            Text.Font = GameFont.Medium;

            int desiredGenesHeaderRowIndex = rowIndex++;

            if (visibleRange.Contains(desiredGenesHeaderRowIndex))
            {
                Rect desiredGenesHeaderRect = GetPreviewRowRect(rect, layout, desiredGenesHeaderRowIndex);
                Widgets.Label(desiredGenesHeaderRect, "XenogermPlanner.Planner.DesiredGenes".Translate());
            }

            Text.Font = GameFont.Small;

            int geneRowsStartIndex = rowIndex;
            int firstVisibleGeneRowIndex = Math.Max(visibleRange.FirstVisibleIndex, geneRowsStartIndex);

            int lastVisibleGeneRowIndexExclusive = Math.Min(
                visibleRange.LastVisibleIndexExclusive,
                geneRowsStartIndex + _selectedSource.PreviewGenes.Count);

            for (int layoutRowIndex = firstVisibleGeneRowIndex;
                 layoutRowIndex < lastVisibleGeneRowIndexExclusive;
                 layoutRowIndex++)
            {
                int geneIndex = layoutRowIndex - geneRowsStartIndex;
                GeneDef gene = _selectedSource.PreviewGenes[geneIndex];
                Rect geneRect = GetPreviewRowRect(rect, layout, layoutRowIndex);

                RimWorldUiWidgets.DrawSelectableRowBackground(
                    geneRect,
                    geneIndex,
                    selected: false,
                    hovered: Mouse.IsOver(geneRect),
                    drawAccent: false);
                XenogermPlannerWidgets.DrawGeneLabel(geneRect, gene);

                XenogermPlannerWidgets.AddGeneTooltip(geneRect, gene);
                XenogermPlannerNativeInspector.TryOpenContextMenu(geneRect, gene);
            }
        }

        private static Rect GetPreviewRowRect(Rect contentRect, VariableHeightScrollListLayout layout, int rowIndex)
        {
            return new Rect(
                contentRect.x,
                contentRect.y + layout.GetRowOffset(rowIndex),
                contentRect.width,
                layout.GetRowHeight(rowIndex));
        }

        private void DrawReadinessModeField(Rect rect)
        {
            var labelRect = new Rect(rect.x, rect.y, ReadinessLabelWidth, rect.height);

            var selectorRect = new Rect(
                labelRect.xMax + ReadinessOptionsStartGap,
                rect.y,
                rect.xMax - labelRect.xMax - ReadinessOptionsStartGap,
                rect.height);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;

            Widgets.Label(labelRect, "XenogermPlanner.PlanImport.ReadinessMode".Translate());

            PlanReadinessMode previousMode = _readinessMode;
            XenogermPlannerWidgets.DrawReadinessModeSelector(selectorRect, ref _readinessMode);

            if (_readinessMode != previousMode)
                RefreshDiagnosticsProjection();
        }

        private void DrawReadinessNotificationsField(Rect rect)
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;

            Widgets.CheckboxLabeled(
                rect,
                "XenogermPlanner.Notifications.ReadinessEnabled".Translate(),
                ref _readinessNotificationsEnabled);

            TooltipHandler.TipRegion(rect, "XenogermPlanner.Notifications.ReadinessEnabledDescription".Translate());
        }

        private void DrawFooter(Rect rect)
        {
            var importButtonRect = new Rect(
                rect.xMax - FooterButtonWidth,
                rect.y + (rect.height - FooterButtonHeight) * 0.5f,
                FooterButtonWidth,
                FooterButtonHeight);

            var cancelButtonRect = new Rect(
                importButtonRect.x - FooterButtonGap - FooterButtonWidth,
                importButtonRect.y,
                FooterButtonWidth,
                FooterButtonHeight);

            if (Widgets.ButtonText(cancelButtonRect, "XenogermPlanner.PlanImport.Cancel".Translate()))
            {
                Close();
            }

            bool previousEnabled = GUI.enabled;

            GUI.enabled = _selectedSource != null && _selectedSource.IsValid;

            bool importClicked = Widgets.ButtonText(importButtonRect, "XenogermPlanner.PlanImport.Import".Translate());

            GUI.enabled = previousEnabled;

            if (importClicked)
                ImportSelectedSource();
        }

        private void SelectSource(ImportSourceEntry source)
        {
            if (ReferenceEquals(_selectedSource, source))
            {
                return;
            }

            _selectedSource = source;
            _operationFailure = CustomXenogermPlanImportFailure.None;
            _previewLayoutCache.Invalidate();
            RefreshDiagnosticsProjection();
            _previewScrollPosition = Vector2.zero;
            _geneDiagnosticsScrollPosition = Vector2.zero;
        }

        private void ImportSelectedSource()
        {
            _operationFailure = CustomXenogermPlanImportFailure.None;
            _previewLayoutCache.Invalidate();

            if (_selectedSource == null)
            {
                _operationFailure = CustomXenogermPlanImportFailure.SourceUnavailable;
                _previewLayoutCache.Invalidate();

                return;
            }

            CustomXenogerm source = _selectedSource.Source;

            IReadOnlyList<CustomXenogerm> runtimeSources = GetRuntimeSources();

            if (runtimeSources == null || !ContainsSourceReference(runtimeSources, source))
            {
                _operationFailure = CustomXenogermPlanImportFailure.SourceUnavailable;
                _previewLayoutCache.Invalidate();

                RefreshSources(null);
                return;
            }

            if (!CustomXenogermPlanImporter.TryReadSource(
                    source,
                    out CustomXenogermPlanImportData importData,
                    out CustomXenogermPlanImportFailure failure))
            {
                _operationFailure = failure;
                _previewLayoutCache.Invalidate();

                RefreshSources(source);
                return;
            }

            var plan = new XenogermPlan(
                importData.Name,
                importData.DesiredGenes,
                _readinessMode,
                _readinessNotificationsEnabled);

            _component.AddPlanWithAllocatedName(plan);

            _onImported?.Invoke(plan);

            Close();
        }

        private void RefreshSources(CustomXenogerm preferredSource)
        {
            _sources.Clear();

            IReadOnlyList<CustomXenogerm> runtimeSources = GetRuntimeSources();

            if (runtimeSources != null)
            {
                for (var index = 0; index < runtimeSources.Count; index++)
                {
                    CustomXenogerm source = runtimeSources[index];

                    CustomXenogermPlanImporter.TryReadSource(
                        source,
                        out CustomXenogermPlanImportData importData,
                        out CustomXenogermPlanImportFailure failure);

                    _sources.Add(new ImportSourceEntry(source, index, importData, failure));
                }
            }

            _sources.Sort(CompareSourceEntries);

            _selectedSource = null;

            if (preferredSource != null)
            {
                foreach (ImportSourceEntry source in _sources)
                {
                    if (!ReferenceEquals(source.Source, preferredSource))
                    {
                        continue;
                    }

                    _selectedSource = source;
                    break;
                }
            }

            if (_selectedSource == null && _sources.Count > 0)
            {
                _selectedSource = _sources[0];
            }

            _previewLayoutCache.Invalidate();
            RefreshDiagnosticsProjection();
            _sourceListScrollPosition = Vector2.zero;
            _previewScrollPosition = Vector2.zero;
            _geneDiagnosticsScrollPosition = Vector2.zero;
        }

        private void RefreshDiagnosticsProjection()
        {
            _presentationLanguageKey = LanguageDatabase.activeLanguage;
            _targetDiagnosticsProjection = _selectedSource != null && _selectedSource.IsValid
                ? GeneTargetDiagnosticsProjection.Build(_selectedSource.TargetAnalysis, _readinessMode)
                : null;
            _geneDiagnosticsLayoutCache.Invalidate();

            if (_targetDiagnosticsProjection == null || !_targetDiagnosticsProjection.HasDiagnostics)
                _geneDiagnosticsScrollPosition = Vector2.zero;
        }

        private void RefreshLanguageDependentPresentationIfNeeded()
        {
            object languageKey = LanguageDatabase.activeLanguage;

            if (Equals(_presentationLanguageKey, languageKey))
                return;

            RefreshDiagnosticsProjection();
        }

        private static IReadOnlyList<CustomXenogerm> GetRuntimeSources()
        {
            CustomXenogermDatabase database = Current.Game?.customXenogermDatabase;

            return database?.CustomXenogermsForReading;
        }

        private static bool ContainsSourceReference(IReadOnlyList<CustomXenogerm> sources, CustomXenogerm source)
        {
            foreach (CustomXenogerm t in sources)
            {
                if (ReferenceEquals(t, source))
                {
                    return true;
                }
            }

            return false;
        }

        private static int CompareSourceEntries(ImportSourceEntry left, ImportSourceEntry right)
        {
            int nameComparison = StringComparer.CurrentCultureIgnoreCase.Compare(
                GetSourceDisplayName(left.Source),
                GetSourceDisplayName(right.Source));

            if (nameComparison != 0)
                return nameComparison;

            return left.SourceIndex.CompareTo(right.SourceIndex);
        }

        private static string GetSourceDisplayName(CustomXenogerm source)
        {
            if (source == null || string.IsNullOrEmpty(source.name))
            {
                return "XenogermPlanner.PlanImport.UnnamedSource".Translate().ToString();
            }

            return source.name;
        }

        private static string GetImportFailureMessage(CustomXenogermPlanImportFailure failure)
        {
            switch (failure)
            {
                case CustomXenogermPlanImportFailure.SourceUnavailable:
                    return "XenogermPlanner.PlanImport.SourceUnavailable".Translate().ToString();

                case CustomXenogermPlanImportFailure.InvalidSourceData:
                    return "XenogermPlanner.PlanImport.InvalidSource".Translate().ToString();

                case CustomXenogermPlanImportFailure.EmptySource:
                    return "XenogermPlanner.PlanImport.EmptySource".Translate().ToString();

                default:
                    return string.Empty;
            }
        }

        private static void DrawWarningText(Rect rect, string text)
        {
            using (ImGuiStateScope.Capture())
            {
                GUI.color = RimWorldUiStyle.Colors.Warning;
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;

                Widgets.Label(rect, text);
            }
        }

        private static float GetTextHeight(string text, float width, GameFont font)
        {
            using (ImGuiStateScope.Capture())
            {
                Text.Font = font;
                return Text.CalcHeight(text, width);
            }
        }
    }
}