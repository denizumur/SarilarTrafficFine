using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SarilarTrafficFine.Business.Features.TrafficFines.Models;
using SarilarTrafficFine.Entities.Enums;

namespace SarilarTrafficFine.Web.Documents;

public sealed class TrafficFinePdfDocument
    : IDocument
{
    private static readonly CultureInfo TurkishCulture =
        CultureInfo.GetCultureInfo(
            "tr-TR");

    private readonly TrafficFineDetailsDto
        _trafficFine;

    private readonly TrafficFineApprovalDetailsDto
        _approval;

    public TrafficFinePdfDocument(
        TrafficFineDetailsDto trafficFine,
        TrafficFineApprovalDetailsDto approval)
    {
        ArgumentNullException.ThrowIfNull(
            trafficFine);

        ArgumentNullException.ThrowIfNull(
            approval);

        _trafficFine =
            trafficFine;

        _approval =
            approval;
    }

    public DocumentMetadata GetMetadata()
    {
        return new DocumentMetadata
        {
            Title =
                $"Trafik Cezası #{_trafficFine.Id}",

            Author =
                "Sarılar Group",

            Subject =
                "Trafik cezası detay ve onay geçmişi"
        };
    }

    public void Compose(
        IDocumentContainer container)
    {
        container.Page(
            page =>
            {
                page.Size(
                    PageSizes.A4);

                page.Margin(
                    32);

                page.DefaultTextStyle(
                    style =>
                        style
                            .FontSize(10)
                            .FontColor(
                                Colors.Grey.Darken4));

                page.Header()
                    .Element(
                        ComposeHeader);

                page.Content()
                    .PaddingVertical(20)
                    .Column(
                        ComposeContent);

                page.Footer()
                    .AlignCenter()
                    .Text(
                        text =>
                        {
                            text.Span(
                                "Sayfa ");

                            text.CurrentPageNumber();

                            text.Span(
                                " / ");

                            text.TotalPages();
                        });
            });
    }

    private void ComposeHeader(
        IContainer container)
    {
        container
            .BorderBottom(1)
            .BorderColor(
                Colors.Grey.Lighten2)
            .PaddingBottom(14)
            .Row(
                row =>
                {
                    row.RelativeItem()
                        .Column(
                            column =>
                            {
                                column.Item()
                                    .Text(
                                        "Sarılar Group")
                                    .FontSize(18)
                                    .SemiBold();

                                column.Item()
                                    .PaddingTop(2)
                                    .Text(
                                        "Trafik Cezası Yönetim Modülü")
                                    .FontSize(10)
                                    .FontColor(
                                        Colors.Grey.Darken1);
                            });

                    row.ConstantItem(160)
                        .AlignRight()
                        .Column(
                            column =>
                            {
                                column.Item()
                                    .AlignRight()
                                    .Text(
                                        $"Kayıt #{_trafficFine.Id}")
                                    .SemiBold();

                                column.Item()
                                    .PaddingTop(2)
                                    .AlignRight()
                                    .Text(
                                        $"Oluşturulma: {FormatDateTime(_trafficFine.CreatedAt)}")
                                    .FontSize(9)
                                    .FontColor(
                                        Colors.Grey.Darken1);
                            });
                });
    }

    private void ComposeContent(
        ColumnDescriptor column)
    {
        column.Spacing(
            18);

        column.Item()
            .Element(
                ComposeTitle);

        column.Item()
            .Element(
                ComposeFineDetails);

        column.Item()
            .Element(
                ComposeWorkflow);

        column.Item()
            .Element(
                ComposeApprovalHistory);
    }

    private void ComposeTitle(
        IContainer container)
    {
        container.Column(
            column =>
            {
                column.Item()
                    .Text(
                        "Trafik Cezası Detayı")
                    .FontSize(22)
                    .Bold();

                column.Item()
                    .PaddingTop(4)
                    .Row(
                        row =>
                        {
                            row.RelativeItem()
                                .Text(
                                    $"{_trafficFine.PlateNumber} · {_trafficFine.Brand} {_trafficFine.Model}")
                                .FontSize(11)
                                .FontColor(
                                    Colors.Grey.Darken1);

                            row.AutoItem()
                                .Background(
                                    GetStatusBackgroundColor(
                                        _trafficFine.Status))
                                .PaddingHorizontal(10)
                                .PaddingVertical(5)
                                .Text(
                                    GetStatusText(
                                        _trafficFine.Status))
                                .FontSize(9)
                                .SemiBold()
                                .FontColor(
                                    Colors.White);
                        });
            });
    }

    private void ComposeFineDetails(
        IContainer container)
    {
        container
            .Element(
                SectionContainer)
            .Column(
                column =>
                {
                    column.Item()
                        .Element(
                            SectionTitle)
                        .Text(
                            "Ceza Bilgileri");

                    column.Item()
                        .PaddingTop(12)
                        .Table(
                            table =>
                            {
                                table.ColumnsDefinition(
                                    columns =>
                                    {
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(2);
                                    });

                                AddDetailRow(
                                    table,
                                    "Kayıt No",
                                    $"#{_trafficFine.Id}");

                                AddDetailRow(
                                    table,
                                    "Plaka",
                                    _trafficFine.PlateNumber);

                                AddDetailRow(
                                    table,
                                    "Araç",
                                    $"{_trafficFine.Brand} {_trafficFine.Model}");

                                AddDetailRow(
                                    table,
                                    "Ceza Tarihi",
                                    _trafficFine.FineDate
                                        .ToString(
                                            "dd.MM.yyyy",
                                            TurkishCulture));

                                AddDetailRow(
                                    table,
                                    "Ceza Tutarı",
                                    $"{_trafficFine.Amount.ToString("N2", TurkishCulture)} ₺");

                                AddDetailRow(
                                    table,
                                    "Durum",
                                    GetStatusText(
                                        _trafficFine.Status));

                                AddDetailRow(
                                    table,
                                    "Mevcut Onay Aşaması",
                                    string.IsNullOrWhiteSpace(
                                        _approval.CurrentStepName)
                                        ? "-"
                                        : _approval.CurrentStepName);

                                AddDetailRow(
                                    table,
                                    "Oluşturan",
                                    _trafficFine.CreatedByUserName);

                                AddDetailRow(
                                    table,
                                    "Oluşturulma Tarihi",
                                    FormatDateTime(
                                        _trafficFine.CreatedAt));

                                AddDetailRow(
                                    table,
                                    "Son Güncelleme",
                                    _trafficFine.UpdatedAt.HasValue
                                        ? FormatDateTime(
                                            _trafficFine.UpdatedAt.Value)
                                        : "-");
                            });

                    column.Item()
                        .PaddingTop(14)
                        .Text(
                            "Açıklama")
                        .FontSize(9)
                        .SemiBold()
                        .FontColor(
                            Colors.Grey.Darken1);

                    column.Item()
                        .PaddingTop(4)
                        .Text(
                            string.IsNullOrWhiteSpace(
                                _trafficFine.Description)
                                ? "Açıklama eklenmemiş."
                                : _trafficFine.Description)
                        .FontSize(10);
                });
    }

    private void ComposeWorkflow(
        IContainer container)
    {
        container
            .Element(
                SectionContainer)
            .Column(
                column =>
                {
                    column.Item()
                        .Element(
                            SectionTitle)
                        .Text(
                            "Onay Akışı");

                    if (!string.IsNullOrWhiteSpace(
                            _approval.ConfigurationError))
                    {
                        column.Item()
                            .PaddingTop(12)
                            .Background(
                                Colors.Red.Lighten4)
                            .Padding(10)
                            .Text(
                                _approval.ConfigurationError)
                            .FontColor(
                                Colors.Red.Darken3);

                        return;
                    }

                    if (_approval.Steps.Count == 0)
                    {
                        column.Item()
                            .PaddingTop(12)
                            .Text(
                                "Onay akışı henüz başlatılmadı.")
                            .FontColor(
                                Colors.Grey.Darken1);

                        return;
                    }

                    column.Item()
                        .PaddingTop(12)
                        .Table(
                            table =>
                            {
                                table.ColumnsDefinition(
                                    columns =>
                                    {
                                        columns.ConstantColumn(42);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(1);
                                    });

                                table.Header(
                                    header =>
                                    {
                                        header.Cell()
                                            .Element(
                                                HeaderCell)
                                            .Text(
                                                "Sıra");

                                        header.Cell()
                                            .Element(
                                                HeaderCell)
                                            .Text(
                                                "Aşama");

                                        header.Cell()
                                            .Element(
                                                HeaderCell)
                                            .Text(
                                                "Yetkili Rol");
                                    });

                                foreach (var step in
                                         _approval.Steps
                                             .OrderBy(
                                                 step =>
                                                     step.StepOrder))
                                {
                                    table.Cell()
                                        .Element(
                                            BodyCell)
                                        .Text(
                                            step.StepOrder.ToString(
                                                TurkishCulture));

                                    table.Cell()
                                        .Element(
                                            BodyCell)
                                        .Text(
                                            step.Name);

                                    table.Cell()
                                        .Element(
                                            BodyCell)
                                        .Text(
                                            GetRoleText(
                                                step.RequiredRole));
                                }
                            });
                });
    }

    private void ComposeApprovalHistory(
        IContainer container)
    {
        container
            .Element(
                SectionContainer)
            .Column(
                column =>
                {
                    column.Item()
                        .Element(
                            SectionTitle)
                        .Text(
                            "Onay Geçmişi");

                    if (_approval.History.Count == 0)
                    {
                        column.Item()
                            .PaddingTop(12)
                            .Text(
                                "Henüz onay geçmişi bulunmuyor.")
                            .FontColor(
                                Colors.Grey.Darken1);

                        return;
                    }

                    column.Item()
                        .PaddingTop(12)
                        .Table(
                            table =>
                            {
                                table.ColumnsDefinition(
                                    columns =>
                                    {
                                        columns.RelativeColumn(1.2f);
                                        columns.RelativeColumn(1.4f);
                                        columns.RelativeColumn(1.1f);
                                        columns.RelativeColumn(1.5f);
                                    });

                                table.Header(
                                    header =>
                                    {
                                        header.Cell()
                                            .Element(
                                                HeaderCell)
                                            .Text(
                                                "Tarih");

                                        header.Cell()
                                            .Element(
                                                HeaderCell)
                                            .Text(
                                                "Kullanıcı");

                                        header.Cell()
                                            .Element(
                                                HeaderCell)
                                            .Text(
                                                "İşlem");

                                        header.Cell()
                                            .Element(
                                                HeaderCell)
                                            .Text(
                                                "Aşama");
                                    });

                                foreach (var history in
                                         _approval.History
                                             .OrderBy(
                                                 history =>
                                                     history.ActionAt))
                                {
                                    table.Cell()
                                        .Element(
                                            BodyCell)
                                        .Text(
                                            FormatDateTime(
                                                history.ActionAt));

                                    table.Cell()
                                        .Element(
                                            BodyCell)
                                        .Text(
                                            history.ActionByUserName);

                                    table.Cell()
                                        .Element(
                                            BodyCell)
                                        .Text(
                                            GetActionText(
                                                history.ActionType));

                                    table.Cell()
                                        .Element(
                                            BodyCell)
                                        .Text(
                                            string.IsNullOrWhiteSpace(
                                                history.WorkflowStepName)
                                                ? "-"
                                                : history.WorkflowStepName);

                                    table.Cell()
                                        .ColumnSpan(4)
                                        .Element(
                                            HistoryDetailCell)
                                        .Column(
                                            detail =>
                                            {
                                                detail.Item()
                                                    .Text(
                                                        $"{history.PreviousState} → {history.NewState}")
                                                    .FontSize(9)
                                                    .SemiBold();

                                                if (!string.IsNullOrWhiteSpace(
                                                        history.Comment))
                                                {
                                                    detail.Item()
                                                        .PaddingTop(3)
                                                        .Text(
                                                            history.Comment)
                                                        .FontSize(9)
                                                        .FontColor(
                                                            Colors.Grey.Darken2);
                                                }
                                            });
                                }
                            });
                });
    }

    private static void AddDetailRow(
        TableDescriptor table,
        string label,
        string value)
    {
        table.Cell()
            .Element(
                DetailLabelCell)
            .Text(
                label);

        table.Cell()
            .Element(
                DetailValueCell)
            .Text(
                value);
    }

    private static IContainer SectionContainer(
        IContainer container)
    {
        return container
            .Border(1)
            .BorderColor(
                Colors.Grey.Lighten2)
            .Padding(16);
    }

    private static IContainer SectionTitle(
        IContainer container)
    {
        return container
            .PaddingBottom(8)
            .BorderBottom(1)
            .BorderColor(
                Colors.Grey.Lighten3)
            .DefaultTextStyle(
                style =>
                    style
                        .FontSize(13)
                        .SemiBold());
    }

    private static IContainer DetailLabelCell(
        IContainer container)
    {
        return container
            .BorderBottom(1)
            .BorderColor(
                Colors.Grey.Lighten3)
            .PaddingVertical(7)
            .PaddingRight(12)
            .DefaultTextStyle(
                style =>
                    style
                        .FontSize(9)
                        .SemiBold()
                        .FontColor(
                            Colors.Grey.Darken1));
    }

    private static IContainer DetailValueCell(
        IContainer container)
    {
        return container
            .BorderBottom(1)
            .BorderColor(
                Colors.Grey.Lighten3)
            .PaddingVertical(7)
            .DefaultTextStyle(
                style =>
                    style.FontSize(10));
    }

    private static IContainer HeaderCell(
        IContainer container)
    {
        return container
            .Background(
                Colors.Grey.Lighten3)
            .BorderBottom(1)
            .BorderColor(
                Colors.Grey.Lighten1)
            .Padding(7)
            .DefaultTextStyle(
                style =>
                    style
                        .FontSize(8)
                        .SemiBold());
    }

    private static IContainer BodyCell(
        IContainer container)
    {
        return container
            .BorderBottom(1)
            .BorderColor(
                Colors.Grey.Lighten3)
            .Padding(7)
            .DefaultTextStyle(
                style =>
                    style.FontSize(8));
    }

    private static IContainer HistoryDetailCell(
        IContainer container)
    {
        return container
            .Background(
                Colors.Grey.Lighten4)
            .BorderBottom(1)
            .BorderColor(
                Colors.Grey.Lighten2)
            .Padding(7);
    }

    private static string FormatDateTime(
        DateTimeOffset value)
    {
        return value
            .ToLocalTime()
            .ToString(
                "dd.MM.yyyy HH:mm",
                TurkishCulture);
    }

    private static string GetStatusText(
        TrafficFineStatus status)
    {
        return status switch
        {
            TrafficFineStatus.New =>
                "TASLAK",

            TrafficFineStatus.InApproval =>
                "ONAY SÜRECİNDE",

            TrafficFineStatus.Completed =>
                "ONAYLANMIŞ",

            TrafficFineStatus.Rejected =>
                "REDDEDİLMİŞ",

            _ =>
                status.ToString()
        };
    }

    private static string GetStatusBackgroundColor(
        TrafficFineStatus status)
    {
        return status switch
        {
            TrafficFineStatus.Completed =>
                Colors.Green.Darken2,

            TrafficFineStatus.Rejected =>
                Colors.Red.Darken2,

            TrafficFineStatus.InApproval =>
                Colors.Orange.Darken2,

            _ =>
                Colors.Grey.Darken2
        };
    }

    private static string GetActionText(
        ApprovalActionType actionType)
    {
        return actionType switch
        {
            ApprovalActionType.Submitted =>
                "Onaya Gönderildi",

            ApprovalActionType.Approved =>
                "Onaylandı",

            ApprovalActionType.Rejected =>
                "Reddedildi",

            _ =>
                actionType.ToString()
        };
    }

    private static string GetRoleText(
        string role)
    {
        return role switch
        {
            "Operator" =>
                "Operatör",

            "Manager" =>
                "Yönetici",

            "Finance" =>
                "Finans",

            _ =>
                role
        };
    }
}