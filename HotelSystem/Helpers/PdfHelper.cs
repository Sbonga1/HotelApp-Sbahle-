using System;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace HotelSystem.Helpers
{
    public static class PdfHelper
    {
        public static byte[] GenerateTicketPDF(
    string customerName,
    string email,
    string ticketType,
    int quantity,
    decimal amount,
    string code,
    string eventTitle = null,
    DateTime? eventDate = null,
    string venue = null,
    string qrImagePath = null
    ) // NEW parameter
        {
            using (var memoryStream = new MemoryStream())
            {
                var document = new Document(PageSize.A5, 36, 36, 36, 36);
                PdfWriter.GetInstance(document, memoryStream);
                document.Open();

                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                var labelFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
                var valueFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

                document.Add(new Paragraph("🎫 Event Ticket", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 15
                });

                void Add(string label, string value)
                {
                    var table = new PdfPTable(2) { WidthPercentage = 100 };
                    table.AddCell(new PdfPCell(new Phrase(label, labelFont)) { Border = 0 });
                    table.AddCell(new PdfPCell(new Phrase(value, valueFont)) { Border = 0 });
                    document.Add(table);
                }

                Add("Customer:", customerName);
                Add("Email:", email);
                if (!string.IsNullOrEmpty(eventTitle)) Add("Event:", eventTitle);
                if (eventDate.HasValue) Add("Event Date:", eventDate.Value.ToString("f"));
                if (!string.IsNullOrEmpty(venue)) Add("Venue:", venue);
                Add("Ticket Type:", ticketType);
                Add("Quantity:", quantity.ToString());
                Add("Amount Paid:", amount.ToString("R 0.00"));
                Add("Issued At:", DateTime.Now.ToString("f"));
                Add("Code:", code);

                // QR Code Section
                if (!string.IsNullOrEmpty(qrImagePath) && File.Exists(qrImagePath))
                {
                    document.Add(new Paragraph(" "));
                    var qr = iTextSharp.text.Image.GetInstance(qrImagePath);
                    qr.Alignment = Element.ALIGN_CENTER;
                    qr.ScaleToFit(150f, 150f);
                    document.Add(qr);

                    document.Add(new Paragraph("Scan this QR code at the entrance.", valueFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingBefore = 10
                    });
                }

                document.Close();
                return memoryStream.ToArray();
            }
        }

    }
}
