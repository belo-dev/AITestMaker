using AI_TestMaker;
using AI_TestMaker.PDF;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.IO;

public static class PDFExporter
{
    public static void ExportarTest(Test test, string ruta, bool incluirSoluciones = true)
    {
        Document doc = new Document(PageSize.A4, 40, 40, 40, 40);

        using (FileStream fs = new FileStream(ruta, FileMode.Create))
        {
            PdfWriter writer = PdfWriter.GetInstance(doc, fs);

            // Activar marca de agua, logo y pie de página
            writer.PageEvent = new PDFPageEvents();

            doc.Open();

            // ============================
            //     TÍTULO
            // ============================
            string tituloTexto = incluirSoluciones
                ? $"{Path.GetFileNameWithoutExtension(ruta)} - {test.Dificultad}"
                : $"{test.Tema}";

            var titulo = new Paragraph($"{tituloTexto}\n\n",
                FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 22));
            titulo.Alignment = Element.ALIGN_CENTER;
            doc.Add(titulo);

            // ============================
            //     DATOS GENERALES
            // ============================
            doc.Add(new Paragraph($"Fecha: {test.Fecha}"));
            doc.Add(new Paragraph($"Tiempo máximo: {test.TiempoMaximo}"));
            doc.Add(new Paragraph($"Tema: {test.Tema}"));

            if (incluirSoluciones)
            {
                doc.Add(new Paragraph($"Tiempo empleado: {test.TiempoEmpleado}"));
                doc.Add(new Paragraph($"Nota: {test.CalcularNota():0.00}\n\n"));
            }
            else
            {
                doc.Add(new Paragraph("Nombre: ________________________________\n"));
                doc.Add(new Paragraph("\n"));
            }

            // ============================
            //     PREGUNTAS
            // ============================
            int n = 1;
            string[] arrOpciones = new string[] { "(A) ", "(B) ", "(C) ", "(D) " };

            foreach (var p in test.Preguntas)
            {
                // Evitar que la pregunta se corte
                if (writer.GetVerticalPosition(true) < 120)
                    doc.NewPage();

                // Enunciado
                doc.Add(new Paragraph($"{n}. {p.Enunciado}",
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12)));

                doc.Add(new Paragraph(" "));

                int index = 0;
                foreach (var o in p.Opciones)
                {
                    bool seleccionada = (p.RespuestaUsuario == index);

                    string prefijo = incluirSoluciones
                        ? (seleccionada ? "[X] " : "[ ] ")
                        : arrOpciones[index];

                    var fuente = incluirSoluciones && o.EsCorrecta
                        ? FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11)
                        : FontFactory.GetFont(FontFactory.HELVETICA, 11);

                    Chunk chunkPrefijo = new Chunk("   " + prefijo,
                        FontFactory.GetFont(FontFactory.COURIER_BOLD, 11));

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

            // ============================
            //     MODELO DE RESPUESTAS
            // ============================
            if (incluirSoluciones)
            {
                doc.NewPage();

                var tituloRespuestas = new Paragraph("Modelo de respuestas\n\n",
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18));
                tituloRespuestas.Alignment = Element.ALIGN_CENTER;
                doc.Add(tituloRespuestas);

                int num = 1;
                foreach (var p in test.Preguntas)
                {
                    // Obtener índice de la opción correcta
                    int indiceCorrecto = p.Opciones.FindIndex(o => o.EsCorrecta);

                    // Convertir a letra A/B/C/D
                    string letraCorrecta = ((char)('A' + indiceCorrecto)).ToString();

                    var linea = new Paragraph($"Pregunta {num} — {letraCorrecta}",
                        FontFactory.GetFont(FontFactory.HELVETICA, 12));

                    doc.Add(linea);
                    num++;
                }
            }


            doc.Close();
        }
    }
}
