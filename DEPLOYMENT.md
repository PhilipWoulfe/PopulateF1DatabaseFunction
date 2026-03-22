# Deployment Runbook

This is the living deployment instruction file for F1Competition.

Use it as the source of truth for:
- new VM setup
- required secrets
- environment files
- deployment commands
- rollback steps
- migration status from LXC to dedicated VM

Update this file whenever deployment behavior, secrets, infrastructure, or CI/CD wiring changes.

## Status

Current state:
- Runtime is still LXC-based.
- Docker Compose is the runtime source of truth.
- GHCR image build and tag promotion already exist.
- Cloudflare public ingress exists.
- Tailscale is the planned private deploy/admin path.
- VM deployment workflow is not wired yet.

Target state:
- App runs only inside a dedicated VM.
- CI deploys automatically to the VM.
- No manual SSH is required for normal deployment.
- No app Docker processes leak onto the Proxmox host.

## Runtime Topology

Public path:
1. Cloudflare
2. Tunnel / edge access
3. VM-hosted `f1-web` and `f1-api`

Private operator path:
1. Tailscale
2. VM private access for deploy/admin/debug

Database path:
1. `postgres` runs inside the same VM runtime stack for this phase

## Deployment Ownership

Default migration owner:
1. `f1-api`

Non-default migration owner:
1. `f1-data-sync-worker`

Defaults today:
1. `DB_AUTO_MIGRATE=true`
2. `DATA_SYNC_AUTO_MIGRATE=false`

Do not enable both by default in the same environment unless there is a deliberate reason.

## Required Repository Files

Primary runtime files:
1. [docker-compose.yml](docker-compose.yml)
2. [.env.example](.env.example)
3. [scripts/deploy-preflight.sh](scripts/deploy-preflight.sh)
4. [.github/workflows/docker-build.yaml](.github/workflows/docker-build.yaml)

Operational docs:
1. [README.md](README.md)
2. [HANDOVER.MD](HANDOVER.MD)
3. [DEPLOYMENT.md](DEPLOYMENT.md)

## New VM Checklist

Provisioning:
1. Create a dedicated Ubuntu 24.04 LTS VM.
2. Assign a static IP.
3. Allocate persistent storage for Docker volumes and logs.
4. Install all current security updates.

Packages:
1. Install Docker Engine.
2. Install Docker Compose plugin.
3. Install Tailscale.
4. Install git.
5. Install curl.

Suggested bootstrap commands:

```bash
sudo apt-get update
sudo apt-get install -y ca-certificates curl gnupg git

sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
sudo chmod a+r /etc/apt/keyrings/docker.gpg

echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
  $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | \
  sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

sudo apt-get update
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
sudo systemctl enable docker
sudo systemctl start docker

curl -fsSL https://tailscale.com/install.sh | sh
sudo tailscale up
```

Users and access:
1. Create a dedicated deploy user.
2. Add deploy user to `docker` group.
3. Disable password SSH.
4. Prefer no public SSH ingress.
5. Use Tailscale for admin/deploy access.

Filesystem:
1. Create `/opt/f1competition` for runtime files.
2. Create `/mnt/f1-logs` for API logs.
3. Ensure the deploy user can write there.

Suggested commands:

```bash
sudo useradd -m -s /bin/bash deploy
sudo usermod -aG docker deploy
sudo mkdir -p /opt/f1competition /mnt/f1-logs
sudo chown -R deploy:deploy /opt/f1competition /mnt/f1-logs
```

Firewall:
1. Allow Tailscale/private admin access.
2. Allow only the public ports needed by Cloudflare tunnel path.
3. Do not expose SSH publicly unless there is a temporary emergency need.

## Runtime Files on VM

Recommended VM layout:

```text
/opt/f1competition/
  docker-compose.yml
  .env
  scripts/
```

Log path:

```text
/mnt/f1-logs
```

## Required Environment Values

These are the important runtime keys expected by the current compose setup.

Core:
1. `TAG`
2. `CONTAINER_NAME`
3. `HOST_PORT`
4. `HOST_PORT_WEB`
5. `HOST_LOG_PATH`
6. `DEPLOY_MIN_FREE_DISK_MB`

Database:
1. `POSTGRES_DB`
2. `POSTGRES_USER`
3. `POSTGRES_PASSWORD`
4. `POSTGRES_PORT`
5. `DB_AUTO_MIGRATE`

Worker:
1. `DATA_SYNC_INTERVAL_MINUTES`
2. `DATA_SYNC_AUTO_MIGRATE`
3. `DATA_SYNC_HTTP_RETRY_COUNT`
4. `DATA_SYNC_HTTP_RETRY_DELAY_MS`
5. `DATA_SYNC_DEADLINE_MINUTES_BEFORE_START`
6. `DATA_SYNC_JOLPICA_BASE_URL`
7. `DATA_SYNC_CONTINUE_ON_ERROR`

Cloudflare / auth:
1. `CLOUDFLARE_AUDIENCE`
2. `CLOUDFLARE_ISSUER`
3. `ADMIN_GROUP_CLAIM_TYPE`
4. `ADMIN_GROUPS`
5. `ADMIN_EMAILS`
6. `CLOUDFLARE_ENABLE_TEST_SERVICE_TOKEN_FALLBACK`
7. `CLOUDFLARE_TEST_SERVICE_TOKEN_SUBJECT_ALLOWLIST`
8. `CLOUDFLARE_TEST_SERVICE_TOKEN_ADMIN_SUBJECT_ALLOWLIST`
9. `CLOUDFLARE_TEST_SERVICE_TOKEN_EMAIL_DOMAIN`

