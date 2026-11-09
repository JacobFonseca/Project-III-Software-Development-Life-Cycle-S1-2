# HydroFlow Working Agreement

Agreed by: Afsana, Evangeline, Kadeema, Jacob. Change it only as a team.

## Branches
- `main` always builds and passes its tests. Nobody commits to it directly.
- One branch per Jira card, named `feature/HF-<number>-<short-name>` (example: `feature/HF-20-device-base`).
- Keep branches short (1 to 3 days). Delete the branch after it is merged.
- Before opening a pull request, bring `main` into your branch (`git merge main`) and fix conflicts on your side.

## Commits
- Small and often. Message starts with the Jira key: `HF-20 Add Device base class`.
- Never commit `bin/`, `obj/`, `.vs/` or `*.db`.

## Pull requests and reviews
- One reviewer per pull request. Review pairs: Afsana reviews Kadeema, Kadeema reviews Jacob, Jacob reviews Evangeline, Evangeline reviews Afsana.
- Review within 24 hours. Check: it builds, tests pass, it matches the contract and the card, and you understand the code.
- Merge with "Squash and merge" once approved and the GitHub Actions check is green.

## Definition of Done
A card is Done when: the code is merged, its unit tests pass, the acceptance criteria or task description is met, and the card is moved to Done in Jira.

## Shared files
Do not change these without telling the team in Teams first: `TelemetryPacket`, `DeviceCommand`, `IMessageBus`, `Device`, `Sensor`, `Actuator`, `DeviceFactory`, the `.sln` and `.csproj` files. If a shared file must change, make that pull request tiny and merge it quickly.

## Code style
- `.editorconfig` is the rule. PascalCase for classes, methods and properties; `_camelCase` for private fields.
- One class per file. File name equals class name.
- Tests are named `Method_Scenario_Expected` and live in `HydroFlow.Tests`, one test file per class (`AirTempSensorTests.cs`).
- UI code only shows things. Decisions live in SCADAController.

## Communication
- Teams chat for daily questions. Post a short update twice a week: what I did, what I will do, what is blocking me.
- If you are going to miss a due date, say so in Teams at least one day before.
- Tuesday class: bring Planner and Jira up to date before each check-in or demo.

## Academic integrity
Follow the course rules on AI and on sharing code. Only commit code that you can explain line by line.

## Conflicts and disagreements
Discuss in Teams first. If we cannot agree, Afsana (Project Leader) decides, and we move on.
