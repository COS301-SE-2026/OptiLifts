import * as pulumi from "@pulumi/pulumi";
import * as resources from "@pulumi/azure-native/resources";
import * as storage from "@pulumi/azure-native/storage";
import * as dbforpostgresql from "@pulumi/azure-native/dbforpostgresql";

import * as containerregistry from "@pulumi/azure-native/containerregistry";
import * as app from "@pulumi/azure-native/app";
import * as namedotcom from "@pulumi/namedotcom";

const stackName = pulumi.getStack();

const rootDomain = "optilifts.app";
const frontendDomain = (stackName === "prod") ? "app.optilifts.app" : "staging-app.optilifts.app";
const backendDomain = (stackName === "prod") ? "api.optilifts.app" : "staging-api.optilifts.app";

const frontendUrl = `https://${frontendDomain}`;
const backendUrl = `https://${backendDomain}`;

//config and secrets
const config = new pulumi.Config();
const postgressPassword = config.requireSecret("postgressPassword");
const jwtSecret = config.requireSecret("jwtSecret");
const dbEncryptionKey = config.requireSecret("dbEncryptionKey");
const devSeeding = config.require("devSeeding");
const jwtExpMin = config.get("jwtExpMin") ?? "1440";
const pgPort = config.get("pgPort") ?? "5432";
const coreApiSentryDsn = config.getSecret("coreApiSentryDsn");
const googleClientId = config.getSecret("googleClientId");
const googleClientSecret = config.getSecret("googleClientSecret");

const domainStage = config.get("domainStage") ?? "none";

const imageTag = process.env.IMAGE_TAG;
if (!imageTag) {
    throw new Error("IMAGE_TAG is not set.");
}

const resourceGroup = new resources.ResourceGroup("rgoptilifts", {
    location: "SouthAfricaNorth"
});

// create azure container registry
const acr = new containerregistry.Registry("acroptilifts", {
    resourceGroupName: resourceGroup.name,
    location: resourceGroup.location,
    sku: {
        // lowest tier, can updgrade if need be
        name: "Basic",
    },
    adminUserEnabled: true,
});

//get credentails for acr so don't have to manually log in
const registryCredentials = containerregistry.listRegistryCredentialsOutput({
    resourceGroupName: resourceGroup.name,
    registryName: acr.name,
});

const acrServer = acr.loginServer;
const acrUsername = registryCredentials.username?.apply(u => {
    if (!u) {
        throw new Error("The ACR username is undefined");
    }

    return u;
});

const acrPassword = registryCredentials.passwords?.apply(p => {
    if (!p || p.length === 0) {
        throw new Error("No password found for the ACR");
    }

    if (!p[0].value) {
        throw new Error("The ACR password value is undefined");
    }

    return p[0].value;
})

// azure storage account for blob
const storageAcc = new storage.StorageAccount("saoptilifts", {
    resourceGroupName: resourceGroup.name,
    location: resourceGroup.location,
    sku: {
        //free tier w/ redundancy
        name: "Standard_LRS",
    },
    kind: "StorageV2",
    allowBlobPublicAccess: true,
});

const profilePicturesContainer = new storage.BlobContainer("bc-profile-pictures", {
    resourceGroupName: resourceGroup.name,
    accountName: storageAcc.name,
    containerName: "profile-pictures",
    publicAccess: storage.PublicAccess.Blob, 
});
const exercisesContainer = new storage.BlobContainer("bc-exercises", {
    resourceGroupName: resourceGroup.name,
    accountName: storageAcc.name,
    containerName: "exercises",
    publicAccess: storage.PublicAccess.Blob, 
});

const storageAccKeys = storage.listStorageAccountKeysOutput({
    resourceGroupName: resourceGroup.name,
    accountName: storageAcc.name,
});

// postgres databse 
// the postgres s
const pgServer = new dbforpostgresql.Server("ps-optilifts", {
    resourceGroupName: resourceGroup.name,
    location: resourceGroup.location,
    sku: {
        //we get 750 hours pm on student package 
        name: "Standard_B1ms",
        //saves credits during downtime for high demand times
        tier: "Burstable",
    },
    version: "15",
    administratorLogin: "optilifts_admin",
    administratorLoginPassword: postgressPassword,
    storage: {
        storageSizeGB: 32,
    },
    backup: {
        backupRetentionDays: 7,
        geoRedundantBackup: "Disabled",
    },
});

