# Deploy BanjirWatch to Railway (Free)

## Prerequisites

1. [Railway account](https://railway.app/) - Sign up with GitHub
2. [GitHub repository](https://github.com/new) for your code

## Deployment Steps

### 1. Push Code to GitHub

```bash
git init
git add .
git commit -m "Initial commit"
git branch -M main
git remote add origin https://github.com/YOUR_USERNAME/BanjirWatch.git
git push -u origin main
```

### 2. Create Railway Project

**Option A: Via Railway Dashboard (Recommended)**

1. Go to [Railway Dashboard](https://railway.app/dashboard)
2. Click **"New Project"**
3. Select **"Deploy from GitHub repo"**
4. Choose your `BanjirWatch` repository
5. Railway will auto-detect the `railway.toml` and `Dockerfile`

**Option B: Via Railway CLI**

```bash
# Install Railway CLI
npm install -g @railway/cli

# Login
railway login

# Link to project (create new if doesn't exist)
cd BanjirWatch
railway link

# Deploy
railway up
```

### 3. Add Volume for Database Persistence

1. In Railway dashboard, click your service
2. Go to **"Volumes"** tab
3. Click **"New Volume"**
4. Mount path: `/data`
5. Size: 1GB (free tier)

### 4. Configure Environment Variables (Optional)

In Railway dashboard → Variables:

| Variable | Value | Description |
|----------|-------|-------------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Already set in railway.toml |
| `DATABASE_PATH` | `/data/BanjirWatch.db` | Already set in railway.toml |

### 5. Get Your URL

After deployment, Railway provides a URL like:
```
https://banjirwatch-production.up.railway.app
```

## Free Tier Limits

| Resource | Limit |
|----------|-------|
| Execution hours | 500 hours/month |
| RAM | 1GB |
| Disk | 1GB |
| Egress | 100GB/month |

> 💡 **Tip**: The free tier sleeps after inactivity. First request may take 10-30 seconds to wake up.

## Auto-Deploy from GitHub

The included GitHub Actions workflow (`.github/workflows/deploy-railway.yml`) automatically deploys on every push to `main`.

**Setup:**
1. Go to Railway dashboard → your project
2. Click **"Settings"** → **"Tokens"**
3. Create a new token
4. In GitHub repo → **Settings** → **Secrets and variables** → **Actions**
5. Add `RAILWAY_TOKEN` with your token

## Troubleshooting

### App won't start
```bash
# Check logs in Railway dashboard or:
railway logs
```

### Database issues
- Ensure volume is mounted at `/data`
- Check `DATABASE_PATH` env var is set to `/data/BanjirWatch.db`

### Port binding errors
- Railway sets `PORT` env variable automatically
- The app reads this in `Program.cs`

## Custom Domain (Optional)

1. Railway dashboard → your service → **Settings** → **Domains**
2. Click **"Generate Domain"** for free `*.railway.app` subdomain
3. Or add your custom domain

## Monitoring

- **Railway Dashboard**: Metrics, logs, and deployments
- **Health Check**: App responds to `GET /` for Railway health checks
