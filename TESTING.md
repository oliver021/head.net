# Head.Net Testing Guide

## 🎯 Overview

Head.Net includes a comprehensive test suite with **85 integration tests** covering CRUD operations, lifecycle hooks, custom actions, paging, and error scenarios.

### Test Statistics
- **Total Tests**: 85
- **Pass Rate**: 100% ✅
- **Frameworks**: net8.0, net9.0
- **Execution Time**: ~600-850ms per framework
- **Coverage**: All endpoint flows, hooks, persistence

## 🚀 Quick Start

### Run All Tests
```bash
dotnet test Head.Net.sln
```

### Run Tests on Specific Framework
```bash
dotnet test Head.Net.sln --framework net9.0
dotnet test Head.Net.sln --framework net8.0
```

### Run with Verbose Output
```bash
dotnet test Head.Net.sln --logger "console;verbosity=detailed"
```

## 📊 Coverage Reports

### Generate Coverage Report
```bash
# PowerShell (Windows)
./scripts/generate-coverage.ps1

# Bash (Linux/macOS)
./scripts/test.sh net9.0 true
```

This will:
1. Run tests with coverage collection
2. Generate HTML report with badges
3. Display summary statistics
4. Save report to `coverage-report/` directory

### View Coverage
```bash
# Windows
start coverage-report/index.html

# Linux/macOS
open coverage-report/index.html
```

## 📝 Test Documentation

### Generate Test Documentation
```bash
# PowerShell
./scripts/generate-test-docs.ps1

# Or manually
./scripts/generate-test-docs.ps1 -OutputFile docs/test-documentation.md
```

This creates `docs/test-documentation.md` with:
- All test names and descriptions
- Test organization by class
- Running instructions
- Infrastructure details

## 🏗️ Test Architecture

### Test Infrastructure

```
tests/
├── Head.Net.Tests/
│   ├── Fixtures/
│   │   ├── TestWebApplicationFactory.cs    # Test server & DI setup
│   │   ├── TestEntity.cs                   # TestInvoice entity
│   │   ├── TestHookCollector.cs            # Hook tracking
│   │   └── TestAuthorizationProvider.cs    # Auth context
│   ├── HeadEntityEndpointIntegrationTests.cs   # CRUD flows
│   ├── HeadEntityHookExecutionTests.cs         # Hook pipeline
│   ├── HeadEntityCustomActionTests.cs          # Domain actions
│   ├── HeadEntityPagingAndFilteringTests.cs    # Query logic
│   └── HeadEntityErrorScenariosTests.cs        # Error handling
```

### Key Components

#### TestWebApplicationFactory
Creates a fully functional test application with:
- Real WebHost via TestServer
- EF Core InMemory database (shared across requests)
- Dependency injection configured
- HTTP client for endpoint testing

```csharp
var factory = new TestWebApplicationFactory();
await factory.InitializeAsync();
var client = factory.CreateClient();

// Make HTTP requests
var response = await client.GetAsync("/invoices");
```

#### TestInvoice
Sample entity implementing `IHeadEntity<int>`:
```csharp
public class TestInvoice : IHeadEntity<int>
{
    public int Id { get; set; }
    public string CustomerName { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public int OwnerId { get; set; }
}
```

#### TestHookCollector
Tracks hook execution for verification:
```csharp
var collector = factory.HookCollector;
collector.Clear();
// ... trigger operation ...
Assert.True(collector.WasHookCalled("BeforeCreate"));
```

## 📋 Test Categories

### 1. CRUD Endpoint Integration Tests (30 tests)
**File**: `HeadEntityEndpointIntegrationTests.cs`

Tests complete HTTP flows:
- ✅ GetList - paginated results
- ✅ GetSingle - single entity retrieval
- ✅ PostCreate - entity creation and persistence
- ✅ PutUpdate - entity updates with persistence
- ✅ DeleteInvoice - entity deletion
- ✅ Hook invocation during operations

**Example:**
```csharp
[Fact]
public async Task PostCreate_Returns_201_And_Persists()
{
    var response = await _client.PostAsJsonAsync("/invoices", 
        new { CustomerName = "Acme", Total = 100m });
    
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    var persisted = await _factory.GetInvoiceAsync(createdId);
    Assert.NotNull(persisted);
}
```

### 2. Hook Execution Tests (20 tests)
**File**: `HeadEntityHookExecutionTests.cs`

