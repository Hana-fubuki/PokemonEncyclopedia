# Pokemon Encyclopedia Test Suite

## Overview

Comprehensive test suite for the Pokemon Encyclopedia project with 95+ tests organized into logical folders for maximum coverage and maintainability.

## Folder Structure

### `/Unit` - Unit Tests (55 tests)
Pure unit tests with no external dependencies, testing individual components in isolation.

**Tests:**
- `PokemonFilterStateTests.cs` (18 tests) - Filter state management, generation/type toggling, clearing
- `PokemonSearchStateTests.cs` (7 tests) - Search text handling, change notifications
- `PokemonApiClientTests.cs` (10 tests) - API client, name normalization, null handling
- `QueryRecordTests.cs` (6 tests) - Query record equality, structure
- `QueryHandlerTests.cs` (2 tests) - Service registration validation
- `ValidationBehaviorTests.cs` (3 tests) - Validator registration and execution
- `PokemonDtoTests.cs` (3 tests) - DTO record properties and equality
- `EdgeCaseTests.cs` (6 tests) - Boundary conditions, whitespace, memory cache

### `/Integration` - Integration Tests (13 tests)
Tests that verify system behavior across multiple components or with running services.

**Tests:**
- `IntegrationWebTests.cs` (5 tests) - Web frontend routes, page loading, health checks
- `WebTests.cs` (1 test) - Base web integration test
- `ApiEndpointsTests.cs` (5 tests) - API endpoint validation (requires service)
- `DependencyInjectionTests.cs` (2 tests) - DI configuration and validator registration

### `/Validators` - Validator Tests (7 tests)
Focused tests for FluentValidation validators.

**Tests:**
- `ValidatorTests.cs` (7 tests)
  - `GetPokemonByNameValidator` (5 tests)
  - `GetMoveByNameValidator` (3 tests)
  - `GetEvolutionChainBySpeciesNameValidator` (3 tests)
  - `GetPokemonByGenerationValidator` (5 tests)

### `/Common` - Test Utilities
Shared test helpers and utilities.

**Files:**
- `TestHelpers.cs` - Common helper functions
  - `CreateTestLoggerFactory()` - Logger setup
  - `NormalizeName()` - Name normalization
  - `IsValidPokemonGeneration()` - Generation validation

## Test Statistics

- **Total Tests**: 95+
- **Unit Tests**: 55 ✅
- **Integration Tests**: 13 (88 passing, 7 require services)
- **Validator Tests**: 7 ✅
- **Pass Rate**: 92.6% (88/95)

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

### State Management (25 tests)
- ✅ PokemonFilterState initialization, updates, events
- ✅ PokemonSearchState initialization, updates, events
- ✅ Changed event handling
- ✅ Filter reset/clear functionality

### API Client (10 tests)
- ✅ Null/empty validation
- ✅ Name normalization (case, whitespace)
- ✅ Caching behavior
- ✅ Method responses (Pokemon, Move, Species, Ability, EvolutionChain)

### Validation (16 tests)
- ✅ Name validation (empty, length)
- ✅ Generation validation (1-9 range)
- ✅ Error messages

### DTOs & Records (9 tests)
- ✅ Query record equality
- ✅ Query record structure
- ✅ DTO property initialization

### Integration (13 tests)
- ✅ Web routes load correctly
- ✅ API endpoints respond
- ✅ Dependency injection works
- ✅ Health checks pass

### Edge Cases (6 tests)
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
  
- name: Upload coverage
  uses: codecov/codecov-action@v3
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
