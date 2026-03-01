using HoleriteSign.Core.Interfaces;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HoleriteSign.Api.Services;

/// <summary>
/// Generates a signed PDF by merging the original payslip with
/// a signature proof page (selfie + audit trail).
/// </summary>
public class SignedPdfService
{
    private readonly IStorageService _storage;

    public SignedPdfService(IStorageService storage)
    {
        _storage = storage;
    }

    /// <summary>
    /// Downloads the original PDF, generates a signature proof page,
    /// merges them into a single PDF, and uploads the result.
    /// </summary>
    public async Task<(byte[] pdfBytes, string fileKey)> GenerateSignedPdfAsync(
        string originalFileKey,
        string signedFileKey,
        string employeeName,
        string companyName,
        string periodLabel,
        string originalFilename,
        byte[] photoBytes,
        string photoMimeType,
        string photoHash,
        string signerIp,
        string signerUserAgent,
        DateTime signedAt,
        string consentText)
    {
        // 1) Download original PDF
        byte[] originalPdfBytes;
        try
        {
            originalPdfBytes = await _storage.DownloadAsync(originalFileKey);
        }
        catch
        {
            originalPdfBytes = Array.Empty<byte>();
        }

        // 2) Generate the signature proof page with QuestPDF
        var proofPageBytes = GenerateProofPage(
            employeeName, companyName, periodLabel, originalFilename,
            photoBytes, photoMimeType, photoHash,
            signerIp, signerUserAgent, signedAt, consentText);

        // 3) Merge: original pages + proof page
        byte[] mergedPdfBytes;
        if (originalPdfBytes.Length > 0)
        {
            mergedPdfBytes = MergePdfs(originalPdfBytes, proofPageBytes);
        }
        else
        {
            // Fallback: if original can't be downloaded, use proof page only
            mergedPdfBytes = proofPageBytes;
        }

        // 4) Upload merged signed PDF
        await _storage.UploadBytesAsync(signedFileKey, mergedPdfBytes, "application/pdf");

        return (mergedPdfBytes, signedFileKey);
    }

    /// <summary>
    /// Generates the signature proof page (selfie + audit trail) as a standalone PDF.
    /// </summary>
    private static byte[] GenerateProofPage(
        string employeeName, string companyName, string periodLabel,
        string originalFilename, byte[] photoBytes, string photoMimeType,
        string photoHash, string signerIp, string signerUserAgent,
        DateTime signedAt, string consentText)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);

                page.Header().Column(col =>
                {
                    col.Item().Text("COMPROVANTE DE ASSINATURA DIGITAL")
                        .FontSize(16).Bold().FontColor(Colors.Blue.Darken3);
                    col.Item().PaddingTop(5).LineHorizontal(2).LineColor(Colors.Blue.Darken3);
                });

                page.Content().PaddingTop(20).Column(col =>
                {
                    // Company & employee info
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                        });

                        void AddRow(string label, string value)
                        {
                            table.Cell().Padding(4).Text(label).FontSize(10).Bold();
                            table.Cell().Padding(4).Text(value).FontSize(10);
                        }

                        AddRow("Empresa:", companyName);
                        AddRow("Funcionário:", employeeName);
                        AddRow("Período:", periodLabel);
                        AddRow("Documento:", originalFilename);
                        AddRow("Assinado em:", signedAt.ToString("dd/MM/yyyy HH:mm:ss 'UTC'"));
                    });

                    col.Item().PaddingVertical(15).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                    // Selfie photo
                    col.Item().Text("Foto do Assinante (Selfie)").FontSize(12).Bold();
                    col.Item().PaddingTop(10).AlignCenter().Width(200).Image(photoBytes);

                    col.Item().PaddingVertical(15).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                    // Audit trail
                    col.Item().Text("Trilha de Auditoria").FontSize(12).Bold();
                    col.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                        });

                        void AddAudit(string label, string value)
                        {
                            table.Cell().Padding(3).Text(label).FontSize(9).Bold();
                            table.Cell().Padding(3).Text(value).FontSize(9);
                        }

                        AddAudit("Hash da foto (SHA-256):", photoHash);
                        AddAudit("IP do assinante:", signerIp);
                        AddAudit("User-Agent:", signerUserAgent.Length > 80
                            ? signerUserAgent[..80] + "..." : signerUserAgent);
                        AddAudit("Tipo da foto:", photoMimeType);
                    });

                    col.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                    // Consent text
                    col.Item().Text("Termo de Consentimento").FontSize(12).Bold();
                    col.Item().PaddingTop(5).Text(consentText).FontSize(9).Italic();

                    col.Item().PaddingTop(15).Text(text =>
                    {
                        text.Span("Este documento foi gerado automaticamente pelo sistema HoleriteSign. ")
                            .FontSize(8).FontColor(Colors.Grey.Medium);
                        text.Span($"Emitido em {DateTime.UtcNow:dd/MM/yyyy HH:mm:ss} UTC.")
                            .FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
            });
        }).GeneratePdf();
    }

    /// <summary>
    /// Merges the original PDF with the proof page PDF using PdfSharpCore.
    /// Result = all original pages + proof page(s) at the end.
    /// </summary>
    private static byte[] MergePdfs(byte[] originalPdfBytes, byte[] proofPdfBytes)
    {
        using var output = new PdfDocument();

        // Import original PDF pages
        using (var originalStream = new MemoryStream(originalPdfBytes))
        {
            using var originalDoc = PdfReader.Open(originalStream, PdfDocumentOpenMode.Import);
            for (int i = 0; i < originalDoc.PageCount; i++)
            {
                output.AddPage(originalDoc.Pages[i]);
            }
        }

        // Import proof page(s)
        using (var proofStream = new MemoryStream(proofPdfBytes))
        {
            using var proofDoc = PdfReader.Open(proofStream, PdfDocumentOpenMode.Import);
            for (int i = 0; i < proofDoc.PageCount; i++)
            {
                output.AddPage(proofDoc.Pages[i]);
            }
        }

        using var resultStream = new MemoryStream();
        output.Save(resultStream, false);
        return resultStream.ToArray();
    }
}
