using System.Globalization;
using System.Text;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using TravelPlanner.Api.Models.Entities;

namespace TravelPlanner.Api.UniversalMethod;

public static class TripPdfHelper
{
    private static readonly object FontLock = new();

    public static byte[] Create(Trip trip, List<ScheduleItem> items, List<TripPlace> places, List<Proposal> proposals)
    {
        lock (FontLock) { GlobalFontSettings.FontResolver ??= new PdfFontResolver(); }
        using var document = new PdfDocument();
        document.Info.Title = trip.Trip_Name;
        document.Info.Author = "Наметили";
        var font = new XFont("Manrope", 11);
        var heading = new XFont("Manrope", 17);
        var small = new XFont("Manrope", 9);
        var accent = new XSolidBrush(XColor.FromArgb(182, 77, 52));
        var ink = new XSolidBrush(XColor.FromArgb(48, 44, 41));
        XGraphics? graphics = null;
        PdfPage? page = null;
        double y = 0;
        NewPage();
        Text(trip.Trip_Name, heading, accent);
        Text(trip.Destination);
        Text($"{trip.Start_Date:dd.MM.yyyy} - {trip.End_Date:dd.MM.yyyy}");
        Text("Время расписания указано в UTC.", small);
        Section("Маршрут по дням");
        if (items.Count == 0) Text("Расписание пока не составлено.");
        foreach (var day in items.GroupBy(item => DateOnly.FromDateTime(item.Start_At)))
        {
            Section(day.Key.ToString("dd MMMM yyyy", CultureInfo.GetCultureInfo("ru-RU")));
            foreach (var item in day)
            {
                var end = item.End_At.Date == item.Start_At.Date ? item.End_At.ToString("HH:mm") : item.End_At.ToString("dd.MM HH:mm");
                Text($"{item.Start_At:HH:mm} - {end}  {item.Title}");
                if (!string.IsNullOrWhiteSpace(item.Proposal?.Description)) Text(item.Proposal.Description, small);
                y += 6;
            }
        }
        Section("Отмеченные места");
        if (places.Count == 0) Text("Места ещё не добавлены.");
        foreach (var place in places)
        {
            Text(place.Title);
            Text(FormattableString.Invariant($"{place.Latitude:F5}, {place.Longitude:F5}"), small);
            if (!string.IsNullOrWhiteSpace(place.Note)) Text(place.Note, small);
            y += 6;
        }
        Section("Плановый бюджет");
        Text("Суммы в общей валюте поездки. Фактические расходы сюда не входят.", small);
        foreach (var proposal in proposals.OrderBy(value => value.Title))
            Text($"{proposal.Title}: {proposal.Planned_Cost.ToString("N2", CultureInfo.GetCultureInfo("ru-RU"))}");
        Text($"Всего: {proposals.Sum(value => value.Planned_Cost).ToString("N2", CultureInfo.GetCultureInfo("ru-RU"))}");
        graphics!.Dispose();
        using var output = new MemoryStream();
        document.Save(output, false);
        return output.ToArray();

        void NewPage()
        {
            graphics?.Dispose();
            page = document.AddPage(); page.Size = PdfSharp.PageSize.A4;
            graphics = XGraphics.FromPdfPage(page);
            graphics.DrawString("Наметили / План поездки", small, accent, 48, 32);
            graphics.DrawString(document.PageCount.ToString(CultureInfo.InvariantCulture), small, ink, 48, page.Height.Point - 28);
            y = 64;
        }
        void Section(string title)
        {
            if (y + 76 > page!.Height.Point - 48) NewPage();
            y += 16; Text(title, heading, accent); y += 6;
        }
        void Text(string? text, XFont? selected = null, XBrush? brush = null)
        {
            selected ??= font;
            var line = new StringBuilder();
            var width = page!.Width.Point - 96;
            foreach (var paragraph in (text ?? "").Replace("\r", "").Split('\n'))
            {
                foreach (var word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (line.Length > 0 && graphics!.MeasureString(line + " " + word, selected).Width > width) DrawLine();
                    if (line.Length > 0) line.Append(' ');
                    foreach (var rune in word.EnumerateRunes())
                    {
                        if (line.Length > 0 && graphics!.MeasureString(line.ToString() + rune, selected).Width > width) DrawLine();
                        line.Append(rune.ToString());
                    }
                }
                DrawLine();
            }
            void DrawLine()
            {
                var height = selected.Size * 1.5;
                if (y + height > page!.Height.Point - 48) NewPage();
                graphics!.DrawString(line.ToString(), selected, brush ?? ink, 48, y + selected.Size);
                y += height; line.Clear();
            }
        }
    }
}
