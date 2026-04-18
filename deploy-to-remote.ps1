param(
    [string]$Server = "192.168.1.190",
    [string]$User = "arsha"
)

Write-Host "Rebuilding the Docker image locally..."
docker build -t rc-park-weather-forcaster:latest .

Write-Host "Saving the Docker image to a tar archive for transfer (this takes a moment)..."
docker save rc-park-weather-forcaster:latest -o rc-park-weather-forcaster.tar

Write-Host "Deploying image to $Server... (You will be prompted for your SSH password)"
scp rc-park-weather-forcaster.tar ${User}@${Server}:/tmp/rc-park-weather-forcaster.tar

Write-Host "Importing the updated image into K3s on $Server... (You will be prompted for your SSH password)"
ssh ${User}@${Server} 'sudo k3s ctr images import /tmp/rc-park-weather-forcaster.tar && rm /tmp/rc-park-weather-forcaster.tar'

Write-Host "Re-applying the updated deployment..."
kubectl apply -f k8s/deployment.yaml

Write-Host "Deployment complete! Checking pod status..."
kubectl get pods

Write-Host "Removing temporary local tar archive..."
Remove-Item rc-park-weather-forcaster.tar -ErrorAction SilentlyContinue

Write-Host "Done! Your service should now be immune to TTL expiry & survive pod restarts."
