using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace SAAC.Whisper
{
    public class WhipserTranscriptionToWordManager : WhisperTranscriptionManager
    {
        public WhipserTranscriptionToWordManager()
        { }

        public override void WriteTranscription(string file, bool cleanList = true)
        {
            WordprocessingDocument wordDocument = WordprocessingDocument.Create(file, WordprocessingDocumentType.Document);
            MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
            mainPart.Document = new Document();
            Body body = new Body();

            foreach (var entry in SortTranscriptions())
            {
                // Create a run for the DateTime in italic
                Run dateTimeRun = new Run(new Text(entry.Item1.ToString("HH:mm:ss")));
                dateTimeRun.RunProperties = new RunProperties(new Italic());

                // Create a run for the speaker id in bold
                Run idRun = new Run(new Text($" - {entry.Item2}"));
                idRun.RunProperties = new RunProperties(new Bold());

                // Create a run for the text
                Run textRun = new Run(new Text($": {entry.Item3}"));

                // Combine DateTime, speaker id and text in one line
                Paragraph paragraph = new Paragraph();
                paragraph.Append(dateTimeRun);
                paragraph.Append(idRun);
                paragraph.Append(textRun);

                // Add the line as a paragraph to the document
                body.Append(paragraph);
            }

            mainPart.Document.Append(body);
            mainPart.Document.Save();
            wordDocument.Save();

            if (cleanList)
                Transcriptions.Clear();
        }
    }
}
