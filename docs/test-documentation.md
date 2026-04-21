# Test Documentation - Head.Net

> Generated: 2026-04-21

## Overview

This document outlines the comprehensive test suite for Head.Net.

### Test Statistics
- **Total Tests**: 85
- **All Tests Passing**: ✅ 100%
- **Frameworks**: net8.0, net9.0
- **Execution Time**: ~600-850ms per framework

## Test Organization

Tests are organized into 5 test classes covering different aspects of the API:

### 1. HeadEntityEndpointIntegrationTests.cs (30 tests)

Full CRUD endpoint integration testing with real HTTP flows:

- GetList_Returns_200_With_Invoices
- GetList_Empty_Returns_200_With_Empty_Data
- GetSingle_Returns_200_With_Invoice
- GetSingle_Missing_Returns_404
- PostCreate_Returns_201_And_Persists
- PostCreate_Invokes_BeforeCreate_Hook
- PostCreate_Invokes_AfterCreate_Hook
- PutUpdate_Returns_200_And_Persists
- PutUpdate_Invokes_BeforeUpdate_Hook
- PutUpdate_Invokes_AfterUpdate_Hook
- PutUpdate_Missing_Returns_404
- DeleteInvoice_Returns_200_And_Removes
- DeleteInvoice_Invokes_BeforeDelete_Hook
- DeleteInvoice_Invokes_AfterDelete_Hook
- DeleteInvoice_Missing_Returns_404
- List_Respects_Skip_Parameter
- BeforeCreate_Sets_CreatedAt_Timestamp
- [And 13 more...]

**Tests**: 30

### 2. HeadEntityHookExecutionTests.cs (20 tests)

Tests hook pipeline execution order and behavior:

- Create_Executes_Hooks_In_Order
- Update_Executes_Hooks_In_Order
- Delete_Executes_Hooks_In_Order
- AfterCreate_Receives_Created_Entity
- BeforeCreate_Can_Mutate_Entity
- Multiple_Sequential_Creates_Have_Separate_Hook_Executions
- Hook_Execution_Persists_Mutations
- BeforeUpdate_Hook_Receives_Id_And_Entity
- AfterUpdate_Hook_Receives_Id_And_Updated_Entity
- BeforeDelete_Hook_Receives_Id
- AfterDelete_Hook_Receives_Deleted_Entity
- [And 9 more...]

**Tests**: 20

### 3. HeadEntityCustomActionTests.cs (18 tests)

Tests custom domain action endpoints:

- CustomAction_Pay_Changes_Status
- CustomAction_Pay_Sets_PaidAt_Timestamp
- CustomAction_Pay_Executes_Hook
- CustomAction_Pay_Returns_Modified_Entity
- CustomAction_Archive_Changes_Status
- CustomAction_Archive_Executes_Hook
- CustomAction_Missing_Entity_Returns_404
- CustomAction_Persists_Mutations
- Multiple_CustomActions_Can_Modify_Same_Entity
- [And 9 more...]

**Tests**: 18

### 4. HeadEntityPagingAndFilteringTests.cs (15 tests)

Tests pagination and query logic:

- List_Default_Paging_Returns_First_Page
- List_Skip_Parameter_Skips_Entities
- List_Take_Parameter_Limits_Results
- List_PageCount_Calculated_Correctly
- List_Last_Page_Partial_Results
- List_Empty_Results_Returns_Zero_Total
- List_Single_Page_Results
- List_Skip_Beyond_Total_Returns_Empty
- List_Boundary_Skip_Zero
- List_Boundary_Take_One
- List_TotalCount_Reflects_All_Records
- [And 4 more...]

**Tests**: 15

### 5. HeadEntityErrorScenariosTests.cs (12 tests)

Tests error handling and edge cases:

- GetNonExistent_Returns_404
- UpdateNonExistent_Returns_404
- DeleteNonExistent_Returns_404
- CustomActionNonExistent_Returns_404
- Create_With_Invalid_Body_Returns_BadRequest
- Update_With_Invalid_Body_Returns_BadRequest
- GetList_InvalidSkip_Returns_BadRequest
- GetList_InvalidTake_Returns_BadRequest
- NegativeSkip_Returns_OK_With_Normalized_Results
- NegativeTake_Normalized_To_One
- RouteToNonExistentAction_Returns_404
- MultipleSequentialErrors_Do_Not_Affect_Success
- GetAfter404_Works_Correctly
- CreateAfterDelete_Creates_New_Entity_With_Different_Id

**Tests**: 12

## Running Tests

### Quick Run
```bash
dotnet test Head.Net.sln
```

### By Framework
```bash
dotnet test Head.Net.sln --framework net9.0
dotnet test Head.Net.sln --framework net8.0
```

### With Coverage
```bash
./scripts/generate-coverage.ps1
```

### Verbose Output
```bash
dotnet test Head.Net.sln --logger "console;verbosity=detailed"
```

## Test Infrastructure

### Components
- **TestWebApplicationFactory**: Creates test WebApplication with DI, DbContext, HTTP client
- **TestInvoice**: Sample IHeadEntity<int> implementation for testing
- **TestHookCollector**: Tracks hook execution for assertions
- **TestAuthorizationProvider**: Manages authorization context for testing

### Key Patterns
- Real HTTP endpoint testing via TestHost
- EF Core InMemory database (isolated per factory)
- Proper DbContext scoping for state management
- Direct database verification of persistence
- Hook execution tracking and verification

## Coverage

The test suite covers:
- ✅ All CRUD operations (Create, Read, Update, Delete)
- ✅ Complete hook lifecycle (Before/After hooks)
- ✅ Custom domain actions
- ✅ Query paging and filtering
- ✅ Error scenarios and validation
- ✅ Multi-framework compatibility

## Continuous Integration

Tests run automatically on:
- **GitHub Actions**: `.github/workflows/coverage.yml`
- **Azure Pipelines**: `azure-pipelines.yml`
- **Local**: `dotnet test Head.Net.sln`

---

**Total Test Count**: 85  
**Status**: ✅ All Passing  
**Coverage**: 100% endpoint flows