const pgDatabase = new dbforpostgresql.Database("db-optilifts", {
    resourceGroupName: resourceGroup.name,
    serverName: pgServer.name,
    databaseName: "optilifts",
});

const pgFirewallConfig = new dbforpostgresql.FirewallRule("pgfw-optilifts", {
    resourceGroupName: resourceGroup.name,
    serverName: pgServer.name,
    // firewall allows connection from any other azure service
    // other methods use a lot of credits, they would still need our db password so is still secure
    startIpAddress: "0.0.0.0",
    endIpAddress: "0.0.0.0",
});


// azure container apps environment
// boundary and netowrk for our 3 container apps
const containerAppEnv = new app.ManagedEnvironment("cae-optilifts", {
    resourceGroupName: resourceGroup.name,
    location: resourceGroup.location,
});

const managedCert = (name: string, subjectName: string) =>
    new app.ManagedCertificate(name, {
        resourceGroupName: resourceGroup.name,
        environmentName: containerAppEnv.name,
        location: resourceGroup.location,
        properties: {
            subjectName,
            domainControlValidation: app.ManagedCertificateDomainControlValidation.CNAME,
        },
    });

const frontendCert = domainStage === "bind" ? managedCert("frontend-cert", frontendDomain) : undefined;
const backendCert = domainStage === "bind" ? managedCert("core-api-cert", backendDomain) : undefined;

const customDomain = (domain: string, cert?: app.ManagedCertificate) => domainStage === "none" ? undefined : [{
    name: domain,
    bindingType: cert ? "SniEnabled" : "Disabled",
    certificateId: cert?.id,
}];

// frontend container app
const frontendApp = new app.ContainerApp("frontend", {
    resourceGroupName: resourceGroup.name,
    managedEnvironmentId: containerAppEnv.id,
    configuration: {
        activeRevisionsMode: "Multiple",
        ingress: {
            external: true, // The frontend must be accessible to users on the internet.
            targetPort: 8080,
            customDomains: customDomain(frontendDomain, frontendCert),
            traffic: [{ latestRevision: true, weight: 100 }],
        },
        registries: [{
            server: acrServer,
            username: acrUsername,
            passwordSecretRef: "acr-password", // NOSONAR
        }],
        secrets: [
            { name: "acr-password", value: acrPassword }
        ],
    },
    template: {
        scale: {
            minReplicas: 0,
            maxReplicas: 2,
        },
        containers: [{
            name: "frontend",
            image: pulumi.interpolate`${acrServer}/optilifts-frontend:${imageTag}`,
            resources: { cpu: 0.5, memory: "1.0Gi" },
            env: [{ name: "NGINX_BACKEND_URL", value: `https://${backendDomain}` }]

        }],
    },
});

