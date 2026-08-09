using FluentValidation;
using Stratix.Application.DTOs.Projects;
using Stratix.Application.DTOs.Users;
using Stratix.Application.DTOs.Reports;
using Stratix.Application.DTOs.Ai;

namespace Stratix.Application.Validators;

public class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(4000).When(x => x.Description != null);
        RuleFor(x => x.Progress).InclusiveBetween(0, 100).When(x => x.Progress.HasValue);
        RuleFor(x => x).Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || x.StartDate <= x.EndDate)
            .WithMessage("StartDate must be on or before EndDate.");
    }
}

public class UpdateProjectRequestValidator : AbstractValidator<UpdateProjectRequest>
{
    public UpdateProjectRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(4000).When(x => x.Description != null);
        RuleFor(x => x.Progress).InclusiveBetween(0, 100).When(x => x.Progress.HasValue);
        RuleFor(x => x).Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || x.StartDate <= x.EndDate)
            .WithMessage("StartDate must be on or before EndDate.");
    }
}

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(200);
        RuleFor(x => x.JobTitle).MaximumLength(150).When(x => x.JobTitle != null);
        RuleFor(x => x.Role).IsInEnum();
    }
}

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.JobTitle).MaximumLength(150).When(x => x.JobTitle != null);
        RuleFor(x => x.Role).IsInEnum();
        RuleFor(x => x.Status).IsInEnum();
    }
}

public class CreateReportRequestValidator : AbstractValidator<CreateReportRequest>
{
    public CreateReportRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.ReportType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Format).NotEmpty().MaximumLength(20);
        RuleFor(x => x).Must(x => !x.DateFrom.HasValue || !x.DateTo.HasValue || x.DateFrom <= x.DateTo)
            .WithMessage("DateFrom must be on or before DateTo.");
    }
}

public class GenerateReportRequestValidator : AbstractValidator<GenerateReportRequest>
{
    public GenerateReportRequestValidator()
    {
        RuleFor(x => x.Title).MaximumLength(300).When(x => x.Title != null);
        RuleFor(x => x.ReportType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Format).NotEmpty().MaximumLength(20);
        RuleFor(x => x).Must(x => !x.DateFrom.HasValue || !x.DateTo.HasValue || x.DateFrom <= x.DateTo)
            .WithMessage("DateFrom must be on or before DateTo.");
    }
}

public class ProjectHealthAnalysisRequestValidator : AbstractValidator<ProjectHealthAnalysisRequest>
{
    public ProjectHealthAnalysisRequestValidator()
    {
        RuleFor(x => x.ProjectId).GreaterThan(0);
    }
}
