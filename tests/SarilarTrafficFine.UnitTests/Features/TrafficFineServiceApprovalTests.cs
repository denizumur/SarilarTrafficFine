using System.Linq.Expressions;
using SarilarTrafficFine.Business.Abstractions.Persistence;
using SarilarTrafficFine.Business.Abstractions.Persistence.Models;
using SarilarTrafficFine.Business.Constants;
using SarilarTrafficFine.Business.Features.TrafficFines;
using SarilarTrafficFine.Business.Features.TrafficFines.Models;
using SarilarTrafficFine.Business.Security;
using SarilarTrafficFine.Entities.Enums;
using SarilarTrafficFine.Entities.Models;

namespace SarilarTrafficFine.UnitTests.Features.TrafficFines;

public sealed class TrafficFineServiceApprovalTests
{
    [Fact]
    public async Task SubmitAsync_NewFine_MovesToFirstDatabaseDefinedStep()
    {
        var workflow = CreateThreeStepWorkflow();

        var trafficFine = CreateTrafficFine(
            status: TrafficFineStatus.New);

        var fixture = CreateFixture(
            trafficFine,
            workflow);

        var operatorUser = CreateUser(
            RoleNames.Operator);

        var result = await fixture.Service.SubmitAsync(
            trafficFine.Id,
            operatorUser);

        Assert.True(result.Succeeded);

        Assert.Equal(
            TrafficFineStatus.InApproval,
            trafficFine.Status);

        Assert.Equal(
            workflow.Id,
            trafficFine.ApprovalWorkflowId);

        Assert.Equal(
            10,
            trafficFine.CurrentApprovalStepId);

        var history =
            Assert.Single(fixture.HistoryRepository.Items);

        Assert.Equal(
            ApprovalActionType.Submitted,
            history.ActionType);

        Assert.Equal(
            "Yeni",
            history.PreviousState);

        Assert.Equal(
            "Onayda · Yönetici Onayý",
            history.NewState);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task ApproveAsync_WrongRole_ReturnsForbidden()
    {
        var workflow = CreateThreeStepWorkflow();

        var trafficFine = CreateTrafficFine(
            status: TrafficFineStatus.InApproval,
            workflowId: workflow.Id,
            currentStepId: 10);

        var fixture = CreateFixture(
            trafficFine,
            workflow);

        var financeUser = CreateUser(
            RoleNames.Finance);

        var result = await fixture.Service.ApproveAsync(
            trafficFine.Id,
            financeUser);

        Assert.False(result.Succeeded);

        Assert.Equal(
            TrafficFineCommandError.Forbidden,
            result.Error);

        Assert.Equal(
            10,
            trafficFine.CurrentApprovalStepId);

        Assert.Empty(
            fixture.HistoryRepository.Items);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task ApproveAsync_CreatorWithMatchingRole_ReturnsForbidden()
    {
        var workflow =
            CreateThreeStepWorkflow();

        var trafficFine =
            CreateTrafficFine(
                status:
                    TrafficFineStatus.InApproval,
                workflowId:
                    workflow.Id,
                currentStepId:
                    10,
                createdByUserId:
                    "manager-user-id");

        var fixture =
            CreateFixture(
                trafficFine,
                workflow);

        var creatorManagerUser =
            CreateUser(
                RoleNames.Manager);

        var result =
            await fixture.Service.ApproveAsync(
                trafficFine.Id,
                creatorManagerUser);

        Assert.False(
            result.Succeeded);

        Assert.Equal(
            TrafficFineCommandError.Forbidden,
            result.Error);

        Assert.Equal(
            "Kendi oluþturduðunuz kaydý onaylayamaz veya reddedemezsiniz.",
            result.ErrorMessage);

        Assert.Equal(
            TrafficFineStatus.InApproval,
            trafficFine.Status);

        Assert.Equal(
            10,
            trafficFine.CurrentApprovalStepId);

        Assert.Empty(
            fixture.HistoryRepository.Items);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task RejectAsync_CreatorWithMatchingRole_ReturnsForbidden()
    {
        var workflow =
            CreateThreeStepWorkflow();

        var trafficFine =
            CreateTrafficFine(
                status:
                    TrafficFineStatus.InApproval,
                workflowId:
                    workflow.Id,
                currentStepId:
                    10,
                createdByUserId:
                    "manager-user-id");

        var fixture =
            CreateFixture(
                trafficFine,
                workflow);

        var creatorManagerUser =
            CreateUser(
                RoleNames.Manager);

        var result =
            await fixture.Service.RejectAsync(
                trafficFine.Id,
                "Self reject engellenmeli.",
                creatorManagerUser);

        Assert.False(
            result.Succeeded);

        Assert.Equal(
            TrafficFineCommandError.Forbidden,
            result.Error);

        Assert.Equal(
            "Kendi oluþturduðunuz kaydý onaylayamaz veya reddedemezsiniz.",
            result.ErrorMessage);

        Assert.Equal(
            TrafficFineStatus.InApproval,
            trafficFine.Status);

        Assert.Equal(
            10,
            trafficFine.CurrentApprovalStepId);

        Assert.Empty(
            fixture.HistoryRepository.Items);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task ApproveAsync_CurrentStepApproved_MovesToNextDatabaseDefinedStep()
    {
        var workflow = CreateThreeStepWorkflow();

        var trafficFine = CreateTrafficFine(
            status: TrafficFineStatus.InApproval,
            workflowId: workflow.Id,
            currentStepId: 10);

        var fixture = CreateFixture(
            trafficFine,
            workflow);

        var managerUser = CreateUser(
            RoleNames.Manager);

        var result = await fixture.Service.ApproveAsync(
            trafficFine.Id,
            managerUser);

        Assert.True(result.Succeeded);

        Assert.Equal(
            TrafficFineStatus.InApproval,
            trafficFine.Status);

        Assert.Equal(
            20,
            trafficFine.CurrentApprovalStepId);

        var history =
            Assert.Single(fixture.HistoryRepository.Items);

        Assert.Equal(
            ApprovalActionType.Approved,
            history.ActionType);

        Assert.Equal(
            "Onayda · Yönetici Onayý",
            history.PreviousState);

        Assert.Equal(
            "Onayda · Hukuk Onayý",
            history.NewState);
    }

    [Fact]
    public async Task ApproveAsync_FinalStepApproved_CompletesTrafficFine()
    {
        var workflow = CreateThreeStepWorkflow();

        var trafficFine = CreateTrafficFine(
            status: TrafficFineStatus.InApproval,
            workflowId: workflow.Id,
            currentStepId: 30);

        var fixture = CreateFixture(
            trafficFine,
            workflow);

        var financeUser = CreateUser(
            RoleNames.Finance);

        var result = await fixture.Service.ApproveAsync(
            trafficFine.Id,
            financeUser);

        Assert.True(result.Succeeded);

        Assert.Equal(
            TrafficFineStatus.Completed,
            trafficFine.Status);

        Assert.Null(
            trafficFine.CurrentApprovalStepId);

        var history =
            Assert.Single(fixture.HistoryRepository.Items);

        Assert.Equal(
            "Onayda · Finans Onayý",
            history.PreviousState);

        Assert.Equal(
            "Tamamlandý",
            history.NewState);

        Assert.Equal(
            ApprovalActionType.Approved,
            history.ActionType);
    }

    [Fact]
    public async Task RejectAsync_WithoutReason_ReturnsValidationError()
    {
        var workflow = CreateThreeStepWorkflow();

        var trafficFine = CreateTrafficFine(
            status: TrafficFineStatus.InApproval,
            workflowId: workflow.Id,
            currentStepId: 10);

        var fixture = CreateFixture(
            trafficFine,
            workflow);

        var managerUser = CreateUser(
            RoleNames.Manager);

        var result = await fixture.Service.RejectAsync(
            trafficFine.Id,
            "   ",
            managerUser);

        Assert.False(result.Succeeded);

        Assert.Equal(
            TrafficFineCommandError.Validation,
            result.Error);

        Assert.Equal(
            "Reason",
            result.ErrorField);

        Assert.Equal(
            TrafficFineStatus.InApproval,
            trafficFine.Status);

        Assert.Empty(
            fixture.HistoryRepository.Items);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task RejectAsync_ValidReason_MovesFineToRejectedTerminalState()
    {
        var workflow = CreateThreeStepWorkflow();

        var trafficFine = CreateTrafficFine(
            status: TrafficFineStatus.InApproval,
            workflowId: workflow.Id,
            currentStepId: 10);

        var fixture = CreateFixture(
            trafficFine,
            workflow);

        var managerUser = CreateUser(
            RoleNames.Manager);

        var result = await fixture.Service.RejectAsync(
            trafficFine.Id,
            "  Belge doðrulamasý baþarýsýz.  ",
            managerUser);

        Assert.True(result.Succeeded);

        Assert.Equal(
            TrafficFineStatus.Rejected,
            trafficFine.Status);

        Assert.Null(
            trafficFine.CurrentApprovalStepId);

        Assert.Equal(
            workflow.Id,
            trafficFine.ApprovalWorkflowId);

        var history =
            Assert.Single(fixture.HistoryRepository.Items);

        Assert.Equal(
            ApprovalActionType.Rejected,
            history.ActionType);

        Assert.Equal(
            "Belge doðrulamasý baþarýsýz.",
            history.Comment);

        Assert.Equal(
            "Onayda · Yönetici Onayý",
            history.PreviousState);

        Assert.Equal(
            "Reddedildi",
            history.NewState);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task ApproveAsync_CompletedFine_ReturnsInvalidState()
    {
        var workflow = CreateThreeStepWorkflow();

        var trafficFine = CreateTrafficFine(
            status: TrafficFineStatus.Completed,
            workflowId: workflow.Id);

        var fixture = CreateFixture(
            trafficFine,
            workflow);

        var financeUser = CreateUser(
            RoleNames.Finance);

        var result = await fixture.Service.ApproveAsync(
            trafficFine.Id,
            financeUser);

        Assert.False(result.Succeeded);

        Assert.Equal(
            TrafficFineCommandError.InvalidState,
            result.Error);

        Assert.Equal(
            TrafficFineStatus.Completed,
            trafficFine.Status);

        Assert.Empty(
            fixture.HistoryRepository.Items);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task ApproveAsync_RejectedFine_ReturnsInvalidState()
    {
        var workflow = CreateThreeStepWorkflow();

        var trafficFine = CreateTrafficFine(
            status: TrafficFineStatus.Rejected,
            workflowId: workflow.Id);

        var fixture = CreateFixture(
            trafficFine,
            workflow);

        var managerUser = CreateUser(
            RoleNames.Manager);

        var result = await fixture.Service.ApproveAsync(
            trafficFine.Id,
            managerUser);

        Assert.False(result.Succeeded);

        Assert.Equal(
            TrafficFineCommandError.InvalidState,
            result.Error);

        Assert.Equal(
            TrafficFineStatus.Rejected,
            trafficFine.Status);

        Assert.Empty(
            fixture.HistoryRepository.Items);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task ApproveAsync_WritesPreviousNewAndStepSnapshotsToHistory()
    {
        var workflow = CreateThreeStepWorkflow();

        var trafficFine = CreateTrafficFine(
            status: TrafficFineStatus.InApproval,
            workflowId: workflow.Id,
            currentStepId: 10);

        var fixture = CreateFixture(
            trafficFine,
            workflow);

        var managerUser = new CurrentUserContext(
            "manager-user-id",
            "manager@demo.local",
            new[]
            {
                RoleNames.Manager
            });

        var result = await fixture.Service.ApproveAsync(
            trafficFine.Id,
            managerUser);

        Assert.True(result.Succeeded);

        var history =
            Assert.Single(fixture.HistoryRepository.Items);

        Assert.Equal(
            trafficFine.Id,
            history.TrafficFineId);

        Assert.Equal(
            "manager-user-id",
            history.ActionByUserId);

        Assert.Equal(
            "manager@demo.local",
            history.ActionByUserName);

        Assert.Equal(
            ApprovalActionType.Approved,
            history.ActionType);

        Assert.Equal(
            "Onayda · Yönetici Onayý",
            history.PreviousState);

        Assert.Equal(
            "Onayda · Hukuk Onayý",
            history.NewState);

        Assert.Equal(
            10,
            history.WorkflowStepId);

        Assert.Equal(
            1,
            history.WorkflowStepOrder);

        Assert.Equal(
            "Yönetici Onayý",
            history.WorkflowStepName);
    }

    [Fact]
    public async Task CreateAsync_ManagerUser_Succeeds()
    {
        var workflow =
            CreateThreeStepWorkflow();

        var fixture =
            CreateFixture(
                CreateTrafficFine(
                    TrafficFineStatus.New),
                workflow);

        var managerUser =
            CreateUser(
                RoleNames.Manager);

        var request =
            new TrafficFineCreateRequest(
                1,
                DateOnly.FromDateTime(
                    DateTime.Now),
                2_500m,
                "Manager trafik cezasý");

        var result =
            await fixture.Service.CreateAsync(
                request,
                managerUser);

        Assert.True(
            result.Succeeded);

        var createdFine =
            Assert.Single(
                fixture.TrafficFineEntityRepository.Items);

        Assert.Equal(
            "manager-user-id",
            createdFine.CreatedByUserId);

        Assert.Equal(
            TrafficFineStatus.New,
            createdFine.Status);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task CreateAsync_FinanceUser_Succeeds()
    {
        var workflow =
            CreateThreeStepWorkflow();

        var fixture =
            CreateFixture(
                CreateTrafficFine(
                    TrafficFineStatus.New),
                workflow);

        var financeUser =
            CreateUser(
                RoleNames.Finance);

        var request =
            new TrafficFineCreateRequest(
                1,
                DateOnly.FromDateTime(
                    DateTime.Now),
                3_250m,
                "Finance trafik cezasý");

        var result =
            await fixture.Service.CreateAsync(
                request,
                financeUser);

        Assert.True(
            result.Succeeded);

        var createdFine =
            Assert.Single(
                fixture.TrafficFineEntityRepository.Items);

        Assert.Equal(
            "finance-user-id",
            createdFine.CreatedByUserId);

        Assert.Equal(
            TrafficFineStatus.New,
            createdFine.Status);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task EditAsync_Creator_Succeeds()
    {
        var workflow =
            CreateThreeStepWorkflow();

        var trafficFine =
            CreateTrafficFine(
                TrafficFineStatus.New,
                createdByUserId:
                    "manager-user-id");

        var fixture =
            CreateFixture(
                trafficFine,
                workflow);

        var managerUser =
            CreateUser(
                RoleNames.Manager);

        var request =
            new TrafficFineEditRequest(
                trafficFine.Id,
                1,
                DateOnly.FromDateTime(
                    DateTime.Now),
                7_500m,
                "Creator edit testi",
                new byte[] { 1 });

        var result =
            await fixture.Service.EditAsync(
                request,
                managerUser);

        Assert.True(
            result.Succeeded);

        Assert.Equal(
            7_500m,
            trafficFine.Amount);

        Assert.Equal(
            "Creator edit testi",
            trafficFine.Description);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task EditAsync_NonCreator_ReturnsForbidden()
    {
        var workflow =
            CreateThreeStepWorkflow();

        var trafficFine =
            CreateTrafficFine(
                TrafficFineStatus.New);

        var fixture =
            CreateFixture(
                trafficFine,
                workflow);

        var managerUser =
            CreateUser(
                RoleNames.Manager);

        var request =
            new TrafficFineEditRequest(
                trafficFine.Id,
                1,
                DateOnly.FromDateTime(
                    DateTime.Now),
                7_500m,
                "Yetkisiz edit testi",
                new byte[] { 1 });

        var result =
            await fixture.Service.EditAsync(
                request,
                managerUser);

        Assert.False(
            result.Succeeded);

        Assert.Equal(
            TrafficFineCommandError.Forbidden,
            result.Error);

        Assert.Equal(
            "Yalnýzca kendi oluþturduðunuz trafik cezasýný düzenleyebilirsiniz.",
            result.ErrorMessage);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task SubmitAsync_NonCreator_ReturnsForbidden()
    {
        var workflow =
            CreateThreeStepWorkflow();

        var trafficFine =
            CreateTrafficFine(
                TrafficFineStatus.New);

        var fixture =
            CreateFixture(
                trafficFine,
                workflow);

        var managerUser =
            CreateUser(
                RoleNames.Manager);

        var result =
            await fixture.Service.SubmitAsync(
                trafficFine.Id,
                managerUser);

        Assert.False(
            result.Succeeded);

        Assert.Equal(
            TrafficFineCommandError.Forbidden,
            result.Error);

        Assert.Equal(
            "Yalnýzca kendi oluþturduðunuz trafik cezasýný onaya gönderebilirsiniz.",
            result.ErrorMessage);

        Assert.Equal(
            TrafficFineStatus.New,
            trafficFine.Status);

        Assert.Empty(
            fixture.HistoryRepository.Items);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task ApproveAsync_CreatorWithMatchingFinanceRole_ReturnsForbidden()
    {
        var workflow =
            CreateThreeStepWorkflow();

        var trafficFine =
            CreateTrafficFine(
                status:
                    TrafficFineStatus.InApproval,
                workflowId:
                    workflow.Id,
                currentStepId:
                    30,
                createdByUserId:
                    "finance-user-id");

        var fixture =
            CreateFixture(
                trafficFine,
                workflow);

        var financeUser =
            CreateUser(
                RoleNames.Finance);

        var result =
            await fixture.Service.ApproveAsync(
                trafficFine.Id,
                financeUser);

        Assert.False(
            result.Succeeded);

        Assert.Equal(
            TrafficFineCommandError.Forbidden,
            result.Error);

        Assert.Equal(
            "Kendi oluþturduðunuz kaydý onaylayamaz veya reddedemezsiniz.",
            result.ErrorMessage);

        Assert.Equal(
            30,
            trafficFine.CurrentApprovalStepId);

        Assert.Empty(
            fixture.HistoryRepository.Items);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task RejectAsync_CreatorWithMatchingFinanceRole_ReturnsForbidden()
    {
        var workflow =
            CreateThreeStepWorkflow();

        var trafficFine =
            CreateTrafficFine(
                status:
                    TrafficFineStatus.InApproval,
                workflowId:
                    workflow.Id,
                currentStepId:
                    30,
                createdByUserId:
                    "finance-user-id");

        var fixture =
            CreateFixture(
                trafficFine,
                workflow);

        var financeUser =
            CreateUser(
                RoleNames.Finance);

        var result =
            await fixture.Service.RejectAsync(
                trafficFine.Id,
                "Finance self reject testi.",
                financeUser);

        Assert.False(
            result.Succeeded);

        Assert.Equal(
            TrafficFineCommandError.Forbidden,
            result.Error);

        Assert.Equal(
            "Kendi oluþturduðunuz kaydý onaylayamaz veya reddedemezsiniz.",
            result.ErrorMessage);

        Assert.Equal(
            30,
            trafficFine.CurrentApprovalStepId);

        Assert.Empty(
            fixture.HistoryRepository.Items);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task CreateAsync_FutureFineDate_ReturnsValidationError()
    {
        var workflow =
            CreateThreeStepWorkflow();

        var trafficFine =
            CreateTrafficFine(
                status: TrafficFineStatus.New);

        var fixture =
            CreateFixture(
                trafficFine,
                workflow);

        var operatorUser =
            CreateUser(
                RoleNames.Operator);

        var request =
            new TrafficFineCreateRequest(
                1,
                DateOnly.FromDateTime(
                    DateTime.Now).AddDays(7),
                1_250m,
                "Gelecek tarih testi");

        var result =
            await fixture.Service.CreateAsync(
                request,
                operatorUser);

        Assert.False(
            result.Succeeded);

        Assert.Equal(
            TrafficFineCommandError.Validation,
            result.Error);

        Assert.Equal(
            "FineDate",
            result.ErrorField);

        Assert.Equal(
            "Ceza tarihi gelecek bir tarih olamaz.",
            result.ErrorMessage);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task GetPendingApprovalsAsync_PassesCurrentUserRolesToRepository()
    {
        var workflow = CreateThreeStepWorkflow();

        var trafficFine = CreateTrafficFine(
            status: TrafficFineStatus.InApproval,
            workflowId: workflow.Id,
            currentStepId: 10);

        var fixture = CreateFixture(
            trafficFine,
            workflow);

        fixture.TrafficFineRepository.PendingRecords.Add(
            new TrafficFineListRecord(
                trafficFine.Id,
                "41 ABC 123",
                "Toyota",
                "Corolla",
                trafficFine.FineDate,
                trafficFine.Amount,
                TrafficFineStatus.InApproval,
                "operator@demo.local",
                "Yönetici Onayý"));

        var currentUser = new CurrentUserContext(
            "multi-role-user-id",
            "approver@demo.local",
            new[]
            {
                RoleNames.Manager,
                "Legal"
            });

        var result =
            await fixture.Service.GetPendingApprovalsAsync(
                currentUser);

        Assert.Single(result);

        Assert.Equal(
            new[]
            {
                RoleNames.Manager,
                "Legal"
            },
            fixture.TrafficFineRepository.LastPendingRoles);
    }

    private static TestFixture CreateFixture(
        TrafficFine trafficFine,
        ApprovalWorkflow workflow)
    {
        var trafficFineRepository =
            new FakeTrafficFineRepository(
                trafficFine);

        var workflowRepository =
            new FakeApprovalWorkflowRepository(
                workflow);

        var trafficFineGenericRepository =
            new FakeGenericRepository<TrafficFine>();

        var historyRepository =
            new FakeGenericRepository<ApprovalHistory>();

        var vehicleRepository =
            new FakeGenericRepository<Vehicle>();

        vehicleRepository.Items.Add(
            new Vehicle
            {
                Id = 1,
                PlateNumber = "41 TEST 001",
                VehicleType =
                    VehicleType.PassengerCar,
                Brand = "Test",
                Model = "Vehicle",
                IsActive = true,
                CreatedAt =
                    DateTimeOffset.UtcNow
            });

        var unitOfWork =
            new FakeUnitOfWork();

        var service =
            new TrafficFineService(
                trafficFineRepository,
                workflowRepository,
                trafficFineGenericRepository,
                historyRepository,
                vehicleRepository,
                unitOfWork);

        return new TestFixture(
            service,
            trafficFineRepository,
            trafficFineGenericRepository,
            historyRepository,
            unitOfWork);
    }

    private static TrafficFine CreateTrafficFine(
        TrafficFineStatus status,
        int? workflowId = null,
        int? currentStepId = null,
        string createdByUserId =
            "operator-user-id")
    {
        return new TrafficFine
        {
            Id = 1,
            VehicleId = 1,
            FineDate = new DateOnly(
                2026,
                8,
                22),
            Amount = 12_250.50m,
            Description = "Test cezasý",
            Status = status,
            ApprovalWorkflowId = workflowId,
            CurrentApprovalStepId = currentStepId,
            CreatedByUserId =
                createdByUserId,
            CreatedAt =
                DateTimeOffset.UtcNow
        };
    }

    private static ApprovalWorkflow
        CreateThreeStepWorkflow()
    {
        var workflow = new ApprovalWorkflow
        {
            Id = 100,
            Code = ApprovalWorkflowCodes.TrafficFine,
            Name = "Trafik Cezasý Onay Akýþý",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        /*
         * Collection intentionally added out of order.
         *
         * Engine must use StepOrder rather than collection order
         * or hard-coded role names.
         */

        workflow.Steps.Add(
            new ApprovalWorkflowStep
            {
                Id = 30,
                ApprovalWorkflowId = workflow.Id,
                StepOrder = 3,
                Name = "Finans Onayý",
                RequiredRole = RoleNames.Finance
            });

        workflow.Steps.Add(
            new ApprovalWorkflowStep
            {
                Id = 10,
                ApprovalWorkflowId = workflow.Id,
                StepOrder = 1,
                Name = "Yönetici Onayý",
                RequiredRole = RoleNames.Manager
            });

        workflow.Steps.Add(
            new ApprovalWorkflowStep
            {
                Id = 20,
                ApprovalWorkflowId = workflow.Id,
                StepOrder = 2,
                Name = "Hukuk Onayý",
                RequiredRole = "Legal"
            });

        return workflow;
    }

    private static CurrentUserContext CreateUser(
        string role)
    {
        return new CurrentUserContext(
            $"{role.ToLowerInvariant()}-user-id",
            $"{role.ToLowerInvariant()}@demo.local",
            new[]
            {
                role
            });
    }

    private sealed record TestFixture(
        TrafficFineService Service,
        FakeTrafficFineRepository TrafficFineRepository,
        FakeGenericRepository<TrafficFine>
            TrafficFineEntityRepository,
        FakeGenericRepository<ApprovalHistory>
            HistoryRepository,
        FakeUnitOfWork UnitOfWork);

    private sealed class FakeTrafficFineRepository
        : ITrafficFineRepository
    {
        private readonly TrafficFine _trafficFine;

        public FakeTrafficFineRepository(
            TrafficFine trafficFine)
        {
            _trafficFine = trafficFine;
        }

        public List<TrafficFineListRecord> PendingRecords { get; } = [];

        public IReadOnlyList<string> LastPendingRoles { get; private set; } =
            Array.Empty<string>();

        public Task<IReadOnlyList<TrafficFineListRecord>>
            ListAsync(
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<TrafficFineListRecord> result =
                Array.Empty<TrafficFineListRecord>();

            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<TrafficFineListRecord>>
            GetPendingForRolesAsync(
                IEnumerable<string> roles,
                CancellationToken cancellationToken = default)
        {
            LastPendingRoles = roles.ToArray();

            IReadOnlyList<TrafficFineListRecord> result =
                PendingRecords.ToList();

            return Task.FromResult(result);
        }

        public Task<TrafficFineDetailsRecord?>
            GetDetailsAsync(
                int id,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult<
                TrafficFineDetailsRecord?>(null);
        }

        public Task<TrafficFineApprovalContextRecord?>
            GetApprovalContextAsync(
                int id,
                CancellationToken cancellationToken = default)
        {
            if (_trafficFine.Id != id)
            {
                return Task.FromResult<
                    TrafficFineApprovalContextRecord?>(null);
            }

            return Task.FromResult<
                TrafficFineApprovalContextRecord?>(
                    new TrafficFineApprovalContextRecord(
                        _trafficFine.Id,
                        _trafficFine.Status,
                        _trafficFine.ApprovalWorkflowId,
                        _trafficFine.CurrentApprovalStepId));
        }

        public Task<IReadOnlyList<ApprovalHistoryRecord>>
            GetApprovalHistoryAsync(
                int trafficFineId,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<ApprovalHistoryRecord> result =
                Array.Empty<ApprovalHistoryRecord>();

            return Task.FromResult(result);
        }

        public Task<TrafficFine?> GetForUpdateAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _trafficFine.Id == id
                    ? _trafficFine
                    : null);
        }

        public void SetOriginalRowVersion(
            TrafficFine trafficFine,
            byte[] rowVersion)
        {
        }
    }

    private sealed class FakeApprovalWorkflowRepository
        : IApprovalWorkflowRepository
    {
        private readonly ApprovalWorkflow _workflow;

        public FakeApprovalWorkflowRepository(
            ApprovalWorkflow workflow)
        {
            _workflow = workflow;
        }

        public Task<ApprovalWorkflow?>
            GetActiveByCodeWithStepsAsync(
                string code,
                CancellationToken cancellationToken = default)
        {
            var result =
                _workflow.IsActive
                && string.Equals(
                    _workflow.Code,
                    code,
                    StringComparison.Ordinal)
                    ? _workflow
                    : null;

            return Task.FromResult(result);
        }

        public Task<ApprovalWorkflow?>
            GetByIdWithStepsAsync(
                int id,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _workflow.Id == id
                    ? _workflow
                    : null);
        }
    }

    private sealed class FakeGenericRepository<TEntity>
        : IGenericRepository<TEntity>
        where TEntity : class
    {
        public List<TEntity> Items { get; } = [];

        public Task<TEntity?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var idProperty =
                typeof(TEntity).GetProperty("Id");

            if (idProperty is null)
            {
                return Task.FromResult<TEntity?>(
                    null);
            }

            var entity = Items.FirstOrDefault(
                item =>
                    Equals(
                        idProperty.GetValue(item),
                        id));

            return Task.FromResult(entity);
        }

        public Task<IReadOnlyList<TEntity>> ListAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<TEntity> result =
                Items.ToList();

            return Task.FromResult(result);
        }

        public Task<bool> AnyAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Items.Any(
                    predicate.Compile()));
        }

        public Task AddAsync(
            TEntity entity,
            CancellationToken cancellationToken = default)
        {
            Items.Add(entity);

            return Task.CompletedTask;
        }

        public void Update(
            TEntity entity)
        {
        }
    }

    private sealed class FakeUnitOfWork
        : IUnitOfWork
    {
        public int SaveChangesCalls { get; private set; }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveChangesCalls++;

            return Task.FromResult(1);
        }
    }
}