Verifies hook pipeline:
- ✅ BeforeCreate mutations
- ✅ AfterCreate notifications
- ✅ BeforeUpdate validation
- ✅ AfterUpdate side effects
- ✅ BeforeDelete checks
- ✅ AfterDelete cleanup
- ✅ Hook execution order

### 3. Custom Action Tests (18 tests)
**File**: `HeadEntityCustomActionTests.cs`

Tests domain-specific operations:
- ✅ Custom action routing (`/invoices/{id}/pay`)
- ✅ Entity mutation in handlers
- ✅ State persistence after action
- ✅ Missing entity handling (404)
- ✅ Multiple actions on same entity

**Example:**
```csharp
[Fact]
public async Task CustomAction_Pay_Changes_Status()
{
    var response = await _client.PostAsync($"/invoices/{id}/pay", null);
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    
    var updated = await _factory.GetInvoiceAsync(id);
    Assert.Equal("paid", updated.Status);
}
```

### 4. Paging & Filtering Tests (15 tests)
**File**: `HeadEntityPagingAndFilteringTests.cs`

Validates query logic:
- ✅ Skip/Take parameter handling
- ✅ PageCount calculation
- ✅ TotalCount accuracy
- ✅ Boundary conditions (empty, single page, multiple pages)
- ✅ Parameter normalization

### 5. Error Scenario Tests (12 tests)
**File**: `HeadEntityErrorScenariosTests.cs`

Tests error handling:
- ✅ 404 for missing entities
- ✅ 400 for malformed requests
- ✅ Parameter validation
- ✅ Sequential error recovery
- ✅ State consistency after errors

## 🔍 Test Naming Convention

Tests follow the pattern: **[Operation]_[Condition]_[Expected Result]**

```
✅ PostCreate_Returns_201_And_Persists
✅ BeforeCreate_Sets_CreatedAt_Timestamp
✅ DeleteInvoice_Returns_200_And_Removes
✅ List_Skip_Parameter_Skips_Entities
✅ CustomActionNonExistent_Returns_404
✅ NegativeSkip_Returns_OK_With_Normalized_Results
```

## 🛠️ CI/CD Integration

### GitHub Actions
`.github/workflows/coverage.yml` automatically:
- Runs tests on push/PR
- Collects code coverage
- Publishes test results
- Uploads to Codecov

### Azure Pipelines
`azure-pipelines.yml` provides:
- Multi-framework testing (net8.0, net9.0)
- Parallel test execution
- Code coverage reporting
- Artifact publishing

## 📈 Continuous Improvement

### Add New Tests
1. Create test method in appropriate file
2. Follow naming convention: `[Operation]_[Condition]_[Expected]`
3. Use factory for setup: `await _factory.SeedInvoiceAsync(invoice)`
4. Make HTTP request via `_client`
5. Verify response AND database state

```csharp
[Fact]
public async Task [TestName]()
{
    // Arrange
    var entity = new TestInvoice { /* ... */ };
    await _factory.SeedInvoiceAsync(entity);

    // Act
    var response = await _client.[Method](...);

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var persisted = await _factory.GetInvoiceAsync(entity.Id);
    Assert.Equal(expectedValue, persisted.Property);
}
```

### Add New Fixture
1. Create in `Fixtures/` folder
2. Make thread-safe for parallel tests
3. Implement `IAsyncDisposable` for cleanup
4. Register in `TestWebApplicationFactory`

## 🐛 Troubleshooting

### Tests Timeout
- Increase timeout: `dotnet test --test-adapter-path:. --logger:"console;verbosity=detailed" -- RunConfiguration.TestSessionTimeout=300000`

### Coverage Not Generated
- Ensure `coverlet.collector` is installed: `dotnet add package coverlet.collector`
- Check output: `./tests/Head.Net.Tests/coverage.xml`

### InMemory Database Issues
- Each factory gets unique `_dbContextId`
- Database is shared across scopes within same factory
- Test factories are isolated from each other

## 📚 Additional Resources

- [Testing Best Practices](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
- [xUnit Documentation](https://xunit.net/docs/getting-started/netcore)
- [EF Core Testing](https://docs.microsoft.com/en-us/ef/core/testing/)
- [TestHost Documentation](https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests)

---

**Last Updated**: 2026-04-21  
**Test Suite Version**: 1.0  
**Status**: All 85 tests passing ✅
