# RC Park Weather Forcaster

RC Park Weather Forcaster is an automated decision-support tool for RC (Radio Control) pilots. It fetches hourly weather forecasts and active alerts using the [Pirate Weather API](https://pirateweather.net/), evaluates conditions against configurable safety thresholds based on the size of your RC plane, and lets you know if it's safe to fly!

## ✈️ Features
* Supports different plane size categories (`Micro`, `Mini`, `ParkFlyer`, `Large`, `GiantScale`), each with default safe flying thresholds for wind, gusts, and temperature.
* Fetches the next 48 hours of forecast (configurable).
* Checks for any severe weather alerts from the NWS (via Pirate Weather).
* Can be run standalone natively, containerized via Docker Compose, or deployed to a Kubernetes cluster.

## 🔑 Prerequisites
1. **Pirate Weather API Key**: You'll need a free API key from [Pirate Weather](https://pirateweather.net/).
2. **.NET 8 SDK** (if running natively).
3. **Docker** (if running via `docker-compose`).
4. **Kubernetes Cluster** (optional, if deploying via `k8s/` manifests).

---

## 🚀 Running Locally (.NET)

1. Navigate to the `RCParkWeatherForcaster` folder where the `.csproj` is located.
2. Open `appsettings.json`.
3. Set your `PirateWeatherApiKey` to your actual API key.
4. Update the `Location` block with the coordinates (`Latitude` and `Longitude`) of your flying field.
5. Choose your `PlaneSize` (e.g. `ParkFlyer`).
6. Run the application:

```bash
cd RCParkWeatherForcaster
dotnet run
```

---

## 🐳 Running in Docker

You can run the application containerized using the provided `docker-compose.yml` file.

1. Open `docker-compose.yml` in the repository root.
2. Update the `Location__Latitude` and `Location__Longitude` environment variables.
3. You can override any configuration from `appsettings.json` by using double-underscore notation (e.g. `ForecastHours` or `Thresholds__MaxWindSpeedMph`).
4. Make sure to provide your API Key either via an `.env` file or by passing it in:

```bash
docker-compose run -e PirateWeatherApiKey="your_api_key" rc-park-weather-forcaster
```

*(Note: The `docker-compose.yml` is currently set up to build the image from the local `Dockerfile`.)*

---

## ☸️ Deploying to Kubernetes

The `k8s/` directory contains standard Kubernetes manifests:

1. **`namespace.yaml`**: Creates an `rc-park-weather` namespace for the application.
2. **`configmap.yaml`**: Contains the application configuration.
   * **ACTION REQUIRED**: Edit `configmap.yaml` to include your `Location__Latitude`, `Location__Longitude`, and `PirateWeatherApiKey`.
3. **`deployment.yaml` & `service.yaml`**: Deploys the container image and exposes it internally.
4. **`ingress.yaml`**: Exposes the service externally.
   * **ACTION REQUIRED**: Edit `weather.yourdomain.com` to match your fully qualified domain name.
5. **`clusterissuer.yaml`**: Used with [cert-manager](https://cert-manager.io/docs/) to automatically provision Let's Encrypt TLS certificates.
   * **ACTION REQUIRED**: Replace `your-email@example.com` with an email address you use for ACME registration.

Once you have configured the files, deploy them:

```bash
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/clusterissuer.yaml
kubectl apply -f k8s/deployment.yaml
kubectl apply -f k8s/service.yaml
kubectl apply -f k8s/ingress.yaml
```

Enjoy safe flying!
