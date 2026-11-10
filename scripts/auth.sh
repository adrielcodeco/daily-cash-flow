#!/usr/bin/env bash
# Local identity helpers. Each call creates a client for a new merchant in Keycloak.
KEYCLOAK=${KEYCLOAK:-http://localhost:18083}
REALM=daily-cash-flow
ALL_SCOPES='"api-audience","entries.read","entries.write","balances.read"'

admin_token() {
  curl -sf "$KEYCLOAK/realms/$REALM/protocol/openid-connect/token" \
    -d grant_type=client_credentials -d client_id=local-admin -d client_secret=local-only |
    python3 -c "import json,sys; print(json.load(sys.stdin)['access_token'])"
}

new_client() { # merchant [scopes]
  local client="merchant-$1"
  local body="{\"clientId\":\"$client\",\"secret\":\"local-only\",\"serviceAccountsEnabled\":true,\"publicClient\":false,\"standardFlowEnabled\":false,\"defaultClientScopes\":[${2:-$ALL_SCOPES}],\"protocolMappers\":[{\"name\":\"merchant\",\"protocol\":\"openid-connect\",\"protocolMapper\":\"oidc-hardcoded-claim-mapper\",\"config\":{\"claim.name\":\"merchant_id\",\"claim.value\":\"$1\",\"jsonType.label\":\"String\",\"access.token.claim\":\"true\"}}]}"
  curl -sf -o /dev/null -X POST "$KEYCLOAK/admin/realms/$REALM/clients" \
    -H "Authorization: Bearer $(admin_token)" -H 'Content-Type: application/json' -d "$body"
  curl -sf "$KEYCLOAK/realms/$REALM/protocol/openid-connect/token" \
    -d grant_type=client_credentials -d client_id="$client" -d client_secret=local-only |
    python3 -c "import json,sys; print(json.load(sys.stdin)['access_token'])"
}
