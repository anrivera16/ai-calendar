# AI Calendar Manager - Deployment Guide

## Required Environment Variables

### Database Configuration
- `DATABASE_URL` - PostgreSQL connection string
  - Local: `postgresql://postgres:password@localhost:5432/ai_calendar_manager`
  - Cloud: `postgresql://user:pass@host:port/database?sslmode=require`

### Google OAuth Configuration
- `GOOGLE_CLIENT_ID` - Your Google OAuth Client ID from Google Console
- `GOOGLE_CLIENT_SECRET` - Your Google OAuth Client Secret (keep secure!)
- `GOOGLE_REDIRECT_URI` - OAuth callback URL
  - Local: `http://localhost:8080/api/auth/callback`
  - Production: `https://your-domain.com/api/auth/callback`

### Security
- `ENCRYPTION_KEY` - Base64 encoded 32-byte key for token encryption
  - Generate with: `openssl rand -base64 32`
  - Example: `GHyJHCtO4NysgnfeC3lkHpyxTJfkY9b/odNuPCBkDWU=`

### Optional Configuration
- `FRONTEND_URL` - Your frontend application URL for CORS
  - Default: `http://localhost:3000`
  - Production: `https://your-frontend-domain.com`
- `API_PORT` - Port for the API to run on (default: 8080)

## Google OAuth Setup

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select existing one
3. Enable the Calendar API and Google+ API
4. Go to "Credentials" → "Create Credentials" → "OAuth 2.0 Client IDs"
5. Set application type to "Web application"
6. Add authorized redirect URIs:
   - Local: `http://localhost:8080/api/auth/callback`
   - Production: `https://your-domain.com/api/auth/callback`
7. Copy the Client ID and Client Secret to your environment variables

## Deployment Options

### Option 1: Railway (Recommended)

1. Install Railway CLI: `npm install -g @railway/cli`
2. Login: `railway login`
3. Create new project: `railway new`
4. Add PostgreSQL: `railway add postgresql`
5. Set environment variables in Railway dashboard or CLI:
   ```bash
   railway variables set GOOGLE_CLIENT_ID=your_client_id
   railway variables set GOOGLE_CLIENT_SECRET=your_client_secret
   railway variables set ENCRYPTION_KEY=your_encryption_key
   railway variables set GOOGLE_REDIRECT_URI=https://your-app.railway.app/api/auth/callback
   ```
6. Deploy: `railway up`

### Option 2: Azure Container Apps

1. Build and push container:
   ```bash
   az acr build --registry your-registry --image ai-calendar-api:latest ./CalendarManager.API
   ```
2. Update `azure-containerapp.yaml` with your values
3. Deploy:
   ```bash
   az containerapp create --resource-group your-rg --environment your-env --yaml azure-containerapp.yaml
   ```

### Option 3: Docker Compose (VPS/Self-hosted)

1. Create `.env` file with required variables
2. Run: `docker-compose up -d --profile production`

## Environment Variables Template

Create a `.env` file (don't commit this!):

```env
# Database
DATABASE_URL=postgresql://user:pass@host:port/database

# Google OAuth
GOOGLE_CLIENT_ID=your-client-id.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=your-client-secret
GOOGLE_REDIRECT_URI=https://your-domain.com/api/auth/callback

# Security
ENCRYPTION_KEY=your-base64-encoded-key

# Optional
FRONTEND_URL=https://your-frontend-domain.com
API_PORT=8080
```

## SSL/HTTPS Requirements

- Google OAuth requires HTTPS in production
- Most cloud platforms provide automatic HTTPS
- For self-hosted: Use reverse proxy (nginx) with Let's Encrypt

## Health Checks

The API provides a health endpoint at `/health` that checks:
- Database connectivity
- General application health

Use this for:
- Load balancer health checks
- Container orchestration health probes
- Monitoring systems

## Security Considerations

1. **Never commit secrets** - Use environment variables
2. **Encrypt tokens** - Tokens are automatically encrypted before database storage
3. **HTTPS only** - Required for OAuth and production use
4. **CORS configuration** - Whitelist only trusted frontend domains
5. **Database security** - Use SSL connections in production

## Troubleshooting

### Common Issues

1. **OAuth callback mismatch**
   - Ensure `GOOGLE_REDIRECT_URI` matches Google Console settings
   - Check both HTTP/HTTPS and domain spelling

2. **Database connection failed**
   - Verify `DATABASE_URL` format
   - Check network connectivity and firewall rules

3. **Token encryption errors**
   - Ensure `ENCRYPTION_KEY` is valid base64
   - Key must be consistent across deployments

4. **CORS errors**
   - Add frontend domain to `FRONTEND_URL`
   - Check browser developer tools for exact error

### Logs and Monitoring

- Application logs show detailed error messages
- Health endpoint provides quick status checks
- Monitor database connections and OAuth token refresh rates