// copre-api container app
const coreApiApp = new app.ContainerApp("core-api", {
    resourceGroupName: resourceGroup.name,
    managedEnvironmentId: containerAppEnv.id,
    configuration: {
        activeRevisionsMode: "Multiple",
        ingress: {
            external: true, //give public url
            targetPort: 8080,
            customDomains: customDomain(backendDomain, backendCert),
            traffic: [{ latestRevision: true, weight: 100 }],
        },

        secrets: [
            //make container app secrets so can inject them
            { name: "acr-password", value: acrPassword },
            { name: "jwt-secret", value: jwtSecret },
            { name: "db-encryption-key", value: dbEncryptionKey },
            { name: "postgres-password", value: postgressPassword },
            {
                name: "postgres-connection-string",
                value: pulumi.interpolate`Host=${pgServer.fullyQualifiedDomainName};Port=${pgPort};Database=${pgDatabase.name};Username=optilifts_admin;Password=${postgressPassword};SslMode=VerifyFull;`
            },
            {
                name: "storage-connection-string",
                value: pulumi.interpolate`DefaultEndpointsProtocol=https;AccountName=${storageAcc.name};AccountKey=${storageAccKeys.keys[0].value};EndpointSuffix=core.windows.net`
            },
            { name: "core-api-sentry-dsn", value: coreApiSentryDsn },
            { name: "google-client-id", value: googleClientId },
            { name: "google-client-secret", value: googleClientSecret }
        ],

        registries: [{
            server: acrServer,
            username: acrUsername,
            // sonarqube is wrong, this is how to reference the secret
            passwordSecretRef: "acr-password", // NOSONAR
        }],
    },
    template: {
        scale: {
            minReplicas: 0,
            maxReplicas: 3,
        },
        containers: [{
            name: "core-api",
            image: pulumi.interpolate`${acrServer}/optilifts-core-api:${imageTag}`,
            resources: { cpu: 0.5, memory: "1.0Gi" },
            env: [
                { name: "POSTGRES_HOST", value: pgServer.fullyQualifiedDomainName },
                { name: "POSTGRES_PORT", value: "5432" },
                { name: "POSTGRES_DB", value: pgDatabase.name },
                { name: "POSTGRES_USER", value: "optilifts_admin" },
                { name: "POSTGRES_PASSWORD", secretRef: "postgres-password" },
                { name: "AUTH_COOKIE_SECURE", value: "true" },
                { name: "DEV_SEEDING", value: devSeeding },
                { name: "FRONTEND_ORIGIN", value: frontendUrl },
                { name: "JWT_SECRET", secretRef: "jwt-secret" },
                { name: "JWT_EXP_MINUTES", value: jwtExpMin },
                { name: "DB_ENCRYPTION_KEY", secretRef: "db-encryption-key" },
                { name: "POSTGRES_CONNECTION_STRING", secretRef: "postgres-connection-string" },
                { name: "CONNECTIONSTRINGS__AZURESTORAGE", secretRef: "storage-connection-string" },
                { name: "CORE_API_SENTRY_DSN", secretRef: "core-api-sentry-dsn" },
                { name: "GOOGLE_CLIENT_ID", secretRef: "google-client-id" },
                { name: "GOOGLE_CLIENT_SECRET", secretRef: "google-client-secret" },
                { name: "ASPNETCORE_ENVIRONMENT", value: "Production" }
            ],
            probes: [{
                type: "Startup",
                tcpSocket: {
                    port: 8080,
                },
                initialDelaySeconds: 10,
                periodSeconds: 10,
                failureThreshold: 30, 
            }],
        }],
    },
});

const aiApiApp = new app.ContainerApp("ai-api", {
    resourceGroupName: resourceGroup.name,
    managedEnvironmentId: containerAppEnv.id,
    configuration: {
        activeRevisionsMode: "Multiple",
        ingress: {
            external: false, // can only be accessed by core-api 
            targetPort: 8000,
            traffic: [{ latestRevision: true, weight: 100 }],
        },
        registries: [{
            server: acrServer,
            username: acrUsername,
            passwordSecretRef: "acr-password", // NOSONAR
        }],
        secrets: [
            { name: "acr-password", value: acrPassword },
        ],
    },
    template: {
        scale: {
            minReplicas: 0,
            maxReplicas: 2,
        },
        containers: [{
            name: "ai-api",
            image: pulumi.interpolate`${acrServer}/optilifts-ai-api:${imageTag}`,
            resources: {
                cpu: 0.5,
                memory: "1.0Gi"
            },
        }],
    },
});

const dnsRecord = (name: string, host: string, recordType: string, answer: pulumi.Input<string>) =>
    new namedotcom.Record(name, { domainName: rootDomain, host, recordType, answer });

const hostOf = (domain: string) => domain.slice(0, -(rootDomain.length + 1));
const fqdnOf = (a: app.ContainerApp) => a.configuration.apply(c => c!.ingress!.fqdn!);

dnsRecord("frontend-cname", hostOf(frontendDomain), "CNAME", fqdnOf(frontendApp));
dnsRecord("core-api-cname", hostOf(backendDomain), "CNAME", fqdnOf(coreApiApp));
dnsRecord("frontend-asuid", `asuid.${hostOf(frontendDomain)}`, "TXT", frontendApp.customDomainVerificationId);
dnsRecord("core-api-asuid", `asuid.${hostOf(backendDomain)}`, "TXT", coreApiApp.customDomainVerificationId);

export const frontendAzureUrl = pulumi.interpolate`https://${fqdnOf(frontendApp)}`;
export const acrLoginServer = acr.loginServer;