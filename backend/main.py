
from fastapi import FastAPI, HTTPException
import pandas as pd
import math
from pathlib import Path

app = FastAPI(title="Intelligent Fleet Tracking API")

# Load GPS dataset
DATA_FILE = Path(__file__).resolve().parent.parent / "data" / "vehicles.csv"
df = pd.read_csv(DATA_FILE)

df["timestamp"] = pd.to_datetime(df["timestamp"])

df = df.sort_values(
    by=["vehicle_id", "timestamp"]
)


# Haversine distance calculation
def calculate_distance(lat1, lon1, lat2, lon2):

    R = 6371

    lat1 = math.radians(lat1)
    lon1 = math.radians(lon1)
    lat2 = math.radians(lat2)
    lon2 = math.radians(lon2)

    dlat = lat2 - lat1
    dlon = lon2 - lon1

    a = (
        math.sin(dlat / 2)**2
        + math.cos(lat1) * math.cos(lat2)
        * math.sin(dlon / 2)**2
    )

    c = 2 * math.atan2(
        math.sqrt(a),
        math.sqrt(1 - a)
    )

    return R * c


# Calculate total vehicle distance
def get_distance(group):

    group = group.reset_index(drop=True)

    total = 0

    for i in range(1, len(group)):

        total += calculate_distance(
            group.loc[i-1, "latitude"],
            group.loc[i-1, "longitude"],
            group.loc[i, "latitude"],
            group.loc[i, "longitude"]
        )

    return round(total, 3)


# Home API
@app.get("/")
def home():

    return {
        "message": "Intelligent Fleet Tracking API is running"
    }


# Get all vehicles
@app.get("/vehicles")
def get_vehicles():

    vehicles = []

    for vehicle, group in df.groupby("vehicle_id"):

        latest = group.iloc[-1]

        vehicles.append({
            "vehicle_id": vehicle,
            "latitude": float(latest["latitude"]),
            "longitude": float(latest["longitude"]),
            "distance_km": get_distance(group)
        })

    return vehicles


# Get individual vehicle details
@app.get("/vehicle/{vehicle_id}")
def get_vehicle(vehicle_id: str):

    group = df[df["vehicle_id"] == vehicle_id]

    if group.empty:
        raise HTTPException(
            status_code=404,
            detail="Vehicle not found"
        )

    latest = group.iloc[-1]

    return {
        "vehicle_id": vehicle_id,
        "latitude": float(latest["latitude"]),
        "longitude": float(latest["longitude"]),
        "distance_km": get_distance(group),
        "timestamp": latest["timestamp"].isoformat()
    }


# Get GPS route of all vehicles
@app.get("/routes")
def get_routes():

    routes = {}

    for vehicle, group in df.groupby("vehicle_id"):

        routes[vehicle] = [
            {
                "latitude": float(row.latitude),
                "longitude": float(row.longitude),
                "timestamp": row.timestamp.isoformat()
            }
            for row in group.itertuples()
        ]

    return routes


# Get distance report
@app.get("/distance")
def get_distances():

    result = {}

    for vehicle, group in df.groupby("vehicle_id"):

        result[vehicle] = get_distance(group)

    return result
