using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;

namespace AI_TestMaker.PDF
{
    public class PDFPageEvents : PdfPageEventHelper
    {
        private readonly Font footerFont;
        private Image watermarkLogo;

        public PDFPageEvents()
        {
            footerFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.Gray);

            string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.png");

            if (File.Exists(logoPath))
            {
                // Logo grande (marca de agua)
                watermarkLogo = Image.GetInstance(logoPath);
                watermarkLogo.ScaleAbsolute(300, 300);
                watermarkLogo.Alignment = Image.ALIGN_CENTER;
            }
        }

        public override void OnEndPage(PdfWriter writer, Document document)
        {
            PdfContentByte canvas = writer.DirectContent;

            // Marca de agua
            if (watermarkLogo != null)
            {
                PdfGState gs = new PdfGState();
                gs.FillOpacity = 0.15f;
                canvas.SaveState();
                canvas.SetGState(gs);

                float x = (document.PageSize.Width - watermarkLogo.ScaledWidth) / 2;
                float y = (document.PageSize.Height - watermarkLogo.ScaledHeight) / 2;

                watermarkLogo.SetAbsolutePosition(x, y);

                canvas.AddImage(watermarkLogo);
                canvas.RestoreState();
            }

            // Pie de página
            ColumnText.ShowTextAligned(
                canvas,
                Element.ALIGN_CENTER,
                new Phrase($"Página {writer.PageNumber}", footerFont),
                (document.PageSize.Width / 2),
                document.BottomMargin / 2,
                0
            );
        }

        public override void OnStartPage(PdfWriter writer, Document document)
        {
            writer.StrictImageSequence = true;
        }
    }
}