Optional cloud profile:
1. `COMPOSE_PROFILES=cloud`
2. `TUNNEL_TOKEN`

## GitHub Secrets and Variables

Already used by current workflows:

GitHub Environment `test`:
1. `E2E_BASE_URL` as environment variable
2. `E2E_API_BASE_URL` as environment variable
3. `E2E_RACE_ID` as environment variable
4. `E2E_CF_CLIENT_ID` as secret
5. `E2E_CF_CLIENT_SECRET` as secret

Repository secret already referenced elsewhere:
1. `GC_PAT`

VM deployment workflow configuration:

Production environment variables:
1. `VM_TAILSCALE_HOST`
2. `VM_DEPLOY_USER`
3. `VM_DEPLOY_PATH`
4. `GHCR_USERNAME`

Production environment secrets:
1. `TAILSCALE_OAUTH_CLIENT_ID`
2. `TAILSCALE_OAUTH_SECRET`
3. `VM_SSH_PRIVATE_KEY`
4. `VM_ENV_FILE`
5. `GHCR_READ_TOKEN`

Recommendation:
1. `VM_ENV_FILE` should be the full multi-line production `.env` content.
2. Keep `.env.example` as the contract for required keys.
3. Do not store production secrets in repo or handover notes.
4. `GHCR_READ_TOKEN` should be a package-read token that matches `GHCR_USERNAME`.

## VM Bring-Up Procedure

1. Provision VM.
2. Install Docker, Compose, and Tailscale.
3. Create deploy user and runtime directories.
4. Copy [docker-compose.yml](docker-compose.yml) to `/opt/f1competition`.
5. Create `/opt/f1competition/.env` from `.env.example` with real values.
6. Copy `scripts/deploy-preflight.sh` to `/opt/f1competition/scripts/`.
7. Run preflight.
8. Log in to GHCR if needed.
9. Start stack.

Example first boot commands:

```bash
cd /opt/f1competition
chmod +x scripts/deploy-preflight.sh
./scripts/deploy-preflight.sh .env
docker compose --env-file .env pull
docker compose --env-file .env up -d
```

## Current Manual Deploy Procedure

Use this as the fallback path if the VM deploy workflow fails or is unavailable.

1. Connect to the target over Tailscale.
2. Update the `.env` values only if intentionally changing configuration.
3. Set `TAG` to the desired tag:
   - `test`
   - `stable`
   - `sha-<shortsha>`
4. Run preflight.
5. Pull images.
6. Recreate containers.
7. Run smoke checks.

Commands:

```bash
cd /opt/f1competition
./scripts/deploy-preflight.sh .env
docker compose --env-file .env pull
docker compose --env-file .env up -d
./scripts/deploy-smoke-check.sh .env
docker compose logs --tail=100 f1-api
docker compose logs --tail=100 f1-web
docker compose logs --tail=100 f1-data-sync-worker
```

## Planned Automated Deploy Procedure

Automated production flow implemented in PR2:
1. Push to `main`
2. GitHub Actions builds and publishes images
3. Images are promoted to `test`
4. E2E gate runs against test
5. Production approval promotes images to `stable`
6. `deploy-prod-vm` in [.github/workflows/docker-build.yaml](.github/workflows/docker-build.yaml) connects over Tailscale/private SSH
7. The workflow uploads `docker-compose.yml`, deploy scripts, and environment file to the VM
8. The workflow runs preflight, image pull, `docker compose up -d`, and smoke checks
9. The workflow writes a deployment summary with host, path, tag, and commit SHA

Manual rollback flow implemented in PR2:
1. Run [.github/workflows/rollback-vm.yaml](.github/workflows/rollback-vm.yaml)
2. Provide the target tag, usually `sha-<shortsha>`
3. The workflow uploads the same runtime files, overrides `TAG`, and reruns preflight plus smoke checks

## Smoke Checks

Minimum checks after deploy:
1. `docker compose ps`
2. API responds on `/health`
3. Web root responds
4. Worker is either healthy/running or exited cleanly in one-shot mode
5. Postgres is healthy

Example:

```bash
./scripts/deploy-smoke-check.sh .env
```

Container-level health checks:
1. `f1-api` uses Docker `HEALTHCHECK` against `http://127.0.0.1:8080/health`.
2. `f1-web` uses Docker `HEALTHCHECK` against `http://127.0.0.1/`.
3. `scripts/deploy-smoke-check.sh` verifies compose service state, API health, web reachability, and that the worker is either running or has exited cleanly.

## Rollback Procedure

1. Choose a known-good tag, usually `sha-<shortsha>`.
2. Update `TAG` in `.env`.
3. Run preflight.
4. Pull images.
5. Recreate containers.
6. Repeat smoke checks.

Commands:

```bash
cd /opt/f1competition
sed -i 's/^TAG=.*/TAG=sha-abcdef1/' .env
./scripts/deploy-preflight.sh .env
docker compose --env-file .env pull
docker compose --env-file .env up -d
./scripts/deploy-smoke-check.sh .env
```

## Operational Checks

After cutover to VM, verify:
1. Proxmox host is not running app Docker processes.
2. No host-level veth churn appears during app restarts.
3. Logs are being written under `HOST_LOG_PATH`.
4. Tailscale access works for admin/deploy tasks.
5. Cloudflare public ingress still routes correctly.

## Change Log

2026-03-22:
1. Created this runbook.
2. Added VM bootstrap checklist.
3. Added secrets and env inventory.
4. Added current manual deploy procedure and planned automated deploy target.
5. Added a reusable deploy smoke-check script and documented post-deploy verification.
6. Added production VM deploy and rollback workflow documentation with concrete GitHub secret and variable names.
