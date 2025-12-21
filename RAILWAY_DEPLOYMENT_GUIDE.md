# Railway Proxy Server - Deployment Guide

## Overview
This guide explains how to update the Railway proxy server with the new advanced subscription endpoints.

## Current Endpoints (Existing)
- `GET /` - Health check
- `POST /verify` - Calls `verify_authentication` RPC
- `POST /verify-periodic` - Calls `verify_authentication` RPC
- `POST /activate` - Calls `authenticate_user` RPC

## New Endpoints to Add
1. `POST /validate-code` - Validate subscription code without using it
2. `POST /redeem-code` - Redeem/use a subscription code
3. `POST /generate-otp` - Generate OTP for email verification
4. `POST /verify-otp` - Verify OTP code
5. `POST /initiate-device-transfer` - Start device transfer process
6. `POST /complete-device-transfer` - Complete device transfer

## Deployment Steps

### Step 1: Access Railway GitHub Repository
1. Go to: https://github.com/anamsmmy/sr3h-auth-proxy
2. Clone or pull the latest code locally

### Step 2: Update server.js
1. Open `server.js` in your editor
2. Find the end of your existing endpoint handlers (before `app.listen()`)
3. Copy the entire content from `railway-server-updates.js` (starting from the NEW ENDPOINTS comment)
4. Paste into `server.js` before the `app.listen()` call

### Step 3: Code Structure Check
Verify your updated `server.js` has:
```javascript
// Imports and initialization
const express = require('express');
const { createClient } = require('@supabase/supabase-js');
const app = express();

// Middleware
app.use(express.json());

// Environment variables
const SUPABASE_URL = process.env.SUPABASE_URL;
const SUPABASE_SERVICE_KEY = process.env.SUPABASE_SERVICE_KEY;
const supabase = createClient(SUPABASE_URL, SUPABASE_SERVICE_KEY);

// Existing endpoints
// - GET /
// - POST /verify
// - POST /verify-periodic
// - POST /activate

// New endpoints (from railway-server-updates.js)
// - POST /validate-code
// - POST /redeem-code
// - POST /generate-otp
// - POST /verify-otp
// - POST /initiate-device-transfer
// - POST /complete-device-transfer

// Server startup
const PORT = process.env.PORT || 3000;
app.listen(PORT, () => {
  console.log(`Server running on port ${PORT}`);
});
```

### Step 4: Test Locally (Optional)
```bash
# Install dependencies
npm install

# Run server locally
npm start

# Test an endpoint
curl -X POST http://localhost:3000/validate-code \
  -H "Content-Type: application/json" \
  -d '{
    "code": "TESTCODE123",
    "email": "test@example.com",
    "hardware_id": "hw-id-123"
  }'
```

### Step 5: Commit and Push to GitHub
```bash
git add .
git commit -m "feat: add advanced subscription management endpoints"
git push origin main
```

### Step 6: Deploy via Railway
1. Go to https://railway.app
2. Select your project (sr3h-auth-proxy)
3. Railway should auto-detect the push and start deployment
4. Monitor the deployment logs
5. Wait for "✓ Deploy successful" message

### Step 7: Verify Deployment
After deployment completes:

1. Test health endpoint:
```bash
curl https://sr3h-auth-proxy-production.up.railway.app/
```

2. Test one of the new endpoints:
```bash
curl -X POST https://sr3h-auth-proxy-production.up.railway.app/validate-code \
  -H "Content-Type: application/json" \
  -d '{
    "code": "TESTCODE123",
    "email": "test@example.com",
    "hardware_id": "hw-id-123"
  }'
```

3. Check Railway logs:
   - Dashboard → Your Project → Logs
   - Look for request logs and errors

## Troubleshooting

### Issue: "Cannot find module 'supabase'"
**Solution**: Ensure `@supabase/supabase-js` is in `package.json`:
```json
{
  "dependencies": {
    "@supabase/supabase-js": "^2.x.x",
    "express": "^4.x.x"
  }
}
```
Then run: `npm install`

### Issue: "RPC function not found"
**Solution**: 
- Verify SQL migrations were applied to Supabase
- Check that all RPC functions exist in Database → Functions
- Verify function names match exactly (case-sensitive)

### Issue: "401 Unauthorized" errors
**Solution**:
- Check `SUPABASE_SERVICE_KEY` in Railway environment variables
- Verify it's set to your Supabase service role key (not anon key)
- In Railway Dashboard → Variables, confirm the key starts with `eyJ`

### Issue: Deployment fails
**Solution**:
- Check Railway logs: Dashboard → Your Project → Logs
- Ensure `server.js` has no syntax errors
- Try pushing a new commit even if no changes (to trigger fresh deploy)
- Check that port binding uses `process.env.PORT`

## Rollback Plan

If something goes wrong:

1. Go to GitHub and revert the commit:
```bash
git revert HEAD
git push origin main
```

2. Railway will automatically re-deploy with previous version

3. Or deploy specific commit:
   - In Railway Dashboard
   - Go to Deployments
   - Click on previous successful deployment
   - Select "Redeploy"

## Monitoring

After deployment:

1. Check Railway Logs regularly:
   - Dashboard → Your Project → Logs
   - Filter by endpoint to see traffic

2. Monitor error rates:
   - Look for "RPC Error" or "Error:" messages
   - Check response status codes

3. Set up alerts (Optional):
   - Railway Dashboard → Notifications
   - Configure email/Slack alerts for deployment failures

## Environment Variables Reference

Ensure these exist in Railway Variables:
| Variable | Example |
|----------|---------|
| `SUPABASE_URL` | `https://xxxxx.supabase.co` |
| `SUPABASE_SERVICE_KEY` | `eyJ...` (service role key) |
| `PORT` | `3000` (or leave empty for auto-assign) |

## Next Steps

1. ✅ Apply SQL migrations to Supabase
2. ✅ Update Railway server code
3. ✅ Deploy to Railway
4. ✅ Update C# application with new service classes
5. Test end-to-end flow from C# application
6. Monitor logs for any issues

## Support

If you encounter issues:
1. Check Railway logs: https://railway.app/dashboard
2. Verify Supabase RPC functions: https://app.supabase.com
3. Review C# service code in `Services/` folder
4. Check network connectivity from application
