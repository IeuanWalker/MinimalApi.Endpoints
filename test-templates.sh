#!/bin/bash
# test-templates.sh
# Bash script to test MinimalApi.Endpoints Template Pack

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
GRAY='\033[0;37m'
NC='\033[0m' # No Color

# Parse arguments
SKIP_BUILD=false
SKIP_CLEANUP=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --skip-build)
            SKIP_BUILD=true
            shift
 ;;
        --skip-cleanup)
            SKIP_CLEANUP=true
            shift
            ;;
        *)
            echo "Unknown option: $1"
       exit 1
            ;;
    esac
done

echo -e "${CYAN}============================================${NC}"
echo -e "${CYAN} MinimalApi.Endpoints Template Pack Tester${NC}"
echo -e "${CYAN}============================================${NC}"
echo ""

# Step 1: Build and Pack
if [ "$SKIP_BUILD" = false ]; then
    echo -e "${YELLOW}[1/7] Building and packing template...${NC}"
    dotnet pack src/IeuanWalker.MinimalApi.Endpoints.TemplatePack/IeuanWalker.MinimalApi.Endpoints.TemplatePack.csproj -c Release
    echo -e "${GREEN}? Build successful${NC}"
    echo ""
else
    echo -e "${YELLOW}[1/7] Skipping build (using existing package)${NC}"
  echo ""
fi

# Step 2: Uninstall previous version
echo -e "${YELLOW}[2/7] Uninstalling previous version (if any)...${NC}"
dotnet new uninstall IeuanWalker.MinimalApi.Endpoints.TemplatePack 2>/dev/null || true
echo -e "${GREEN}? Cleanup complete${NC}"
echo ""

# Step 3: Install template
echo -e "${YELLOW}[3/7] Installing template...${NC}"
PACKAGE_PATH="src/IeuanWalker.MinimalApi.Endpoints.TemplatePack/bin/Release/IeuanWalker.MinimalApi.Endpoints.TemplatePack.1.0.0.nupkg"
dotnet new install "$PACKAGE_PATH"
echo -e "${GREEN}? Template installed${NC}"
echo ""

# Step 4: Create test directory
echo -e "${YELLOW}[4/7] Creating test directory...${NC}"
TEST_DIR="test-output"
rm -rf "$TEST_DIR"
mkdir -p "$TEST_DIR"
echo -e "${GREEN}? Test directory created: $TEST_DIR${NC}"
echo ""

# Step 5: Run test cases
echo -e "${YELLOW}[5/7] Running test cases...${NC}"
echo ""

PASSED=0
FAILED=0

# Test Case 1: GET with Request and Response
echo -e "  ${CYAN}Testing: GET with Request and Response${NC}"
if dotnet new endpoint -n GetUserById -ns TestApi.Endpoints.Users.GetById -m GET -r "/api/users/{id}" -o test-output/GetUserById >/dev/null 2>&1; then
    if [ -f "test-output/GetUserById/GetUserByIdEndpoint.cs" ] && \
       [ -f "test-output/GetUserById/RequestModel.cs" ] && \
       [ -f "test-output/GetUserById/ResponseModel.cs" ]; then
        echo -e "  ${GREEN}? PASSED: All files generated correctly${NC}"
        ((PASSED++))
    else
        echo -e "  ${RED}? FAILED: Missing expected files${NC}"
   ((FAILED++))
    fi
else
    echo -e "  ${RED}? FAILED: Template execution failed${NC}"
    ((FAILED++))
fi
echo ""

# Test Case 2: POST with Validator
echo -e "  ${CYAN}Testing: POST with Validator${NC}"
if dotnet new endpoint -n CreateUser -ns TestApi.Endpoints.Users.Create -m POST -r "/api/users" --validator true -o test-output/CreateUser >/dev/null 2>&1; then
    if [ -f "test-output/CreateUser/CreateUserEndpoint.cs" ] && \
       [ -f "test-output/CreateUser/RequestModel.cs" ] && \
       [ -f "test-output/CreateUser/ResponseModel.cs" ] && \
       [ -f "test-output/CreateUser/RequestModelValidator.cs" ]; then
        echo -e "  ${GREEN}? PASSED: All files generated correctly${NC}"
        ((PASSED++))
    else
    echo -e "  ${RED}? FAILED: Missing expected files${NC}"
        ((FAILED++))
    fi
else
    echo -e "  ${RED}? FAILED: Template execution failed${NC}"
    ((FAILED++))
fi
echo ""

