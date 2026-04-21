#!/bin/bash

set -e

COLOR_CYAN='\033[0;36m'
COLOR_GREEN='\033[0;32m'
COLOR_YELLOW='\033[1;33m'
COLOR_RED='\033[0;31m'
NC='\033[0m' # No Color

FRAMEWORK=${1:-net9.0}
GENERATE_COVERAGE=${2:-false}

echo -e "${COLOR_CYAN}🧪 Running Head.Net tests...${NC}"
echo "Framework: $FRAMEWORK"
echo ""

# Run tests
dotnet test Head.Net.sln \
  --framework $FRAMEWORK \
  --logger "console;verbosity=minimal" \
  --logger "trx;LogFileName=test-results.trx"

if [ $? -ne 0 ]; then
  echo -e "${COLOR_RED}❌ Tests failed!${NC}"
  exit 1
fi

echo -e "${COLOR_GREEN}✅ All tests passed!${NC}"
echo ""

# Generate coverage if requested
if [ "$GENERATE_COVERAGE" == "true" ] || [ "$GENERATE_COVERAGE" == "yes" ]; then
  echo -e "${COLOR_CYAN}📊 Generating coverage report...${NC}"

  # Check if ReportGenerator is installed
  if ! command -v reportgenerator &> /dev/null; then
    echo -e "${COLOR_YELLOW}📦 Installing ReportGenerator...${NC}"
    dotnet tool install -g reportgenerator
  fi

  # Run tests with coverage
  dotnet test tests/Head.Net.Tests/Head.Net.Tests.csproj \
    --framework $FRAMEWORK \
    /p:CollectCoverage=true \
    /p:CoverageFormat=opencover \
    /p:CoverageFileName="coverage.xml"

  # Generate report
  reportgenerator \
    -reports:"tests/Head.Net.Tests/coverage.xml" \
    -targetdir:"coverage-report" \
    -reporttypes:"Html;Badges" \
    -classfilters:"+Head.Net.* -Head.Net.Tests.*"

  echo -e "${COLOR_GREEN}✅ Coverage report generated in 'coverage-report/'${NC}"
fi

# Generate test documentation
echo -e "${COLOR_CYAN}📝 Generating test documentation...${NC}"
# This would need a bash/python version of the PowerShell script
# For now, just mention it
echo -e "${COLOR_YELLOW}💡 Run './scripts/generate-test-docs.ps1' to generate documentation${NC}"

echo ""
echo -e "${COLOR_GREEN}✅ Test suite completed successfully!${NC}"
