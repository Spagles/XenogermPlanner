# Xenogerm Planner

*Plan your xenogerms before you own every genepack.*

Xenogerm Planner is a planning and tracking mod for RimWorld's Biotech genetics system.

Instead of waiting until you already have every genepack you need, you can create a xenogerm plan in advance, choose the genes you want, and track your progress as your colony collects them.

Plans are saved with your game and remain independent from vanilla xenogerm templates.

## Features

- Create plans from scratch, from existing vanilla xenogerm templates, or from premade and saved xenotypes, then edit the genes before saving.
- Edit existing plans using all genes available in your game.
- Duplicate plans or copy and paste them through the clipboard.
- Track the genepacks currently available on your active map.
- Inspect genes and genepacks or jump directly to their location.
- Select a Gene Assembler and check whether it can assemble your plan.
- See missing prerequisite genes and possible gene conflicts.
- See complexity, metabolic efficiency, hunger rate, and required Archite Capsules while editing a plan.
- Find pawns who may provide genes you are still missing.
- Create vanilla xenogerm templates from ready plans.
- Receive optional notifications when a plan becomes ready.
- Receive advisory notifications when active orbital or visiting caravan traders offer genepacks relevant to incomplete plans.

## Planning modes

### All planned genes

Every gene in the plan must be available, but the genepacks may also contain extra genes.

### Exact gene set

The genepacks must provide exactly the genes in the plan, without any extras.

## Requirements

- RimWorld 1.6
- Biotech

Harmony is not required and is not included.

## Compatibility

Xenogerm Planner is designed to work with the standard RimWorld and Biotech genetics systems.

Gene mods are generally expected to work as long as they continue to use the normal RimWorld systems for genes, genepacks, Gene Banks, Gene Assemblers, Gene Extractors, and xenogerm templates.

Mods that heavily replace or rewrite these systems may not be compatible.

Planning currently covers the active map. Genepacks in caravans, on other maps, or still being offered by traders do not count toward plan readiness.

See [Compatibility and known limitations](docs/compatibility.md) for the complete support policy.

## Integrations

### Trader advisories

Xenogerm Planner monitors supported traders on the active map and can notify you when an orbital trader or visiting trader caravan currently offers a genepack relevant to an incomplete plan.

Trader offers remain advisory. They do not enter the Planner's physical product inventory and do not count toward readiness until the genepack is actually acquired and becomes available on the active map. Visiting-caravan notifications can jump to the trader pawn; orbital notifications remain text-only.

### Settlement Trade Overview

Xenogerm Planner supports optional integration with Settlement Trade Overview.

When both mods are installed, Settlement Trade Overview can mark genepacks for sale that contain genes useful for plans you have not completed yet. The integration respects both planning modes, including the stricter Exact gene set mode.

Trade offers are only suggestions and do not count toward a plan until the genepack is actually acquired and becomes available to Xenogerm Planner on the active map.

Neither mod requires the other for its normal functionality.

**Settlement Trade Overview:** https://steamcommunity.com/sharedfiles/filedetails/?id=3781528628

### Integration API

Xenogerm Planner exposes an optional read-only integration API version `1` for other mods that need Planner-owned genepack relevance information without introducing a mandatory dependency.

See the [integration API guide](docs/integration-api.md) for the public contract, versioning rules, statuses, and soft-binding example.

## Languages

- English
- Russian
- Ukrainian

## Installation

For normal gameplay, installing Xenogerm Planner through the Steam Workshop is recommended.

**Steam Workshop:** https://steamcommunity.com/sharedfiles/filedetails/?id=3781523927

This repository contains the source project and is not a ready-to-install mod folder. Development builds require the setup described in the [Development](#development) section below.

## Development

### Prerequisites

- RimWorld 1.6 installed locally.
- A build environment capable of targeting .NET Framework 4.7.2.
- A sibling checkout of `Escarval.RimWorld.UI`.

`Escarval.RimWorld.UI` is compiled into `XenogermPlanner.dll` as shared source. It is a development dependency, not a separate runtime mod dependency.

### Workspace layout

The current build expects the repositories to use this layout:

```text
RimWorldMods/
├── Escarval.RimWorld.UI/
└── XenogermPlanner/
```

### Local configuration

1. Configure the local props file required by `Escarval.RimWorld.UI`.
2. Copy `docs/XenogermPlanner.Local.props.example` to `src/XenogermPlanner.Local.props`.
3. Set `RimWorldManagedDir` to RimWorld's `Managed` directory.
4. Set `RimWorldModAssembliesDir` to the development mod assembly directory used by the post-build deployment target.

The local props files contain machine-specific paths and should remain excluded from Git.

### Build

Build the production solution in Release configuration:

```bash
dotnet build src/XenogermPlanner.sln -c Release
```

### Tests

Run the Xenogerm Planner test project with:

```bash
dotnet test tests/XenogermPlanner.Tests/XenogermPlanner.Tests.csproj -c Release
```

## Technical documentation

- [Architecture](docs/architecture.md)
- [Testing policy](docs/testing.md)
- [Compatibility and known limitations](docs/compatibility.md)
- [Integration API](docs/integration-api.md)

## Links

- Steam Workshop: https://steamcommunity.com/sharedfiles/filedetails/?id=3781523927
- GitHub Releases: coming soon