# Test Case 3: DELETE without Response
echo -e "  ${CYAN}Testing: DELETE without Response${NC}"
if dotnet new endpoint -n DeleteUser -ns TestApi.Endpoints.Users.Delete -m DELETE -r "/api/users/{id}" --withResponse false -o test-output/DeleteUser >/dev/null 2>&1; then
    if [ -f "test-output/DeleteUser/DeleteUserEndpoint.cs" ] && \
       [ -f "test-output/DeleteUser/RequestModel.cs" ] && \
       [ ! -f "test-output/DeleteUser/ResponseModel.cs" ]; then
        echo -e "  ${GREEN}? PASSED: All files generated correctly${NC}"
     ((PASSED++))
    else
 echo -e "  ${RED}? FAILED: Unexpected files generated${NC}"
 ((FAILED++))
    fi
else
    echo -e "  ${RED}? FAILED: Template execution failed${NC}"
    ((FAILED++))
fi
echo ""

# Test Case 4: GET without Request
echo -e "  ${CYAN}Testing: GET without Request${NC}"
if dotnet new endpoint -n GetAllUsers -ns TestApi.Endpoints.Users.GetAll -m GET -r "/api/users" --withRequest false -o test-output/GetAllUsers >/dev/null 2>&1; then
    if [ -f "test-output/GetAllUsers/GetAllUsersEndpoint.cs" ] && \
    [ ! -f "test-output/GetAllUsers/RequestModel.cs" ] && \
       [ -f "test-output/GetAllUsers/ResponseModel.cs" ]; then
        echo -e "  ${GREEN}? PASSED: All files generated correctly${NC}"
    ((PASSED++))
    else
        echo -e "  ${RED}? FAILED: Unexpected files generated${NC}"
        ((FAILED++))
    fi
else
    echo -e "  ${RED}? FAILED: Template execution failed${NC}"
    ((FAILED++))
fi
echo ""

# Test Case 5: Simple Endpoint
echo -e "  ${CYAN}Testing: Simple Endpoint (No Request/Response)${NC}"
if dotnet new endpoint -n Ping -ns TestApi.Endpoints.Health.Ping -m GET -r "/api/health/ping" --withRequest false --withResponse false -o test-output/Ping >/dev/null 2>&1; then
    if [ -f "test-output/Ping/PingEndpoint.cs" ] && \
       [ ! -f "test-output/Ping/RequestModel.cs" ] && \
  [ ! -f "test-output/Ping/ResponseModel.cs" ]; then
  echo -e "  ${GREEN}? PASSED: All files generated correctly${NC}"
     ((PASSED++))
    else
 echo -e "  ${RED}? FAILED: Unexpected files generated${NC}"
        ((FAILED++))
    fi
else
    echo -e "  ${RED}? FAILED: Template execution failed${NC}"
    ((FAILED++))
fi
echo ""

# Test Case 6: Endpoint with Group
echo -e "  ${CYAN}Testing: Endpoint with Group${NC}"
if dotnet new endpoint -n GetUserProfile -ns TestApi.Endpoints.Users.GetProfile -m GET -r "/{id}/profile" --group UserEndpointGroup -o test-output/GetUserProfile >/dev/null 2>&1; then
    if [ -f "test-output/GetUserProfile/GetUserProfileEndpoint.cs" ] && \
       grep -q "Group<UserEndpointGroup>" "test-output/GetUserProfile/GetUserProfileEndpoint.cs"; then
        echo -e "  ${GREEN}? PASSED: All files generated correctly${NC}"
     ((PASSED++))
    else
      echo -e "  ${RED}? FAILED: Group configuration not found${NC}"
        ((FAILED++))
    fi
else
    echo -e "  ${RED}? FAILED: Template execution failed${NC}"
    ((FAILED++))
fi
echo ""

if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}Test Results: $PASSED passed, $FAILED failed${NC}"
else
    echo -e "${YELLOW}Test Results: $PASSED passed, $FAILED failed${NC}"
fi
echo ""

# Step 6: List generated files
echo -e "${YELLOW}[6/7] Generated files:${NC}"
find "$TEST_DIR" -type f | sort | while read -r file; do
    echo -e "  ${GRAY}- $file${NC}"
done
echo ""

# Step 7: Completion
echo -e "${YELLOW}[7/7] Test complete!${NC}"
echo ""
echo -e "${CYAN}Review generated files in: $TEST_DIR${NC}"
echo ""

if [ "$SKIP_CLEANUP" = false ]; then
    echo -e "${YELLOW}Cleanup commands:${NC}"
    echo -e "  ${GRAY}• Remove test output:   rm -rf $TEST_DIR${NC}"
    echo -e "  ${GRAY}• Uninstall template:   dotnet new uninstall IeuanWalker.MinimalApi.Endpoints.TemplatePack${NC}"
 echo ""
fi

if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}? All tests passed!${NC}"
    exit 0
else
    echo -e "${RED}? Some tests failed. Please review the output above.${NC}"
    exit 1
fi
