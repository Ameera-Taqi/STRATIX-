using Stratix.Application.Services.Reports;
using Stratix.Domain.Enums;

namespace Stratix.Application.Interfaces;

public interface IReportDocumentBuilder
{
    GeneratedReportFile Build(ReportDocumentModel model, ReportFormat format);
}
