# Lantern AI — frontend

React + TypeScript (Vite) chat UI for Lantern AI. See the repo root
[README](../README.md) for how this fits together and how to run the full
stack.

## Development

```bash
cp .env.example .env   # points VITE_API_BASE_URL at the backend
npm install
npm run dev
```

## Scripts

- `npm run dev` — Vite dev server with HMR
- `npm run build` — type-check (`tsc -b`) then production build
- `npm run lint` — Oxlint
- `npm run preview` — preview the production build locally
