#!/usr/bin/env bash
# Arranca SIPITEX accesible desde Codespaces (bind 0.0.0.0)
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT/src/Sipitex.Web"

echo "==> SIPITEX en http://0.0.0.0:5240"
echo "==> En Ports: 5240 debe ser Public"
echo "==> Abre: ...-5240.app.github.dev/Account/Login"
echo "==> Login: admin@sipitex.test / Admin123!"
echo

# Forzar URLs aunque launchSettings falle
export ASPNETCORE_URLS="http://0.0.0.0:5240"
exec dotnet run --launch-profile http --urls "http://0.0.0.0:5240"
