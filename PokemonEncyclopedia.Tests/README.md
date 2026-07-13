# Pokemon Encyclopedia Test Suite

## Overview

Comprehensive test suite for the Pokemon Encyclopedia project with 113 tests organized into logical folders for maximum coverage and maintainability.

## Folder Structure

### `/Unit` - Unit Tests (75 tests)
Pure unit tests with no external dependencies, testing individual components in isolation.

**Tests:**
- `PokemonFilterStateTests.cs` (14 tests) - Filter state management, generation/type toggling, clearing
- `PokemonSearchStateTests.cs` (7 tests) - Search text handling, change notifications
- `PokemonApiClientTests.cs` (10 tests) - API client, name normalization, null handling
- `QueryRecordTests.cs` (6 tests) - Query record equality, structure
- `QueryHandlerTests.cs` (2 tests) - Service registration validation
- `ValidationBehaviorTests.cs` (3 tests) - Validator registration and execution
- `PokemonDtoTests.cs` (3 tests) - DTO record properties and equality
- `EdgeCaseTests.cs` (21 tests) - Boundary conditions and memory cache operations
- `PokemonPreCachingTests.cs` (4 tests) - Pre-caching behavior
- `PokemonVarietiesMethodTests.cs` (5 tests) - Reflection coverage for API method

### `/Integration` - Integration Tests (21 tests)
Tests that verify system behavior across multiple components or with running services.

**Tests:**
- `IntegrationWebTests.cs` (4 tests) - Web frontend routes, page loading, health checks
- `ApiEndpointsTests.cs` (4 tests) - API endpoint validation (requires service)
- `DependencyInjectionTests.cs` (2 tests) - DI configuration and validator registration
- `PokemonVarietiesIntegrationTests.cs` (11 tests) - Species varieties, caching, cancellation
- `AspireAppHostFixture.cs` - Shared integration test fixture

### `/Validators` - Validator Tests (17 tests)
Focused tests for FluentValidation validators.

**Tests:**
- `GetPokemonByNameValidatorTests` (5 tests)
- `GetMoveByNameValidatorTests` (3 tests)
- `GetEvolutionChainBySpeciesNameValidatorTests` (3 tests)
- `GetPokemonByGenerationValidatorTests` (6 tests)

### `/Common` - Test Utilities
Shared test helpers and utilities.

**Files:**
- `TestHelpers.cs` - Common helper functions
  - `CreateTestLoggerFactory()` - Logger setup
  - `NormalizeName()` - Name normalization
  - `IsValidPokemonGeneration()` - Generation validation

## Test Statistics

- **Total Tests**: 113
- **Unit Tests**: 75 ✅
- **Integration Tests**: 21 ✅
- **Validator Tests**: 17 ✅
- **Pass Rate**: 100% (113/113)

## Running Tests

### All Tests
```bash
dotnet test PokemonEncyclopedia.Tests
```

### Specific Test Class
```bash
dotnet test --filter "ClassName=PokemonEncyclopedia.Tests.Unit.PokemonFilterStateTests"
```

### Specific Test Method
```bash
dotnet test --filter "MethodName=SearchText_ShouldUpdateWhenChanged"
```

### Unit Tests Only
```bash
dotnet test --filter "Namespace~PokemonEncyclopedia.Tests.Unit"
```

### With Verbose Output
```bash
dotnet test -v detailed
```

### With Coverage Report
```bash
dotnet test /p:CollectCoverage=true /p:CoverageFormat=cobertura /p:CoverageReportFormat=summary
```

## Test Naming Conventions

All tests follow the pattern: `MethodName_Condition_ExpectedBehavior`

Examples:
- `SearchText_ShouldUpdateWhenChanged` - When search text is updated, it should change
- `ToggleGeneration_ShouldRemoveGenerationWhenPresent` - When toggling an existing generation, remove it
- `Validator_ShouldFailWithEmptyName` - When name is empty, validation should fail

## Coverage by Feature

These are the main coverage areas; smaller utility and reflection tests are listed above.

### State Management (21 tests)
- ✅ PokemonFilterState initialization, updates, events
- ✅ PokemonSearchState initialization, updates, events
- ✅ Changed event handling
- ✅ Filter reset/clear functionality

### API Client (10 tests)
- ✅ Null/empty validation
- ✅ Name normalization (case, whitespace)
- ✅ Caching behavior
- ✅ Method responses (Pokemon, Move, Species, Ability, EvolutionChain)

### Validation (20 tests)
- ✅ Name validation (empty, length)
- ✅ Generation validation (1-9 range)
- ✅ Error messages

### DTOs & Records (9 tests)
- ✅ Query record equality
- ✅ Query record structure
- ✅ DTO property initialization

### Integration (21 tests)
- ✅ Web routes load correctly
- ✅ API endpoints respond
- ✅ Dependency injection works
- ✅ Health checks pass

### Edge Cases (21 tests)
- ✅ Whitespace handling
- ✅ Null values
- ✅ Out of range inputs
- ✅ Memory cache operations

## Key Testing Libraries

- **xUnit** - Test framework
- **Moq** - Mocking framework
- **FluentAssertions** - Assertion syntax
- **FluentValidation** - Validation testing
- **Aspire.Hosting.Testing** - Integration testing

## Adding New Tests

1. **Create test file** in appropriate folder (`/Unit`, `/Integration`, `/Validators`)
2. **Follow naming convention**: `MethodName_Condition_ExpectedBehavior`
3. **Use Arrange-Act-Assert pattern**:
   ```csharp
   [Fact]
   public void MyTest()
   {
       // Arrange
       var sut = new SystemUnderTest();
       
       // Act
       var result = sut.Method();
       
       // Assert
       result.Should().Be(expected);
   }
   ```
4. **Test edge cases** and boundary conditions
5. **Use descriptive assertions** with FluentAssertions

## CI/CD Integration

These tests are ready for CI/CD pipelines. Example GitHub Actions workflow:

```yaml
- name: Run tests
  run: dotnet test --logger trx --collect:"XPlat Code Coverage"

- name: Copy coverage file
  run: find coverage -name 'coverage.cobertura.xml' -exec cp {} coverage/coverage.cobertura.xml \;

- name: Code Coverage Summary
  uses: irongut/CodeCoverageSummary@v1.3.0
  with:
    filename: coverage/coverage.cobertura.xml
    badge: true
    format: markdown
```

## Test Execution Tips

- Tests run in parallel by default for speed
- Use `--no-build` after successful build to save time
- Filter tests to run only what you need during development
- Integration tests may timeout on first run while building containers

## Known Issues

- Some integration tests require Aspire services running (expected behavior)
- First integration test run downloads Docker images (can be slow)
- Evolution chart navigation tests depend on Pokemon detail page loading

## Future Improvements

- Add performance/benchmark tests
- Add API response validation tests
- Add error scenario tests
- Add accessibility tests
- Add visual regression tests for UI components
- Increase to 95%+ code coverage
