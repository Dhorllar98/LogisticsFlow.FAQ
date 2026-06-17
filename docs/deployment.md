# Deployment Guide — LogisticsFlow FAQ Assistant

## Architecture

- **Backend** (.NET 8 Web API): deployed to Railway or Render
- **Frontend** (React + Vite): deployed to Vercel
- **Knowledge base**: bundled JSON file, deployed alongside the backend

## Required Environment Variables (Backend)

| Variable | Purpose |
|---|---|
| `ANTHROPIC_API_KEY` | Claude API authentication |
| `AllowedOrigins` | Comma-separated list of permitted frontend origins for CORS |
| `ASPNETCORE_ENVIRONMENT` | `Production` on deployed environments |

These are set directly in the hosting platform's dashboard — never in a
committed `appsettings.json`.

## Required Environment Variables (Frontend)

| Variable | Purpose |
|---|---|
| `VITE_API_BASE_URL` | URL of the deployed backend, e.g. `https://logisticsflow-api.up.railway.app` |

## Local Development

Backend: