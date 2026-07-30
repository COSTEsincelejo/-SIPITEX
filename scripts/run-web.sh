#!/usr/bin/env bash
# Arranca SIPITEX en Codespaces / local (HTTP :5240)
set -euo pipefail
cd "$(dirname "$0")/../src/Sipitex.Web"
echo "Iniciando SIPITEX en http://localhost:5240 ..."
echo "En Codespaces: Ports → 5240 → Visibility = Public"
echo "Luego abre: ...-5240.app.github.dev/Account/Login"
echo "Login: admin@sipitex.test / Admin123!"
exec dotnet run --launch-profile http --no-launch-profile=false
