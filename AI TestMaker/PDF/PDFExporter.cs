using AI_TestMaker;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.IO;

public static class PDFExporter
{
    public static void ExportarTest(Test test, string ruta)
    {
        // Crear documento
        Document doc = new Document(PageSize.A4, 40, 40, 40, 40);

        using (FileStream fs = new FileStream(ruta, FileMode.Create))
        {
            PdfWriter writer = PdfWriter.GetInstance(doc, fs);
            doc.Open();

            // Título
            var titulo = new Paragraph($"{Path.GetFileNameWithoutExtension(ruta)} - {test.Dificultad}\n\n",
                FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 22));
            titulo.Alignment = Element.ALIGN_CENTER;
            doc.Add(titulo);

            // Datos generales
            doc.Add(new Paragraph($"Fecha: {test.Fecha}"));
            doc.Add(new Paragraph($"Tiempo máximo: {test.TiempoMaximo}"));
            doc.Add(new Paragraph($"Tema: {test.Tema}"));
            doc.Add(new Paragraph($"Tiempo empleado: {test.TiempoEmpleado}"));
            doc.Add(new Paragraph($"Nota: {test.CalcularNota():0.00}\n\n"));

            // Preguntas
            int n = 1;
            foreach (var p in test.Preguntas)
            {
                doc.Add(new Paragraph($"{n}. {p.Enunciado} ",
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12)));

                doc.Add(new Paragraph(" "));

                int index = 0;
                foreach (var o in p.Opciones)
                {
                    // ¿El usuario seleccionó esta opción?
                    bool seleccionada = (p.RespuestaUsuario == index);

                    // Prefijo visual
                    string prefijo = seleccionada ? "[X] " : "[ ] ";

                    // Fuente: correcta en negrita, incorrecta normal
                    var fuente = o.EsCorrecta
                        ? FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11)
                        : FontFactory.GetFont(FontFactory.HELVETICA, 11);

                    // Prefijo en Courier para alineación
                    Chunk chunkPrefijo = new Chunk("   " + prefijo,
                        FontFactory.GetFont(FontFactory.COURIER_BOLD, 11));

                    // Texto de la opción
                    Chunk chunkTexto = new Chunk(o.Texto, fuente);

                    Paragraph parrafo = new Paragraph();
                    parrafo.Add(chunkPrefijo);
                    parrafo.Add(chunkTexto);

                    doc.Add(parrafo);

                    index++;
                }

                doc.Add(new Paragraph("\n"));
                n++;
            }

            doc.Close();
        }
    }
}
