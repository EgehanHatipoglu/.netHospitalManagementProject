using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using HospitalManagementAvolonia.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HospitalManagementAvolonia.Services
{
    /// <summary>Generates PDF and Excel reports from patient and appointment data.</summary>
    public static class ReportService
    {
        static ReportService()
        {
            // QuestPDF community license – free for non-commercial / OSS use
            QuestPDF.Settings.License = LicenseType.Community;
        }

        // ──────────────────────────────────────────────────────────────
        // PDF – Patients
        // ──────────────────────────────────────────────────────────────
        public static string ExportPatientsPdf(List<Patient> patients)
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"Hasta_Raporu_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("🏥 Hastane Yönetim Sistemi")
                           .FontSize(18).Bold().FontColor(Colors.Blue.Medium);
                        col.Item().Text($"Hasta Listesi  |  {DateTime.Now:dd.MM.yyyy HH:mm}")
                           .FontSize(11).FontColor(Colors.Grey.Medium);
                        col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Blue.Lighten3);
                    });

                    page.Content().PaddingVertical(12).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(40);   // ID
                            cols.RelativeColumn(2);    // Ad Soyad
                            cols.ConstantColumn(55);   // Yaş
                            cols.RelativeColumn(2);    // TC
                            cols.RelativeColumn(2);    // Telefon
                        });

                        // Header row
                        static IContainer HeaderCell(IContainer c) =>
                            c.DefaultTextStyle(s => s.Bold().FontSize(10).FontColor(Colors.White))
                             .Background(Colors.Blue.Medium).Padding(6);

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("ID");
                            header.Cell().Element(HeaderCell).Text("Ad Soyad");
                            header.Cell().Element(HeaderCell).Text("Yaş");
                            header.Cell().Element(HeaderCell).Text("TC No");
                            header.Cell().Element(HeaderCell).Text("Telefon");
                        });

                        // Data rows
                        bool alt = false;
                        foreach (var p in patients)
                        {
                            bool isAlt = alt = !alt;
                            IContainer RowCell(IContainer c) =>
                                c.Background(isAlt ? Colors.Grey.Lighten4 : Colors.White)
                                 .BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                 .Padding(5);

                            int age = DateTime.Today.Year - p.BirthDate.Year;
                            table.Cell().Element(RowCell).Text(p.Id.ToString());
                            table.Cell().Element(RowCell).Text(p.FullName);
                            table.Cell().Element(RowCell).Text(age.ToString());
                            table.Cell().Element(RowCell).Text(p.NationalId);
                            table.Cell().Element(RowCell).Text(p.Phone);
                        }
                    });

                    page.Footer().AlignCenter().Text(txt =>
                    {
                        txt.Span("Sayfa ").FontSize(9).FontColor(Colors.Grey.Medium);
                        txt.CurrentPageNumber().FontSize(9).FontColor(Colors.Grey.Medium);
                        txt.Span($" / Toplam Hasta: {patients.Count}").FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                });
            }).GeneratePdf(path);

            return path;
        }

        // ──────────────────────────────────────────────────────────────
        // PDF – Appointments
        // ──────────────────────────────────────────────────────────────
        public static string ExportAppointmentsPdf(List<Appointment> appointments)
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"Randevu_Raporu_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("🏥 Hastane Yönetim Sistemi")
                           .FontSize(18).Bold().FontColor(Colors.Green.Medium);
                        col.Item().Text($"Randevu Listesi  |  {DateTime.Now:dd.MM.yyyy HH:mm}")
                           .FontSize(11).FontColor(Colors.Grey.Medium);
                        col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Green.Lighten3);
                    });

                    page.Content().PaddingVertical(12).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(40);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(3);
                            cols.ConstantColumn(75);
                        });

                        static IContainer HeaderCell(IContainer c) =>
                            c.DefaultTextStyle(s => s.Bold().FontSize(10).FontColor(Colors.White))
                             .Background(Colors.Green.Medium).Padding(6);

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("ID");
                            header.Cell().Element(HeaderCell).Text("Hasta");
                            header.Cell().Element(HeaderCell).Text("Doktor");
                            header.Cell().Element(HeaderCell).Text("Tarih / Saat");
                            header.Cell().Element(HeaderCell).Text("Durum");
                        });

                        bool alt = false;
                        foreach (var a in appointments)
                        {
                            bool isAlt = alt = !alt;
                            IContainer RowCell(IContainer c) =>
                                c.Background(isAlt ? Colors.Grey.Lighten4 : Colors.White)
                                 .BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                 .Padding(5);

                            table.Cell().Element(RowCell).Text(a.Id.ToString());
                            table.Cell().Element(RowCell).Text(a.Patient.FullName);
                            table.Cell().Element(RowCell).Text(a.Doctor.FullName);
                            table.Cell().Element(RowCell).Text(a.Start.ToString("dd/MM/yyyy HH:mm"));
                            table.Cell().Element(RowCell).Text(a.Status);
                        }
                    });

                    page.Footer().AlignCenter().Text(txt =>
                    {
                        txt.Span("Sayfa ").FontSize(9).FontColor(Colors.Grey.Medium);
                        txt.CurrentPageNumber().FontSize(9).FontColor(Colors.Grey.Medium);
                        txt.Span($" / Toplam: {appointments.Count}").FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                });
            }).GeneratePdf(path);

            return path;
        }

        // ──────────────────────────────────────────────────────────────
        // Excel – Patients (.xlsx)
        // ──────────────────────────────────────────────────────────────
        public static string ExportPatientsExcel(List<Patient> patients)
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"Hasta_Listesi_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Hastalar");

            // Headers
            string[] headers = { "ID", "Ad", "Soyad", "TC No", "Telefon", "Doğum Tarihi", "Yaş" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold        = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#3B82F6");
                cell.Style.Font.FontColor   = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Data
            for (int r = 0; r < patients.Count; r++)
            {
                var p   = patients[r];
                int row = r + 2;
                int age = DateTime.Today.Year - p.BirthDate.Year;
                ws.Cell(row, 1).Value = p.Id;
                ws.Cell(row, 2).Value = p.FirstName;
                ws.Cell(row, 3).Value = p.LastName;
                ws.Cell(row, 4).Value = p.NationalId;
                ws.Cell(row, 5).Value = p.Phone;
                ws.Cell(row, 6).Value = p.BirthDate.ToString("dd/MM/yyyy");
                ws.Cell(row, 7).Value = age;

                if (r % 2 == 1)
                {
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#EFF6FF");
                }
            }

            ws.Columns().AdjustToContents();
            wb.SaveAs(path);
            return path;
        }

        // ──────────────────────────────────────────────────────────────
        // Excel – Appointments (.xlsx)
        // ──────────────────────────────────────────────────────────────
        public static string ExportAppointmentsExcel(List<Appointment> appointments)
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"Randevu_Listesi_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Randevular");

            string[] headers = { "ID", "Hasta", "Doktor", "Bölüm", "Tarih", "Saat", "Durum" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold        = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#10B981");
                cell.Style.Font.FontColor   = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            for (int r = 0; r < appointments.Count; r++)
            {
                var a   = appointments[r];
                int row = r + 2;
                ws.Cell(row, 1).Value = a.Id;
                ws.Cell(row, 2).Value = a.Patient.FullName;
                ws.Cell(row, 3).Value = a.Doctor.FullName;
                ws.Cell(row, 4).Value = a.Doctor.Department?.Name ?? "—";
                ws.Cell(row, 5).Value = a.Start.ToString("dd/MM/yyyy");
                ws.Cell(row, 6).Value = a.Start.ToString("HH:mm");
                ws.Cell(row, 7).Value = a.Status;

                if (r % 2 == 1)
                {
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#ECFDF5");
                }
            }

            ws.Columns().AdjustToContents();
            wb.SaveAs(path);
            return path;
        }
    }
}
