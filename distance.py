
import pandas as pd
import math

# Load GPS dataset
df = pd.read_csv("data/vehicles.csv")

# Convert timestamp
df["timestamp"] = pd.to_datetime(df["timestamp"])

# Sort records
df = df.sort_values(
    by=["vehicle_id", "timestamp"]
)

# Haversine formula
def calculate_distance(lat1, lon1, lat2, lon2):

    R = 6371  # Earth's radius in KM

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


# Calculate distance for each vehicle
print("\nVEHICLE DISTANCE REPORT")

for vehicle, group in df.groupby("vehicle_id"):

    group = group.reset_index(drop=True)

    total_distance = 0

    for i in range(1, len(group)):

        distance = calculate_distance(
            group.loc[i-1, "latitude"],
            group.loc[i-1, "longitude"],
            group.loc[i, "latitude"],
            group.loc[i, "longitude"]
        )

        total_distance += distance

    print("--------------------------")
    print("Vehicle:", vehicle)
    print("Total Distance:", round(total_distance, 3), "KM")
