import * as pulumi from "@pulumi/pulumi";
import * as resources from "@pulumi/azure-native/resources";
import * as web from "@pulumi/azure-native/web";

// resource group that contains all recources
const resourceGroup = new resources.ResourceGroup("rg-optilifts", {
    location: "SouthAfricaNorth",
});

// setup for the backend server resources
const appServicePlan = new web.AppServicePlan("asp-optilifts-backend", {
    resourceGroupName: resourceGroup.name,
    kind: "Linux",
    reserved: true, 
    sku: {
        name: "F1",  //60 minutes of computer time per day, we can change it later on
        tier: "Free",
    },
});

// resource for the webapp
const frontendApp = new web.WebApp("app-optilifts-frontend", {
    resourceGroupName: resourceGroup.name,
    serverFarmId: appServicePlan.id,
    siteConfig: {
        linuxFxVersion: "NODE|20-lts", 
    },
});

// resource for the core API
const backendApi = new web.WebApp("app-optilifts-core", {
    resourceGroupName: resourceGroup.name,
    serverFarmId: appServicePlan.id,
    siteConfig: {
        linuxFxVersion: "DOTNETCORE|8.0", 
    },
});

export const frontendUrl = frontendApp.defaultHostName;
export const backendUrl = backendApi.defaultHostName;