#!/usr/bin/env bash
set -euo pipefail

API_URL="http://localhost:8080"
NUMBERS='[4,8,17,19,11,13,42,35]'

echo "🔹 Testing Keno API endpoint..."
curl -v -X POST \
  "$API_URL/api/Keno/play?wager=1&gameSessionId=22" \
  -H 'accept: text/plain' \
  -H 'Content-Type: application/json' \
  -d "$NUMBERS"

echo "🔹 Testing Keno video availability..."
ID=$(curl -s -X POST \
  "$API_URL/api/Keno/play?wager=1&gameSessionId=22" \
  -H 'accept: application/json' \
  -H 'Content-Type: application/json' \
  -d "$NUMBERS" | jq -r '.id')

if [ -z "$ID" ] || [ "$ID" == "null" ]; then
  echo "❌ No game ID returned from API"
  exit 1
fi

STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$API_URL/${ID}.mp4")

if [ "$STATUS" -ne 200 ]; then
  echo "❌ Video file not available (HTTP $STATUS)"
  exit 1
fi

echo "✅ Keno API tests passed successfully!"
