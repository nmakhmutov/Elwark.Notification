#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT="$SCRIPT_DIR/Notification.Api.csproj"

if [ "$#" -lt 1 ]; then
  echo "Migration name is required."
  read -r -p "Enter migration name: " MIGRATION_NAME
else
  MIGRATION_NAME="$1"
  shift
fi

if [ -z "${MIGRATION_NAME// }" ]; then
  echo "Migration name cannot be empty."
  exit 1
fi

if [ ! -f "$PROJECT" ]; then
  echo "Project not found: $PROJECT"
  exit 1
fi

dotnet ef migrations add "$MIGRATION_NAME" \
  --project "$PROJECT" \
  --startup-project "$PROJECT" \
  --output-dir Infrastructure/Migrations \
  "$@"
