az acr login --name acrlab007cereleo

docker tag bff-rent-car-local acrlab007cereleo.azurecr.io/bff-rent-car-local:v1

docker push acrlab007cereleo.azurecr.io/bff-rent-car-local:v1

az containerapp env create --name bff-rent-car-local --resource-group LAB007 --location eastus

az containerapp create --name bff-rent-car-local --resource-group LAB007 --environment bff-rent-car-local --image acrlab007cereleo.azurecr.io/bff-rent-car-local:v1 --target-port 3001 --ingress 'external' --registry-server acrlab007cereleo.azurecr.io --registry-username acrlab007cereleo --registry-password BKQCOVuhW9LRS2H91Y7Qb8tNntS+K+P0RO9tpHbsfp+ACRANgBsa

