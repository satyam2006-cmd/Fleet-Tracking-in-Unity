import pandas as pd

# Load GPS dataset
data = pd.read_csv("vehicles.csv")

# Display dataset
print("GPS DATA:")
print(data)

# Sort by vehicle and timestamp
data["timestamp"] = pd.to_datetime(data["timestamp"])

data = data.sort_values(
    by=["vehicle_id", "timestamp"]
)

print("\nSORTED GPS DATA:")
print(data)

# Display available vehicles
print("\nVEHICLES:")
print(data["vehicle_id"].unique())
