# HydroFlow SCADA
A SCADA/HMI desktop application that monitors and controls a simulated indoor vertical hydroponic farm.
Course project: CSCN72030 Project III (F26), Conestoga College. Team: TechHub.

## What it does
- Reads 7 simulated sensors and controls 5 simulated actuators. Every device reads a looping data file to simulate changing conditions.
- All devices talk through one **MessageBus** (Observer pattern). Devices are built by a **DeviceFactory** (Factory pattern). Dashboard buttons send commands to actuators (Command pattern).
- **SCADAController** checks readings against safe ranges and decides when to act.
- A **WPF dashboard (HMIDisplay)** shows live tiles, alerts and manual controls.
- **SQLiteLogger** stores every message in a local SQLite database.

## Tech
C#, .NET (version pinned in `global.json`), WPF, SQLite, MSTest, GitHub Actions.

## Solution layout
```
HydroFlow.sln
 HydroFlow.Core        message types, MessageBus, Device/Sensor/Actuator, DeviceFactory, LoopingFileReader
 HydroFlow.Devices     the 7 sensors and 5 actuators (one folder per module)
 HydroFlow.Controller  SCADAController
 HydroFlow.Logging     SQLiteLogger
 HydroFlow.UI          WPF dashboard (HMIDisplay)
 HydroFlow.Tests       unit and integration tests
 data/                 one looping data file per device
 docs/                 working agreement, Sprint 1 contract, SRS, diagrams
```

## Build, run, test
Requires Visual Studio 2022 with the ".NET desktop development" workload and the SDK in `global.json`.
```
dotnet build
dotnet test
dotnet run --project HydroFlow.UI
```
Data files must be set to "Copy to Output Directory".

## Team
| Name | Role | Module |
|---|---|---|
| Afsana Zaman | Project Leader | Climate, Device, MessageBus |
| Evangeline Singh | Quality Assurance Leader | Water Supply, Sensor, SCADAController |
| Kadeema Wakha | Software Architect | Water Chemistry, Actuator |
| Jacob Stekelenburg | System Engineer | Lighting and Integration, DeviceFactory, HMIDisplay, SQLiteLogger |

## How we work
Read `docs/WORKING_AGREEMENT.md`. Short version: one branch per Jira card, pull request into `main`, one teammate reviews, tests must pass.

## Sprints
| Sprint | Dates | Goal |
|---|---|---|
| 1 | Nov 2 to Nov 13 | Working MessageBus integrated across all devices |
| 2 | Nov 16 to Nov 27 | SQLite stores every message; basic dashboard works |
| 3 | Nov 30 to Dec 11 | Complete GUI, UX test, acceptance testing |
| Final | Dec 14 to Dec 15 | Documentation, test report, final demo (Dec 15) |
