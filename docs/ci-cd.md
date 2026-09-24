# StarPlex Cinema CI/CD Documentation

This document describes the continuous integration and deployment processes for the StarPlex Cinema application.

## CI/CD Workflows

### PR Checks (`ci.yml`)
When a pull request is opened targeting the `main` branch, the `CI` workflow is triggered.
It performs the following:
- **Backend Job**: Sets up .NET SDK 8.0, restores dependencies, builds the solution in Release mode, and runs all integration and unit tests using Testcontainers (PostgreSQL). The TRX test results are uploaded as an artifact even if tests fail.
- **Frontend Job**: Uses Node.js 22 to install dependencies, builds the Vite application, and builds the frontend Docker image using the production Nginx configuration. It explicitly verifies that the production image does not contain local Docker Compose proxies (e.g., `http://api:8080`).

No Azure credentials or backend secrets are required for these checks.

### Deployment (`deploy-dev.yml`)
When code is pushed to `main` (e.g., a PR is merged) or triggered manually via `workflow_dispatch`, the deployment workflow runs.
1. It calls the `CI` workflow as a reusable step.
2. If CI succeeds, it authenticates with Azure using OIDC.
3. It builds both Docker images using the full commit SHA as the tag.
4. It pushes the images to Azure Container Registry (`acrstarplex.azurecr.io`).
5. It updates the Azure Container Apps (`starplex-api-dev` and `starplex-web-dev`) with the new image.
6. **Verification**: It polls Azure to ensure the newly created revision for both apps is actively receiving traffic and is marked as healthy. It performs HTTP checks against the backend `/health` endpoint and verifies that the frontend serves the expected HTML and loads its referenced JS assets successfully.

## Repository Variables

The deployment workflow requires the following **Variables** (configured under Settings > Secrets and variables > Actions > Variables):
- `AZURE_CLIENT_ID`: The Client ID of the User-Assigned Managed Identity.
- `AZURE_TENANT_ID`: Your Azure Tenant ID.
- `AZURE_SUBSCRIPTION_ID`: Your Azure Subscription ID.
- `VITE_BACKEND_URL`: The URL of the backend API (e.g., `https://starplex-api-dev.redtree-9d789217.polandcentral.azurecontainerapps.io`).
- `VITE_STRIPE_PUBLIC_KEY`: The Stripe public key used by the frontend checkout.

## OIDC and RBAC Assumptions
We use passwordless OIDC authentication to Azure. It is assumed that:
- The managed identity `id-starplex-github-dev` exists.
- The GitHub OIDC subject is strictly mapped to `repo:VolodymyrZakusylo/Starplex-Cinema:ref:refs/heads/main`.
- The identity has `AcrPush` role on the `acrstarplex` registry.
- The identity has `Container Apps Contributor` role on both Azure Container Apps (`starplex-api-dev` and `starplex-web-dev`).

## Why `NGINX_CONFIG=nginx.prod.conf` is Mandatory
The frontend `nginx.conf` default is tailored for local `docker-compose`. It contains `proxy_pass` directives pointing to `http://api:8080` (the backend container).
In Azure, the frontend runs as a completely standalone app and there is no DNS host named "api". If the default configuration is used, Nginx will crash on startup with `host not found in upstream "api"`.
Therefore, the production image must be built with `--build-arg NGINX_CONFIG=nginx.prod.conf` to serve the SPA statically without proxying.

## Manual Reruns and Inspecting Failures
- **Manual Rerun**: You can manually trigger a deployment from the Actions tab by selecting the "Deploy Dev" workflow and clicking "Run workflow". Note that it is restricted to the `main` branch.
- **Inspecting Failures**: If a deployment fails, first check the GitHub Actions step logs. The verification step generates a Markdown summary (visible on the workflow run summary page) that lists the expected Revisions and the Commit SHA.
If the verification times out or ACA rolls back the revision, you should inspect the Azure Container App Console Logs in the Azure Portal or using `az containerapp logs show` to find application runtime exceptions (e.g. startup migration failures).
