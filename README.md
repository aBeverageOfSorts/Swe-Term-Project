# Last Chance

A 2D top-down prison escape game built in Unity for CEN3031 (Software Engineering, Fall 2026).

You play an inmate who has to move through a facility, avoid or fight patrolling officers, pick up items, and reach an exit before getting caught.

## Team — Contraband Studios

| Member | Role |
|---|---|
| Brian | Player Movement & Combat |
| Mathew | UI/UX & Game Manager |
| Riya | Items, Inventory Data & Level Layout |
| Samy | Officer AI |

## Tech Stack

- **Engine:** Unity (2D)
- **Language:** C#
- **Database:** MariaDB, via MySqlConnector
- **Version Control:** Git + GitHub, with Git LFS for binary assets

## Getting Started

1. Clone the repo.
2. Install [Git LFS](https://git-lfs.com/) if you don't have it, then run `git lfs install` once per machine.
3. Open the project folder in Unity Hub (see `ProjectSettings/ProjectVersion.txt` for the exact editor version once the project is pushed).
4. Set up a local MariaDB instance and update the connection string in the config file (not committed — see `.gitignore`).

## Branching

- `main` — always stable, protected, merges only via reviewed pull request.
- `develop` — integration branch.
- `feature/<name>-<short-description>` — individual work, branched off `develop`.

## Project Board

Task tracking lives on this repo's GitHub Projects board (Backlog / In Progress / In Review / Done).
