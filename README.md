# Fleet Tracking in Unity

A Unity 3D fleet-tracking demo backed by a small FastAPI service. The Unity scene displays three vehicles, moves them along authored city routes using GPS samples, and includes a dashboard, route analytics, traffic signals, and vehicle markers.

## Requirements

- Unity **6000.6.8f1** (see `ProjectSettings/ProjectVersion.txt`)
- Python 3.10 or newer
- Git (to clone the repository)

## Project layout

```text
Assets/Scenes/FleetTrackingScene.unity  Main fleet demo scene
Assets/Scripts/                         Unity movement, API, dashboard, and traffic scripts
backend/main.py                         FastAPI service
data/vehicles.csv                       Sample GPS points for V1, V2, and V3
requirements.txt                        Python service dependencies
```

## Run the API

From the repository root, create and activate a virtual environment, then install the dependencies:

```powershell
py -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install -r requirements.txt
```

Start the service from the `backend` directory:

```powershell
Set-Location backend
python -m uvicorn main:app --host 127.0.0.1 --port 8000
```

The service reads `data/vehicles.csv` relative to its own source file, so it can be started from the project root or the `backend` directory. Keep the API running while using the Unity scene. If port 8000 is already occupied, stop the other server with `Ctrl+C` or change the port and the API URL in `Assets/Scripts/VehicleMovement.cs`.

Useful API URLs:

- `http://127.0.0.1:8000/` — service status
- `http://127.0.0.1:8000/docs` — interactive API documentation
- `http://127.0.0.1:8000/vehicles` — latest position and total distance for each vehicle
- `http://127.0.0.1:8000/vehicle/V1` — details for one vehicle
- `http://127.0.0.1:8000/routes` — GPS samples grouped by vehicle
- `http://127.0.0.1:8000/distance` — total distance by vehicle, in kilometers

## Run the Unity scene

1. Open this folder in Unity Hub with Unity 6000.6.8f1. Let Unity resolve the packages listed in `Packages/manifest.json`.
2. Open `Assets/Scenes/FleetTrackingScene.unity`.
3. Start the API as described above.
4. Press **Play**. The scene requests route samples from `http://127.0.0.1:8000/routes`.

The included CSV is example data for three vehicles. The Unity app uses each vehicle's sample sequence to advance it along its configured scene route; the GPS coordinates are also available to the dashboard and API clients.

## Data format

`data/vehicles.csv` must contain these columns:

```csv
vehicle_id,latitude,longitude,timestamp
V1,19.0760,72.8777,2026-11-17 09:00:00
```

The `/routes` response is keyed by vehicle ID, and the Unity scene currently expects `V1`, `V2`, and `V3`. Keep at least two timestamped samples per vehicle for movement and sort them by timestamp (the API sorts the data when it loads).

## Notes

- The API binds to loopback (`127.0.0.1`) by default and is intended for local development.
- Unity's spline objects are editing guides. The runtime road paths are built by `RoadPathBuilder` and do not depend on the spline guides being active during Play mode.
- `RoadExporter` can export road-like scene renderer bounds to `road_export.txt` when its component is used in the scene.
