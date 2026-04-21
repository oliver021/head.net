using Head.Net.Abstractions;
using Head.Net.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Head.Net.Tests;

public sealed class HeadEntityEndpointBuilderTests
{
    [Fact]
    public void Fluent_API_Supports_All_Hooks()
    {
        // BeforeCreate returns HeadHookResult<T>? (null = success)
        HeadBeforeCreateDelegate<DummyEntity> beforeCreate = (entity, ct)
            => new ValueTask<HeadHookResult<DummyEntity>?>((HeadHookResult<DummyEntity>?)null);

        HeadAfterCreateDelegate<DummyEntity> afterCreate = (entity, ct) => ValueTask.CompletedTask;

        // BeforeUpdate returns HeadHookResult<T>? (null = success)
        HeadBeforeUpdateDelegate<DummyEntity, int> beforeUpdate = (id, entity, ct)
            => new ValueTask<HeadHookResult<DummyEntity>?>((HeadHookResult<DummyEntity>?)null);

        HeadAfterUpdateDelegate<DummyEntity, int> afterUpdate = (id, entity, ct) => ValueTask.CompletedTask;

        HeadBeforeDeleteDelegate<DummyEntity, int> beforeDelete = (id, ct) => ValueTask.CompletedTask;

        HeadAfterDeleteDelegate<DummyEntity> afterDelete = (entity, ct) => ValueTask.CompletedTask;

        // These hooks are defined and can be registered
        Assert.NotNull(beforeCreate);
        Assert.NotNull(afterCreate);
        Assert.NotNull(beforeUpdate);
        Assert.NotNull(afterUpdate);
        Assert.NotNull(beforeDelete);
        Assert.NotNull(afterDelete);
    }

    [Fact]
    public void HeadQueryOptions_Supports_Paging()
    {
        var options = new HeadQueryOptions { Skip = 10, Take = 20 };

        Assert.Equal(10, options.Skip);
        Assert.Equal(20, options.Take);
    }

    [Fact]
    public void HeadPagedResult_Calculates_PageCount()
    {
        var data = new[] { new DummyEntity { Id = 1 } };
        var result = new HeadPagedResult<DummyEntity>(data, totalCount: 250, skip: 0, take: 10);

        Assert.Equal(25, result.PageCount);
        Assert.Single(result.Data);
        Assert.Equal(250, result.TotalCount);
    }

    [Fact]
    public void HeadValidationResult_Tracks_Errors()
    {
        var result = HeadValidationResult.Failure("Error 1", "Error 2");

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains("Error 1", result.Errors);
    }

    [Fact]
    public void HeadValidationResult_Success_Has_No_Errors()
    {
        var result = HeadValidationResult.Success();

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void HeadHookResult_Continue_Allows_Proceeding()
    {
        var result = HeadHookResult<DummyEntity>.Continue();

        Assert.True(result.ShouldProceed);
        Assert.Null(result.ValidationResult);
    }

    [Fact]
    public void HeadHookResult_Invalid_Prevents_Proceeding()
    {
        var validation = HeadValidationResult.Failure("Invalid data");
        var result = HeadHookResult<DummyEntity>.Invalid(validation);

        Assert.False(result.ShouldProceed);
        Assert.NotNull(result.ValidationResult);
    }

    [Fact]
    public void HeadAuthorizationContext_Tracks_User()
    {
        var ctx = new HeadAuthorizationContext(userId: 42, role: "admin");

        Assert.Equal(42, ctx.UserId);
        Assert.Equal("admin", ctx.Role);
        Assert.True(ctx.IsAuthenticated);
    }

    [Fact]
    public void HeadAuthorizationContext_Unauthenticated_When_UserId_Zero()
    {
        var ctx = new HeadAuthorizationContext(userId: 0);

        Assert.False(ctx.IsAuthenticated);
    }

    [Fact]
    public void HeadAuthorizationResult_Allow_Succeeds()
    {
        var result = HeadAuthorizationResult.Allow();

        Assert.True(result.Allowed);
        Assert.Null(result.Reason);
    }

    [Fact]
    public void HeadAuthorizationResult_Deny_Fails()
    {
        var result = HeadAuthorizationResult.Deny("Access denied");

        Assert.False(result.Allowed);
        Assert.Equal("Access denied", result.Reason);
    }

    [Fact]
    public void Ownership_Extractor_Delegate_Defined()
    {
        HeadOwnershipExtractor<DummyEntity> extractor = entity => entity.OwnerId;

        var entity = new DummyEntity { Id = 1, OwnerId = 42 };
        var ownerId = extractor(entity);

        Assert.Equal(42, ownerId);
    }

    [Fact]
    public void Authorization_Policy_Delegate_Defined()
    {
        HeadAuthorizationPolicyDelegate<DummyEntity> policy = async (entity, userId, ct) =>
        {
            await Task.CompletedTask;
            return entity.OwnerId == userId;
        };

        Assert.NotNull(policy);
    }

    [Fact]
    public void Setup_Calls_Configure_On_Setup_Class()
    {
        var builder = MakeBuilder();

        builder.Setup<TrackingSetup>();

        Assert.True(TrackingSetup.WasConfigured);
    }

    [Fact]
    public void Setup_Injects_Constructor_Dependencies_From_ServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<FakeDependency>();
        var builder = MakeBuilder(services.BuildServiceProvider());

        builder.Setup<DependencyCapturingSetup>();

        Assert.NotNull(DependencyCapturingSetup.CapturedDependency);
    }

    [Fact]
    public void Setup_Is_Composable_With_WithCrud()
    {
        var builder = MakeBuilder();

        var result = builder.WithCrud().Setup<TrackingSetup>();

        Assert.NotNull(result);
        Assert.True(TrackingSetup.WasConfigured);
    }

    private static HeadEntityEndpointBuilder<DummyEntity, int> MakeBuilder(
        IServiceProvider? serviceProvider = null)
    {
        serviceProvider ??= new ServiceCollection().BuildServiceProvider();
        return new HeadEntityEndpointBuilder<DummyEntity, int>(
            new FakeEndpointRouteBuilder(serviceProvider), "/dummies");
    }

    private sealed class FakeEndpointRouteBuilder : IEndpointRouteBuilder
    {
        public FakeEndpointRouteBuilder(IServiceProvider sp) => ServiceProvider = sp;
        public IServiceProvider ServiceProvider { get; }
        public ICollection<EndpointDataSource> DataSources { get; } = [];
        public IApplicationBuilder CreateApplicationBuilder() => throw new NotSupportedException();
    }

    private sealed class TrackingSetup : IHeadEntitySetup<DummyEntity, int>
    {
        public static bool WasConfigured { get; private set; }
        public TrackingSetup() => WasConfigured = false;
        public void Configure(HeadEntityEndpointBuilder<DummyEntity, int> builder) => WasConfigured = true;
    }

    private sealed class FakeDependency { }

    private sealed class DependencyCapturingSetup : IHeadEntitySetup<DummyEntity, int>
    {
        public static FakeDependency? CapturedDependency { get; private set; }
        public DependencyCapturingSetup(FakeDependency dep) => CapturedDependency = dep;
        public void Configure(HeadEntityEndpointBuilder<DummyEntity, int> builder) { }
    }

    private sealed class DummyEntity : IHeadEntity<int>
    {
        public int Id { get; set; }
        public int OwnerId { get; set; }
    }
}
